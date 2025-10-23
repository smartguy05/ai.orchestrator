using ChromaDB.Client;
using Ai.Orchestrator.Plugins.Memories.Models;
using System.ClientModel;
using Ai.Orchestrator.Models.Enums;
using Ai.Orchestrator.Models.Interfaces;
using ChromaDB.Client.Models;
using OpenAI;

namespace Ai.Orchestrator.Plugins.Memories
{
    /// <summary>
    /// Plugin to interact with ChromaDB for storing and retrieving AI agent memories.
    /// </summary>
    public class ChromaService : IDisposable
    {
        private static Guid? _dbCollectionId;
        private readonly ChromaClient _chromaClient;
        private readonly ChromaConfigurationOptions _chromaConfigOptions;
        private readonly HttpClient _httpClient;
        private readonly string _defaultCollectionName;
        private readonly OpenAIClient _openAiClient;
        private readonly string _embeddingModel;
        private readonly LogDelegate _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromaService"/> class.
        /// </summary>
        /// <param name="config">The service configuration containing ChromaDB and OpenAI settings.</param>
        /// <param name="httpClientInstance">Optional HttpClient instance. If null, a new one will be created.</param>
        public ChromaService(ServiceConfig config, LogDelegate logger, HttpClient httpClientInstance = null)
        {
            _logger = logger;
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.ChromaUrl))
                throw new ArgumentException("Chroma API URL cannot be null or whitespace.", nameof(config.ChromaUrl));
            if (string.IsNullOrWhiteSpace(config.DefaultCollectionName))
                throw new ArgumentException("Default collection name cannot be null or whitespace.", nameof(config.DefaultCollectionName));
            if (string.IsNullOrWhiteSpace(config.OpenAiApiKey))
                throw new ArgumentException("OpenAI API Key cannot be null or whitespace.", nameof(config.OpenAiApiKey));
            
            _chromaConfigOptions = new ChromaConfigurationOptions(uri: config.ChromaUrl);
            _httpClient = httpClientInstance ?? new HttpClient();
            _chromaClient = new ChromaClient(_chromaConfigOptions, _httpClient);
            _defaultCollectionName = config.DefaultCollectionName;
            
            // Initialize OpenAI client for embeddings
            var openAiOptions = new OpenAIClientOptions();
            if (!string.IsNullOrWhiteSpace(config.OpenAiUrl))
            {
                openAiOptions.Endpoint = new Uri(config.OpenAiUrl);
            }
            _openAiClient = new OpenAIClient(new ApiKeyCredential(config.OpenAiApiKey), openAiOptions);
            _embeddingModel = config.EmbeddingModel ?? "text-embedding-3-small";
        }

        /// <summary>
        /// Tests the connection to the ChromaDB instance.
        /// </summary>
        /// <returns>True if the connection is successful, otherwise false.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await _logger(LogLevel.Info, $"Testing connection to ChromaDB at: {_chromaConfigOptions.Uri}");
                var heartbeat = await _chromaClient.Heartbeat();
                await _logger(LogLevel.Info, $"Heartbeat response: {heartbeat.NanosecondHeartbeat}");
                // Heartbeat returns a long, typically a timestamp. Success is indicated by no exception.
                return heartbeat.NanosecondHeartbeat > 0;
            }
            catch (Exception ex)
            {
                await _logger(LogLevel.Error,$"Connection test failed: {ex.Message}");
                await _logger(LogLevel.Error,$"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Diagnostic method to test ChromaDB operations step by step
        /// </summary>
        /// <returns>Diagnostic information about the ChromaDB connection and operations.</returns>
        public async Task<string> DiagnoseChromaDbAsync()
        {
            var diagnostics = new List<string>();
            
            try
            {
                diagnostics.Add($"ChromaDB URL configured: {_chromaConfigOptions.Uri}");
                diagnostics.Add($"Default collection name: {_defaultCollectionName}");
                
                // Test heartbeat
                try
                {
                    var heartbeat = await _chromaClient.Heartbeat();
                    diagnostics.Add($"✓ Heartbeat successful: {heartbeat.NanosecondHeartbeat}");
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"✗ Heartbeat failed: {ex.Message}");
                    return string.Join("\n", diagnostics);
                }
                
                // Test listing collections
                try
                {
                    var collections = await _chromaClient.ListCollections();
                    diagnostics.Add($"✓ Listed collections: {collections.Count()} found");
                    foreach (var col in collections.Take(5))
                    {
                        diagnostics.Add($"  - {col.Name} (ID: {col.Id})");
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"✗ List collections failed: {ex.Message}");
                }
                
                // Test creating a simple collection
                try
                {
                    var testCollectionName = "test-" + Guid.NewGuid().ToString()[..8];
                    var sanitizedName = SanitizeCollectionName(testCollectionName);
                    diagnostics.Add($"Testing collection creation with name: {sanitizedName}");
                    
                    var collection = await _chromaClient.CreateCollection(sanitizedName);
                    diagnostics.Add($"✓ Test collection created: {collection.Name} (ID: {collection.Id})");
                    
                    // Clean up test collection
                    await _chromaClient.DeleteCollection(sanitizedName);
                    diagnostics.Add($"✓ Test collection deleted");
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"✗ Test collection creation failed: {ex.Message}");
                }
                
            }
            catch (Exception ex)
            {
                diagnostics.Add($"✗ General diagnostic error: {ex.Message}");
            }
            
            return string.Join("\n", diagnostics);
        }

        /// <summary>
        /// Adds a memory from text content to the specified collection with automatic embedding generation.
        /// </summary>
        /// <param name="text">The text content of the memory.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <returns>True if the memory was added successfully, otherwise false.</returns>
        public async Task<bool> AddMemoryAsync(string text, string collectionName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));

                var memoryId = Guid.NewGuid().ToString();
                
                // Generate embedding using OpenAI
                var embedding = await GenerateEmbeddingAsync(text);
                
                // Test connection first
                if (!await TestConnectionAsync())
                {
                    throw new Exception("Cannot connect to ChromaDB server");
                }
                
                // Get or create collection using the robust method
                var collectionClient = await GetOrCreateCollectionClientAsync(collectionName);
                
                // Add the document
                var metadata = new Dictionary<string, object>
                {
                    ["created_at"] = DateTime.UtcNow.ToString("O"),
                    ["source"] = "text_input",
                    ["embedding_model"] = _embeddingModel
                };
                
                await collectionClient.Add(
                    ids: [memoryId],
                    embeddings: [embedding],
                    metadatas: [metadata],
                    documents: [text]
                );
                
                await _logger(LogLevel.Info, $"Successfully added memory with ID: {memoryId}");
                return true;
            }
            catch (Exception ex)
            {
                await _logger(LogLevel.Error, $"Error adding memory: {ex.Message}");
                await _logger(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Searches for memories in the specified collection based on a text query.
        /// This method generates embeddings for the query text automatically.
        /// </summary>
        /// <param name="queryText">The text query to search with.</param>
        /// <param name="where">Optional where clause for filtering.</param>
        /// <param name="whereDocument">Optional document where clause for filtering.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <param name="includeEmbeddings">Optional. Whether to include embeddings in the results.</param>
        /// <returns>A list of matching memory records.</returns>
        public async Task<IEnumerable<string>> SearchMemoriesByTextAsync(
            string queryText,
            ChromaWhereOperator where = null,
            ChromaWhereDocumentOperator whereDocument = null,
            int maxResults = 5,
            string collectionName = null,
            bool includeEmbeddings = false)
        {
            if (string.IsNullOrWhiteSpace(queryText))
                throw new ArgumentException("Query text cannot be null or whitespace.", nameof(queryText));

            var queryEmbedding = await GenerateEmbeddingAsync(queryText);
            var memories = await SearchMemoriesAsync(queryEmbedding, where, whereDocument, maxResults, collectionName, includeEmbeddings);
            return memories.Select(s => s.Text).ToList();
        }
        
        /// <summary>
        /// Retrieves a specific memory by its ID from the specified collection.
        /// </summary>
        /// <param name="memoryId">The ID of the memory to retrieve.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <param name="includeEmbedding">Optional. Whether to include embedding in the result.</param>
        /// <returns>The memory record if found, otherwise null.</returns>
        public async Task<MemoryRecord> GetMemoryByIdAsync(string memoryId, string collectionName = null, bool includeEmbedding = false)
        {
            if (string.IsNullOrWhiteSpace(memoryId)) throw new ArgumentException("Memory ID cannot be null or whitespace.", nameof(memoryId));

            // var collectionId = await GetDbId(_chromaConfigOptions.Uri.ToString(), collectionName);
            var collectionClient = await GetOrCreateCollectionClientAsync(collectionName);
            var includeOption = ChromaGetInclude.Metadatas | ChromaGetInclude.Documents;
             if (includeEmbedding)
            {
                includeOption |= ChromaGetInclude.Embeddings;
            }

            var result = await collectionClient.Get([memoryId], include: includeOption);
            var ids = result.Select(s => s.Id).ToList();
            var embeddings = result.Select(s => s.Embeddings).ToList();
            var metadatas = result.Select(s => s.Metadata).ToList();
            var documents = result.Select(s => s.Document).ToList();
            
            if (ids.Any())
            {
                var id = ids.First();
                var embedding = includeEmbedding && embeddings.Any() ? embeddings.First() : ReadOnlyMemory<float>.Empty;
                var metadata = metadatas?.FirstOrDefault()?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var document = documents?.FirstOrDefault();

                if (string.IsNullOrEmpty(document) && metadata?.ContainsKey("text_content") == true)
                {
                    document = metadata["text_content"]?.ToString();
                }

                return new MemoryRecord(id, embedding, document, metadata);
            }
            return null;
        }
        
        /// <summary>
        /// Deletes a memory by its ID from the specified collection.
        /// </summary>
        /// <param name="memoryId">The ID of the memory to delete.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <returns>Task indicating completion.</returns>
        public async Task<bool> DeleteMemoryAsync(string memoryId, string collectionName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(memoryId)) throw new ArgumentException("Memory ID cannot be null or whitespace.", nameof(memoryId));

                // var collectionId = await GetDbId(_chromaConfigOptions.Uri.ToString(), collectionName);
                var collectionClient = await GetOrCreateCollectionClientAsync(collectionName);
                await collectionClient.Delete([memoryId]);
            }
            catch (Exception e)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Edits an existing memory by updating its text content and regenerating embeddings.
        /// </summary>
        /// <param name="memoryId">The ID of the memory to edit.</param>
        /// <param name="text">The new text content for the memory.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <returns>True if the memory was updated successfully, otherwise false.</returns>
        public async Task<bool> EditMemoryAsync(string memoryId, string text, string collectionName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(memoryId))
                    throw new ArgumentException("Memory ID cannot be null or whitespace.", nameof(memoryId));
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));

                var existingMemory = await GetMemoryByIdAsync(memoryId, collectionName);
                if (existingMemory == null)
                {
                    return false; // Memory not found
                }

                // Generate new embedding for the updated text
                var newEmbedding = await GenerateEmbeddingAsync(text);
                
                // Update metadata to reflect the edit
                var updatedMetadata = existingMemory.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) 
                                    ?? new Dictionary<string, object>();
                updatedMetadata["updated_at"] = DateTime.UtcNow.ToString("O");
                updatedMetadata["embedding_model"] = _embeddingModel;
                
                // Create updated memory record
                var updatedMemory = new MemoryRecord(
                    id: memoryId,
                    embedding: newEmbedding,
                    text: text,
                    metadata: updatedMetadata
                );

                // Use the AddMemoryRecordAsync method to update
                return await AddMemoryRecordAsync(updatedMemory, collectionName);
            }
            catch (Exception ex)
            {
                await _logger(LogLevel.Error, $"Error editing memory: {ex.Message}");
                return false;
            }
        }
        
        public async Task<ChromaCollectionClient> GetOrCreateCollectionClientAsync(string collectionName = null)
        {
            try
            {
                var name = collectionName ?? _defaultCollectionName;
                
                // Validate collection name
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Collection name cannot be null or empty");
                }
                
                // Sanitize collection name - ChromaDB has naming restrictions
                name = SanitizeCollectionName(name);
                await _logger(LogLevel.Debug,$"Working with collection: {name}");
                await _logger(LogLevel.Trace,$"ChromaDB URL: {_chromaConfigOptions.Uri}");
                
                // First try to get the collection
                ChromaCollection collection = null;
                try
                {
                    await _logger(LogLevel.Debug,$"Attempting to get existing collection: {name}");
                    collection = await _chromaClient.GetCollection(name);
                    await _logger(LogLevel.Debug,$"Found existing collection: {name}");
                }
                catch (Exception getEx)
                {
                    await _logger(LogLevel.Info,$"Collection not found, will create: {getEx.Message}");
                    // Collection doesn't exist, we'll create it
                }
                
                // If collection doesn't exist, create it
                if (collection == null)
                {
                    try
                    {
                        await _logger(LogLevel.Info,$"Creating new collection: {name}");
                        collection = await _chromaClient.CreateCollection(
                            name: name,
                            metadata: new Dictionary<string, object> 
                            { 
                                ["description"] = $"Memory collection created at {DateTime.UtcNow:O}" 
                            });
                        await _logger(LogLevel.Info,$"Successfully created collection: {name}");
                    }
                    catch (Exception createEx)
                    {
                        await _logger(LogLevel.Error,$"Error creating collection via CreateCollection: {createEx.Message}");

                        // As a fallback, try GetOrCreateCollection
                        await _logger(LogLevel.Error,"Attempting GetOrCreateCollection as fallback...");
                        try
                        {
                            // ChromaDB requires non-empty metadata dictionary
                            collection = await _chromaClient.GetOrCreateCollection(
                                name,
                                new Dictionary<string, object>
                                {
                                    ["description"] = $"Memory collection created at {DateTime.UtcNow:O}"
                                });
                        }
                        catch (Exception fallbackEx)
                        {
                            await _logger(LogLevel.Error,$"GetOrCreateCollection also failed: {fallbackEx.Message}");
                            throw;
                        }
                    }
                }
                
                if (collection == null)
                {
                    throw new Exception($"Failed to get or create collection '{name}' - all attempts returned null");
                }

                _dbCollectionId ??= collection.Id;
                
                await _logger(LogLevel.Debug,$"Successfully obtained collection: {name} with ID: {_dbCollectionId}");

                return new ChromaCollectionClient(collection, _chromaConfigOptions, _httpClient);
            }
            catch (Exception ex)
            {
                await _logger(LogLevel.Error,$"Error in GetOrCreateCollectionClientAsync: {ex.Message}");
                await _logger(LogLevel.Error,$"Stack trace: {ex.StackTrace}");
                
                // Add more specific error information
                if (ex.Message.Contains("MethodNotAllowed") || ex.Message.Contains("405"))
                {
                    throw new Exception($"ChromaDB API error - check if server is running correctly and URL is correct. URL: {_chromaConfigOptions.Uri}", ex);
                }
                else if (ex.Message.Contains("NotFound") || ex.Message.Contains("404"))
                {
                    throw new Exception($"ChromaDB server not found. Check if the ChromaDB server is running and accessible at {_chromaConfigOptions.Uri}. Make sure the URL is correct and the server is started.", ex);
                }
                
                throw new Exception($"Failed to get or create collection '{collectionName ?? _defaultCollectionName}': {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        
        private string SanitizeCollectionName(string name)
        {
            // ChromaDB collection names must be between 3-63 characters and contain only a-z, 0-9, -, and _
            // They must start and end with a letter or number
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\-_]", "-")
                .Trim('-', '_');
            
            // Ensure it starts with a letter or number
            if (!char.IsLetterOrDigit(sanitized.FirstOrDefault()))
            {
                sanitized = "a" + sanitized;
            }
            
            // Ensure it ends with a letter or number
            if (!char.IsLetterOrDigit(sanitized.LastOrDefault()))
            {
                sanitized = sanitized + "1";
            }
            
            // Ensure length is between 3-63 characters
            if (sanitized.Length < 3)
            {
                sanitized = sanitized.PadRight(3, '0');
            }
            
            return sanitized.Substring(0, Math.Min(sanitized.Length, 63));
        }
        
        /// <summary>
        /// Searches for memories in the specified collection based on a query embedding.
        /// </summary>
        /// <param name="queryEmbedding">The embedding to search with.</param>
        /// <param name="where">Optional where clause for filtering.</param>
        /// <param name="whereDocument">Optional document where clause for filtering.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <param name="includeEmbeddings">Optional. Whether to include embeddings in the results.</param>
        /// <returns>A list of matching memory records.</returns>
        private async Task<IEnumerable<MemoryRecord>> SearchMemoriesAsync(
            ReadOnlyMemory<float> queryEmbedding,
            ChromaWhereOperator where,
            ChromaWhereDocumentOperator whereDocument,
            int maxResults = 10,
            string collectionName = null,
            bool includeEmbeddings = false)
        {
            var collectionClient = await GetOrCreateCollectionClientAsync(collectionName);
            var includeOption = ChromaQueryInclude.Metadatas | ChromaQueryInclude.Documents | ChromaQueryInclude.Distances;
            if (includeEmbeddings)
            {
                includeOption |= ChromaQueryInclude.Embeddings;
            }

            var queryResults = await collectionClient.Query(
                queryEmbeddings: [queryEmbedding],
                nResults: maxResults,
                where: where,
                whereDocument: whereDocument,
                include: includeOption
            );

            var memories = new List<MemoryRecord>();
            if (queryResults.Any())
            {
                foreach (var resultSet in queryResults)
                {
                    if (resultSet == null) continue;
                    foreach (var item in resultSet)
                    {
                        if (item?.Id == null) continue;
                        
                        var textContent = item.Document;
                        if (string.IsNullOrEmpty(textContent) && item.Metadata?.ContainsKey("text_content") == true)
                        {
                            textContent = item.Metadata["text_content"]?.ToString();
                        }
                        
                        memories.Add(new MemoryRecord(
                            item.Id,
                            item.Embeddings.HasValue ? item.Embeddings.Value : ReadOnlyMemory<float>.Empty,
                            textContent,
                            item.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        ));
                    }
                }
            }
            return memories;
        }
        
        /// <summary>
        /// Adds or updates a memory record in the specified collection.
        /// This is the original method that accepts a full MemoryRecord object.
        /// </summary>
        /// <param name="memory">The memory record to add or update.</param>
        /// <param name="collectionName">Optional. The name of the collection. Uses default if null.</param>
        /// <returns>Task indicating completion.</returns>
        private async Task<bool> AddMemoryRecordAsync(MemoryRecord memory, string collectionName = null)
        {
            try
            {
                if (memory == null) throw new ArgumentNullException(nameof(memory));

                // var collectionId = await GetDbId(_chromaConfigOptions.Uri.ToString(), collectionName);
                var collectionClient = await GetOrCreateCollectionClientAsync(collectionName);
            
                var metadata = memory.Metadata ?? new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(memory.Text) && !metadata.ContainsKey("text_content"))
                {
                    metadata["text_content"] = memory.Text;
                }

                await collectionClient.Upsert(
                    ids: [memory.Id],
                    embeddings: [memory.Embedding ?? new ReadOnlyMemory<float>()],
                    metadatas: [metadata],
                    documents: !string.IsNullOrEmpty(memory.Text) ? [memory.Text] : null
                );
            }
            catch (Exception e)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Generates embeddings for the given text using OpenAI.
        /// </summary>
        /// <param name="text">The text to generate embeddings for.</param>
        /// <returns>The embedding vector as ReadOnlyMemory&lt;float&gt;.</returns>
        private async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var embeddingClient = _openAiClient.GetEmbeddingClient(_embeddingModel);
                var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
                return embedding.Value.ToFloats();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate embedding: {ex.Message}", ex);
            }
        }
    }
} 