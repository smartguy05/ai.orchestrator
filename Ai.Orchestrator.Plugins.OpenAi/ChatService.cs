using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Plugins.OpenAi.Models;
using Ai.Orchestrator.Services;

namespace Ai.Orchestrator.Plugins.OpenAi;

public class ChatService
{
    private readonly JsonSerializerOptions _serializerOptions = new ()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public ChatService()
    {
        MessageCache.Init();
    }
    
    private ScheduledTask _pendingScheduledTask;
    
    public async Task<object> CompleteChat(ServiceRequest request, ServiceConfig config,
        Dictionary<string, IEnumerable<string>> serviceFunctions, int attempt)
    {
        const int maxAttempts = 3;
        request.ConversationId ??= Guid.NewGuid().ToString();
        
        var tools = config.Tools.Select(s => new ToolOption("function", s.Function)).ToList();
        var messages = await GetMessages(request);

        if (request.Photo is not null && request.Photo.Any())
        {
            messages = await UploadImageAsync(config, request.Photo, messages, request.ConversationId);
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                return new {
                    request.ConversationId,
                    Result = "Image received"
                };
            }
        }
        
        var result = await SendRequest(config, request, messages, tools);

        if (!result.IsSuccessStatusCode)
        {
            var errorContent = await result.Content.ReadAsStringAsync();

            // Context too long, remove all but system prompt and last user message(s)
            if (errorContent.ToLower().Contains("this model's maximum context length is"))
            {
                if (messages.Count(w => w.Role.ToLower() == "user") < 2)
                {
                    var lastUserMessage = messages.FindLastIndex(f => f.Role.ToLower() == "user");
                    messages = new List<ChatMessageHistory>
                    {
                        messages.First(),
                        messages[lastUserMessage]
                    };
                }
                else // get last 2 user messages
                {
                    var secondToLastUserMessageIndex = messages
                        .Select((msg, index) => new { Message = msg, Index = index })
                        .Where(x => x.Message.Role.ToLower() == "user")
                        .OrderBy(x => x.Index)
                        .Reverse()
                        .Skip(1)
                        .First().Index;
                    var lastUserMessage = messages.FindLastIndex(f => f.Role.ToLower() == "user");
                    var userMessages = messages
                        .Where((msg, index) => index >= secondToLastUserMessageIndex && index <= lastUserMessage)
                        .ToList();
                    
                    messages = new List<ChatMessageHistory>
                    {
                        messages.First() // include system message
                    };
                    messages.AddRange(userMessages);
                }
                
                await MessageCache.SaveCachedMessages(request.ConversationId, messages);

                if (attempt <= maxAttempts)
                {
                    attempt++;
                    request.Messages = null;
                    Console.WriteLine($"Retrying {attempt} of {maxAttempts} attempts");
                    return await CompleteChat(request, config, serviceFunctions, attempt);
                }
                
                Console.WriteLine("Retry failed.");
            }

            if (errorContent == "Last user message must contain a text type")
            {
                return null;
            }
            
            throw new Exception($"HTTP Error: {result.StatusCode}\nResponse Content: {errorContent}");
        }
        
        var choice = result.Content.ReadFromJsonAsync<ChatCompletionResponse>().Result.Choices.First();
        
        // if (!ContainsFileUrl(messages))
        // {
        //     // todo: check and log result
        //     await CleanupExpiredFilesAsync(config);
        // }

        return await ProcessResponse(choice, request, messages, serviceFunctions, config);
    }
    
    private async Task<object> ProcessResponse(Choice choice, ServiceRequest request, List<ChatMessageHistory> messages, Dictionary<string, IEnumerable<string>> serviceFunctions, ServiceConfig config)
    {
        switch (choice.FinishReason)
        {
            case ChatFinishReasons.Stop:
            {
                var response = choice.Message.GetProperty("content").GetString(); 
                messages.Add(new ()
                {
                    Role = ChatMessageTypes.Assistant,
                    Content = response,
                });
                await MessageCache.SaveCachedMessages(request.ConversationId, messages);
                return new {
                    request.ConversationId,
                    Result = response
                };
            }
            case ChatFinishReasons.ToolCalls:
            {
                var requests = new List<OrchestratorRequest>();
                var scheduledTasks = 0;
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(choice.Message.ToString());
                    var root = doc.RootElement;
                    var toolCallsResponse = root.Deserialize<ToolCallsResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    messages.Add(new ()
                    {
                        Role = ChatMessageTypes.Assistant,
                        ToolCallId = null,
                        Content = null,
                        ToolCalls = toolCallsResponse.ToolCalls,
                        Name = "tool_calls"
                    });
                    
                    foreach (var (toolCall, index) in toolCallsResponse.ToolCalls.Select((call, idx) => (call, idx)))
                    {
                        var serviceFunction = serviceFunctions
                            .FirstOrDefault(w => w.Value.ToList().Contains(toolCall.Function.Name));
                        using var argumentsJson = JsonDocument.Parse(toolCall.Function.Arguments);
                    
                        var serviceRequest = new Dictionary<string, object>();
                        
                        serviceRequest.Add("method", toolCall.Function.Name);
                        serviceRequest.Add("requestingService", "Ai.Orchestrator.Plugins.OpenAi");
                        foreach (var property in argumentsJson.RootElement.EnumerateObject())
                        {
                            if (!serviceRequest.ContainsKey(property.Name))
                            {
                                serviceRequest.Add(char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1)
                                    , property.Value);
                            }
                        }
                        
                        if (toolCall.Function.Name == "schedule_task")
                        {
                            var taskExpiration = serviceRequest.TryGetValue("expiration", out var expiration)
                                ? expiration switch
                                {
                                    string stringExpiration => stringExpiration,
                                    JsonElement jsonExpiration => jsonExpiration.GetString(),
                                    _ => null
                                }
                                : null;
                            var taskRecurring = serviceRequest.TryGetValue("recurring", out var recurring) && recurring switch
                            {
                                bool boolRecurring => boolRecurring,
                                JsonElement jsonRecurring => jsonRecurring.GetBoolean(),
                                _ => false
                            };
                            var taskTimeout = serviceRequest.TryGetValue("timeout", out var timeout)
                                ? timeout switch
                                {
                                    string stringTimeout => int.TryParse(stringTimeout, out var stringTimeoutInt) ? stringTimeoutInt : null,
                                    JsonElement jsonTimeout => jsonTimeout.TryGetInt32(out var intVal) ? intVal : null,
                                    _ => (int?)null
                                }
                                : null;
                            var taskName = serviceRequest.TryGetValue("name", out var name)
                                ? name switch
                                {
                                    string stringName => stringName,
                                    JsonElement jsonName => jsonName.GetString(),
                                    _ => null
                                }
                                : null;
                            var taskDescription = serviceRequest.TryGetValue("description", out var description)
                                ? description switch
                                {
                                    string stringDescription => stringDescription,
                                    JsonElement jsonDescription => jsonDescription.GetString(),
                                    _ => null
                                }
                                : null;
                            
                            _pendingScheduledTask = new ScheduledTask
                            {
                                Description = taskDescription,
                                Name = taskName,
                                Expiration = taskExpiration,
                                Timeout = taskTimeout,
                                IsRecurring = taskRecurring
                            };
                            
                            messages.Add(new ChatMessageHistory
                            {
                                Role = ChatMessageTypes.Tool,
                                Content = $"Scheduled task '{_pendingScheduledTask.Name}' pending",
                                ToolCallId = request.ToolCallId,
                                Name = "schedule_task"
                            });
                            
                            continue;
                        }

                        if (_pendingScheduledTask is not null)
                        {
                            Console.WriteLine("Scheduling task");
                            _pendingScheduledTask.OrchestratorRequest = new OrchestratorRequest
                            {
                                Service = serviceFunction.Key,
                                ServiceRequest = JsonSerializer.Serialize(serviceRequest, _serializerOptions),
                                ToolCallId = null,
                                ServiceFunctions = serviceFunctions,
                                Messages =
                                [
                                    new ChatMessageHistory
                                    {
                                        Role = "system",
                                        Content = config.DefaultSystemPrompt
                                    }
                                ]
                            };
                            
                            Console.WriteLine(JsonSerializer.Serialize(_pendingScheduledTask, _serializerOptions));
                            
                            var taskScheduler = ServiceResolver.GetTaskScheduler();
                            await taskScheduler.AddScheduledTask(_pendingScheduledTask);
                            
                            messages.Add(new ChatMessageHistory
                            {
                                Role = ChatMessageTypes.Tool,
                                Content = $"Scheduled task '{_pendingScheduledTask.Name}' scheduled",
                                ToolCallId = request.ToolCallId,
                                Name = "schedule_task"
                            });
                            _pendingScheduledTask = null;
                            scheduledTasks++;
                        }
                        else
                        {
                            Console.WriteLine("Tool call required");
                            Console.WriteLine(JsonSerializer.Serialize(messages, _serializerOptions));
                            serviceRequest.Add("conversationId", request.ConversationId);
                            requests.Add(new OrchestratorRequest
                            {
                                Service = serviceFunction.Key,
                                ServiceRequest = JsonSerializer.Serialize(serviceRequest, _serializerOptions),
                                ToolCallId = toolCall.Id,
                                ServiceFunctions = serviceFunctions,
                                Messages = messages
                            });
                        }
                    }
                    
                    await MessageCache.SaveCachedMessages(request.ConversationId, messages);

                    if (scheduledTasks > 0)
                    {
                        // todo: Return to AI plugin instead of short-circuiting
                        // requests.Add(new OrchestratorRequest
                        // {
                        //     Service = "Ai.Orchestrator.Plugins.OpenAi",
                        //     ServiceRequest = JsonSerializer.Serialize(request, _serializerOptions),
                        //     ToolCallId = null,
                        //     ServiceFunctions = serviceFunctions,
                        //     Messages = messages
                        // });
                        return new
                        {
                            request.ConversationId,
                            Result = "Tasks successfully scheduled!"
                        };
                    }
                    
                    if (requests.Count == 1)
                    {
                        return requests.First();
                    }

                    return requests;
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred processing return value: {JsonSerializer.Serialize(choice, _serializerOptions)}", e);
                }
            }
            case ChatFinishReasons.Length:
            {
                throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");
            }
            case ChatFinishReasons.ContentFilter:
            {
                throw new NotImplementedException("Omitted content due to a content filter flag.");
            }
            case ChatFinishReasons.FunctionCall:
            {
                throw new NotImplementedException("Deprecated in favor of tool calls.");
            }
            default:
            {
                throw new NotImplementedException(choice.FinishReason);
            }
        }
    }
    
    private async Task<HttpResponseMessage> SendRequest(ServiceConfig config, ServiceRequest request, List<ChatMessageHistory> messages, List<ToolOption> tools)
    {
        var validRoles = new List<string>
        {
            ChatMessageTypes.User,
            ChatMessageTypes.Tool
        };
        var lastMessageRole = messages.Last().Role.ToLower();
        if (messages.Count == 0 || !validRoles.Contains(lastMessageRole))
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Last message must be a user message")
            };
        }
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(300); // Set timeout to 300 seconds (5 minutes)
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenAiApiKey);
        
        var oAiRequest = new ApiRequest
        {
            Model = request.Model,
            Messages = messages,
            Tools = tools
        };
        var body = JsonSerializer.Serialize(oAiRequest, _serializerOptions);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        return await httpClient.PostAsync($"{config.OpenAiUrl}/chat/completions", content);
    }
    
    private async Task<List<ChatMessageHistory>> UploadImageAsync(ServiceConfig config, string photo, List<ChatMessageHistory> messages, string conversationId)
    {
        using var content = new MultipartFormDataContent();
        
        var photoBytes = Convert.FromBase64String(photo);
        var fileContent = new ByteArrayContent(photoBytes);
        
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "uploaded.jpg");
        content.Add(new StringContent("assistants"), "purpose");
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(300); // Set timeout to 300 seconds (5 minutes)
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenAiApiKey);
            
            var response = await httpClient.PostAsync(
                $"{config.OpenAiUrl}/files", 
                content
            );

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FileResponse>(responseString);
                Console.WriteLine($"File uploaded successfully. File ID: {result.Id}");

                var lastMessage = messages.Last();
                if (lastMessage.Role.ToLower() == "user")
                {
                    if (lastMessage.Content is JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
                    {
                        var contentList = jsonElement.EnumerateArray().ToList();
                        contentList.Add(JsonSerializer.SerializeToElement(new 
                        {
                            type = "file_url", 
                            file_url = new
                            {
                                file_id = result.Id
                            }
                        }, _serializerOptions));
                        messages.Last().Content = contentList;
                    }
                    else
                    {
                        var contentList = new List<object>();
                        if (!string.IsNullOrWhiteSpace(lastMessage.Content))
                        {
                            contentList.Add(new { type = "text", text = lastMessage.Content });
                        }
                        contentList.Add(new { type = "file_url", file_url = new { file_id = result.Id } });
                        messages.Last().Content = contentList;
                    }
                }
                else
                {
                    var contentList = new List<object>();
                    contentList.Add(new 
                    {
                        type = "file_url", 
                        file_url = new
                        {
                            file_id = result.Id
                        }
                    });
                    messages.Last().Content = contentList;
                    messages.Add(new ChatMessageHistory
                    {
                        Role = "user",
                        Content = contentList
                    });
                }

                await MessageCache.SaveCachedMessages(conversationId, messages);
                return messages;
            }
            
            var errorResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Upload failed: {errorResponse}");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error uploading image: {e.Message}");
            throw;
        }

        return messages;
    }
    
    private void NormalizeMessages(ref List<ChatMessageHistory> messages)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            if (messages[i].Content is not null && messages[i].Content is not string)
            {
                var content = CleanMessage(JsonSerializer.Serialize(messages[i].Content, _serializerOptions));
                
                var newMessage = new ChatMessageHistory
                {
                    Role = messages[i].Role,
                    Content = content,
                    ToolCallId = messages[i].ToolCallId,
                    Id = messages[i].Id,
                    ToolCalls = messages[i].ToolCalls
                };
                messages[i] = newMessage;
            }
        }
    }
    
    private string CleanMessage(string message)
    {
        // Remove specific Unicode escape sequences
        message = System.Text.RegularExpressions.Regex.Replace(message, @"\\u[0-9a-fA-F]{4}", "");
    
        // Trim quotes
        message = message.Trim('"');
    
        return message;
    }
    
    private async Task<List<ChatMessageHistory>> GetMessages(ServiceRequest request)
    {
        var cachedMessages = await MessageCache.GetCachedMessages(request.ConversationId);
        var messages = cachedMessages ?? request.Messages.ToList();
        var systemPrompt = AddContext(request.SystemPrompt, request.ConversationId);

        if (!messages.Any())
        {
            messages = new List<ChatMessageHistory>
            {
                new () { Role = "system", Content = systemPrompt},
                new () { Role = "user", Content = request.UserPrompt}
            };
        }
        else
        {
            messages = messages.Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(systemPrompt) && messages.All(a => a.Role != "system"))
            {
                messages.Add(new ChatMessageHistory{ Role = "system", Content = systemPrompt});   
            }
            if (!string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                var lastMessage = messages.Last();
                if (lastMessage.Role.ToLower() == "user" && lastMessage.Content is JsonElement jsonElement && 
                    jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var contentList = jsonElement.EnumerateArray().ToList();
                    contentList.Add(JsonSerializer.SerializeToElement(new 
                    {
                        type = "text", 
                        text = request.UserPrompt 
                    }, _serializerOptions));
                    messages.Last().Content = contentList;
                }
                else
                {
                    messages.Add(new ChatMessageHistory{ Role = "user", Content = request.UserPrompt });
                }
            }
        }

        NormalizeMessages(ref messages);
        return messages;
    }

    /// <summary>
    /// Retrieves all files and deletes those that have expired
    /// </summary>
    /// <returns>A summary of deletion operations</returns>
    private async Task<FileCleanupResult> CleanupExpiredFilesAsync(ServiceConfig config)
    {
        var result = new FileCleanupResult();

        try 
        {
            var files = await ListFilesAsync(config.OpenAiUrl);
            
            var now = DateTimeOffset.UtcNow;

            // Filter and process expired files
            var expiredFiles = files
                .Where(f => 
                    f.ExpiresAt.HasValue && 
                    DateTimeOffset.FromUnixTimeSeconds(f.ExpiresAt.Value) < now)
                .ToList();

            // Delete each expired file
            foreach (var file in expiredFiles)
            {
                if (await DeleteFileAsync(file.Id, config.OpenAiUrl))
                {
                    result.SuccessfullyDeletedFiles.Add(file);
                }
                else
                {
                    result.FailedDeletionFiles.Add(file);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Lists all files in the OpenAI account
    /// </summary>
    private async Task<List<FileResponse>> ListFilesAsync(string apiUrl)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{apiUrl}/files");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var fileListResponse = JsonSerializer.Deserialize<FileListResponse>(responseContent);
            return fileListResponse.Data;
        }
        else
        {
            throw new HttpRequestException($"Failed to list files: {responseContent}");
        }
    }

    /// <summary>
    /// Deletes a specific file by its ID
    /// </summary>
    private async Task<bool> DeleteFileAsync(string apiUrl, string fileId)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.DeleteAsync($"{apiUrl}/files/{fileId}");
        return response.IsSuccessStatusCode;
    }

    
    private string AddContext(string systemPrompt, string conversationId)
    {
        if (!string.IsNullOrEmpty(systemPrompt) && !systemPrompt.Contains("<context>"))
        {
            systemPrompt += $@"
                <context>{Environment.NewLine}
                    Conversation Id: {conversationId} {Environment.NewLine}
                    Current Date: {DateTime.Now.ToShortDateString()} {Environment.NewLine}
                    Current Time: {DateTime.Now.ToShortTimeString()} {Environment.NewLine}
                    Timezone: {TimeZoneInfo.Local.Id} {Environment.NewLine}
                </context>
            ";
        }

        return systemPrompt;
    }
}