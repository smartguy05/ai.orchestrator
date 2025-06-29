using System.Text.RegularExpressions;
using System.Web;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Enums;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.Email.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;

namespace Ai.Orchestrator.Plugins.Email;

public class EmailCommand: CommandBase<ServiceRequest,ServiceConfig>
{
    public override string Name => "Ai.Orchestrator.Plugins.Email";
    public override string Description => "Send/Read email";
    protected override IConfirmationService ConfirmationService { get; set; }

    protected override async Task<object> DoWork(ServiceRequest serviceRequest, ServiceConfig config, IEnumerable<ToolCall> enumerableToolCalls)
    {
        switch (serviceRequest.Method.ToLower())
        {
            case "get_email":
            {
                var mail = (await GetEmail(serviceRequest, config)).ToList();
                var count = mail.Count;
                if (!string.IsNullOrWhiteSpace(serviceRequest.SearchSubject))
                {
                    mail = mail
                        .Where(w =>
                            w.Subject.Contains(serviceRequest.SearchSubject, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                if (serviceRequest.MaxReturnedEmails < mail.Count)
                {
                    mail = mail.Take(serviceRequest.MaxReturnedEmails).ToList();
                }

                return new
                {
                    Success = true,
                    Total = count,
                    Result = mail
                };
            }
            case "send_email":
            {
                if (string.IsNullOrWhiteSpace(serviceRequest.ConfirmationId))
                {
                    var confirmation = new Confirmation
                    {
                        ConfirmationMessage = "Are you sure you want to send this email?",
                        Content = serviceRequest.Body,
                        Options = new Dictionary<string, bool>
                        {
                            { "Yes", true },
                            { "No", false }
                        },
                        Id = Guid.NewGuid()
                    };
                    serviceRequest.ConfirmationId = confirmation.Id.ToString();
                    var request = new OrchestratorRequest
                    {
                        Service = Name,
                        ServiceRequest = serviceRequest
                    };
                    var confirmationRequest = await ConfirmationService.RequestConfirmation(confirmation, request);
                    var isSuccessful = (bool?)confirmationRequest.GetType().GetProperty("Success")?.GetValue(confirmationRequest) ?? false;
                    if (isSuccessful)
                    {
                        return new
                        {
                            Success = true,
                            ConfirmationId = confirmation.Id.ToString()
                        };    
                    }
                    
                    return new
                    {
                        Success = false
                    };
                }

                if (ConfirmationService.DoesConfirmationExist(Guid.Parse(serviceRequest.ConfirmationId), out _))
                {
                    var success = await SendEmail(serviceRequest, config);
                    return new
                    {
                        Success = success
                    };
                }

                return new
                {
                    Success = false,
                    Error = "Unable to send email without valid confirmation"
                };
            }
            case "delete_email":
            {
                if (string.IsNullOrWhiteSpace(serviceRequest.ConfirmationId))
                {
                    return await ConfirmationService.RequestConfirmation(
                        new Confirmation
                        {
                            ConfirmationMessage = "Are you sure you want to delete this email?",
                            Content = serviceRequest.Body,
                            Options = new Dictionary<string, bool>
                            {
                                { "Yes", true },
                                { "No", false }
                            }
                        },
                        new OrchestratorRequest
                        {
                            Service = Name,
                            ServiceRequest = serviceRequest
                        });
                }

                if (ConfirmationService.DoesConfirmationExist(Guid.Parse(serviceRequest.ConfirmationId), out _))
                {
                    var success = await DeleteEmail(serviceRequest, config);
                    return new
                    {
                        Success = success
                    };
                }

                return new
                {
                    Success = false,
                    Error = "Unable to send email without valid confirmation"
                };
            }
            default:
            {
                throw new Exception("Invalid email method specified");
            }
        }
    }

    private EmailParameters GetMailAccount(ServiceRequest request, ServiceConfig config)
    {
        var mailAccount = config.EmailAccounts.FirstOrDefault(f =>
            string.Equals(f.Name, request.Account, StringComparison.InvariantCultureIgnoreCase));
        mailAccount ??= config.EmailAccounts.FirstOrDefault(f => f.Default);
        if (mailAccount is null)
        {
            throw new Exception("No mail accounts found!");
        }
        return mailAccount;
    }

    private async Task<bool> SendEmail(ServiceRequest request, ServiceConfig config)
    {
        var mailAccount = GetMailAccount(request, config);

        var message = new MimeMessage
        {
            Subject = request.Subject,
            Body = new TextPart("html")
            {
                Text = request.Body
            }
        };
        message.From.Add(new MailboxAddress(mailAccount.DisplayName, mailAccount.Email));
        message.To.Add(new MailboxAddress(request.RecipientName, request.To));

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(mailAccount.Smtp, mailAccount.SmtpPort);
            await client.AuthenticateAsync(mailAccount.Username, mailAccount.Password);

            await Log(LogLevel.Info, $"Mail account {mailAccount.Username} authenticated");

            await client.SendAsync(message);

            await Log(LogLevel.Info, "Email successfully sent");
        }
        catch (Exception e)
        {
            await Log(LogLevel.Error, $"Error sending email: {e.Message}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }

        return true;
    }

    private async Task<int> DeleteEmail(ServiceRequest request, ServiceConfig config)
    {
        var mailAccount = GetMailAccount(request, config);
        int deletedCount = 0;

        using var client = new ImapClient();
        try
        {
            await client.ConnectAsync(mailAccount.Imap, mailAccount.ImapPort, mailAccount.UseSsl);
            await client.AuthenticateAsync(mailAccount.Username, mailAccount.Password);
            
            await Log(LogLevel.Info, $"Mail account {mailAccount.Username} authenticated");
            
            await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
            
            // Delete emails by MessageId using HeaderContains
            if (string.IsNullOrWhiteSpace(request.MessageId))
            {
                throw new Exception("MessageId is required to delete emails");
            }

            var uids = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.HeaderContains("Message-Id", request.MessageId));
            foreach (var uid in uids.UniqueIds)
            {
                await client.Inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true);
                deletedCount++;
            }

            await client.Inbox.ExpungeAsync();
            
            await Log(LogLevel.Info,$"{deletedCount} email(s) deleted");
        }
        catch (Exception e)
        {
            await Log(LogLevel.Error,$"Error deleting email: {e.Message}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }

        return deletedCount;
    }
    
    private async Task<IEnumerable<MailMessage>> GetEmail(ServiceRequest request, ServiceConfig config)
    {
        var mailAccount = GetMailAccount(request, config);
        
        using var client = new ImapClient();
        try
        {
            await client.ConnectAsync(mailAccount.Imap, mailAccount.ImapPort, mailAccount.UseSsl);
            await client.AuthenticateAsync(mailAccount.Username, mailAccount.Password);
            
            await Log(LogLevel.Info, $"Mail account {mailAccount.Username} authenticated");
            
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            await Log(LogLevel.Debug, $"Total messages in INBOX: {client.Inbox.Count}");
            
            var emailIds = new List<UniqueId>();
            if (!string.IsNullOrWhiteSpace(request.MessageId))
            {
                var emails = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.HeaderContains("Message-Id", request.MessageId));
                emailIds.AddRange(emails.UniqueIds);
            } else if (request.UnreadOnly) 
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.NotSeen));
            } else if (!string.IsNullOrWhiteSpace(request.Sender))
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.FromContains(request.Sender)));
            } else if (!string.IsNullOrWhiteSpace(request.Subject))
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.SubjectContains(request.Subject)));
            } else if (!string.IsNullOrWhiteSpace(request.To))
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.ToContains(request.To)));
            } else if (!string.IsNullOrWhiteSpace(request.EmailsSentAfter))
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.SentSince(DateTime.Parse(request.EmailsSentAfter))));
            } else if (!string.IsNullOrWhiteSpace(request.EmailsSentBefore))
            {
                emailIds.AddRange(await client.Inbox.SearchAsync(SearchQuery.SentSince(DateTime.Parse(request.EmailsSentBefore))));
            }

            var distinctEmailIds = emailIds.Distinct().ToList();
            
            var messageCount = client.Inbox.Count;
            messageCount = Math.Min(distinctEmailIds.Any() ? distinctEmailIds.Count : messageCount, request.MaxReturnedEmails);
            await Log(LogLevel.Debug, $"Total messages processing: {messageCount}");

            List<MailMessage> messages = new();
            if (distinctEmailIds.Any())
            {
                foreach (var uniqueId in distinctEmailIds)
                {
                    var message = await client.Inbox.GetMessageAsync(uniqueId);
                    messages.Add(GetMailMessage(message));
                }
            }
            else
            {
                for (var i = 0; i < messageCount; i++)
                {
                    var message = await client.Inbox.GetMessageAsync(i);
                    messages.Add(GetMailMessage(message));
                }
            }

            return messages;

            MailMessage GetMailMessage(MimeMessage mimeMessage)
            {
                return new MailMessage
                {
                    Body = GetEmailBody(mimeMessage),
                    HasAttachments = mimeMessage.Attachments is not null,
                    MessageId = mimeMessage.MessageId,
                    Date = mimeMessage.Date,
                    Subject = mimeMessage.Subject,
                    Priority = mimeMessage.Priority,
                    Sender = mimeMessage.Sender?.Address,
                    From = string.Join(", ", mimeMessage.From?.Select(s => s.Name) ?? new List<string>()),
                    ReplyTo = string.Join(", ", mimeMessage.ReplyTo?.Select(s => s.Name) ?? new List<string>()),
                    To = string.Join(", ", mimeMessage.To?.Select(s => s.Name) ?? new List<string>()),
                    Bcc = string.Join(", ", mimeMessage.Bcc?.Select(s => s.Name) ?? new List<string>())
                };
            }
        }
        catch (Exception e)
        {
            await Log(LogLevel.Error, $"Error getting email: {e.Message}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
    
    static string GetEmailBody(MimeMessage message)
    {
        // Prefer plain text over HTML
        if (message.Body is Multipart multipart)
        {
            // First, look for a plain text part
            var plainTextPart = multipart
                .OfType<TextPart>()
                .FirstOrDefault(p => !p.IsHtml);

            if (plainTextPart != null)
            {
                return CleanPlainText(plainTextPart.Text);
            }

            // If no plain text, try to extract text from HTML
            var htmlPart = multipart
                .OfType<TextPart>()
                .FirstOrDefault(p => p.IsHtml);

            if (htmlPart != null)
            {
                return ExtractTextFromHtml(htmlPart.Text);
            }
        }
        else if (message.Body is TextPart textPart)
        {
            return textPart.IsHtml 
                ? ExtractTextFromHtml(textPart.Text) 
                : CleanPlainText(textPart.Text);
        }

        return string.Empty;
    }

    static string CleanPlainText(string text)
    {
        // Remove excess whitespace
        return Regex
            .Replace(text.Trim(), @"\s+", " ");
    }

    static string ExtractTextFromHtml(string html)
    {
        // Use HtmlAgilityPack to extract text (you'll need to add the NuGet package)
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        var text = doc.DocumentNode.InnerText.Trim();
        return CleanHtmlEmail(text);
    }
    
    static string CleanHtmlEmail(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
    
        // Replace double-escaped Unicode sequences
        input = Regex.Replace(input, @"\\u([0-9a-fA-F]{4})", match => 
        {
            // Convert the hex value to an integer
            var charCode = Convert.ToInt32(match.Groups[1].Value, 16);
            // Convert to the actual character
            return char.ConvertFromUtf32(charCode);
        });
    
        // First decode HTML entities
        var decoded = HttpUtility.HtmlDecode(input);
        
        // Remove URLs with comprehensive pattern
        // This matches http/https/ftp URLs, www addresses, and common TLDs
        decoded = Regex.Replace(
            decoded, 
            @"(https?|ftp)://[^\s/$.?#].[^\s]*|www\.[^\s/$.?#].[^\s]*|[^\s@]+\.(com|net|org|edu|gov|mil|co|io|app|dev|me|info|biz)[^\s,.:;""')}]*",
            ""
        );
    
        // Remove control characters and zero-width characters
        decoded = Regex.Replace(decoded, @"[\u034F\u00AD\u200B-\u200F\u2028-\u202F]+", "");
    
        // Remove excessive whitespace characters
        decoded = Regex.Replace(decoded, @"[\r\n\t]+", " ");
    
        // Remove multiple spaces
        decoded = Regex.Replace(decoded, @"\s+", " ");
    
        // Trim the result
        return decoded.Trim();
    }
}