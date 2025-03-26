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
    public override string Name => "Email";
    public override string Description => "Send/Read email";

    public override async Task<object> DoWork(ServiceRequest serviceRequest, ServiceConfig config, IEnumerable<ToolCall> enumerableToolCalls)
    {
        switch (serviceRequest.Method.ToLower())
        {
            case "get_email":
            {
                var mail = (await GetEmail(serviceRequest, config)).ToList();
                if (!string.IsNullOrWhiteSpace(serviceRequest.SearchSubject))
                {
                    mail = mail
                        .Where(w =>
                            w.Subject.Contains(serviceRequest.SearchSubject, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }
                
                return mail;
            }
            case "send_email":
            {
                var success = await SendEmail(serviceRequest, config);
                return Task.FromResult((object)new
                {
                    Success = success
                });
            }
            case "delete_email":
            {
                var success = await DeleteEmail(serviceRequest, config);
                return Task.FromResult((object)new
                {
                    Success = success
                });
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

            Console.WriteLine($"Mail account {mailAccount.Username} authenticated");

            await client.SendAsync(message);

            Console.WriteLine("Email successfully sent");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending email: {e.Message}");
            throw;
        }
        finally
        {
            client.Disconnect(true);
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
            
            Console.WriteLine($"Mail account {mailAccount.Username} authenticated");
            
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
            
            Console.WriteLine($"{deletedCount} email(s) deleted");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting email: {e.Message}");
            throw;
        }
        finally
        {
            client.Disconnect(true);
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
            
            Console.WriteLine($"Mail account {mailAccount.Username} authenticated");
            
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            Console.WriteLine($"Total messages in INBOX: {client.Inbox.Count}");
            
            var messageCount = Math.Min(client.Inbox.Count, request.MaxReturnedEmails ?? 10);

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
            
            messageCount = distinctEmailIds.Any() ? distinctEmailIds.Count : messageCount;
            Console.WriteLine($"Total messages processing: {messageCount}");

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
            Console.WriteLine($"Error getting email: {e.Message}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
    
    static string GetEmailBody(MimeMessage message)
    {
        // If the email contains multiple parts (multipart), find the text/plain or text/html part
        if (message.Body is TextPart textPart)
        {
            return MinifyContent(textPart.Text);
        }
        else if (message.Body is Multipart multipart)
        {
            foreach (var part in multipart)
            {
                if (part is TextPart tp && tp.IsHtml)
                    return MinifyContent(tp.Text); // Return minified HTML body without CSS if available
                else if (part is TextPart tpPlain)
                    return MinifyContent(tpPlain.Text); // Return minified plain text body
            }
        }

        return string.Empty; // No body found
    }

    static string MinifyContent(string html)
    {
        // Extract content within <body> tags if they exist
        var bodyMatch = System.Text.RegularExpressions.Regex.Match(html, @"<body[^>]*>([\s\S]*?)</body>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (bodyMatch.Success)
        {
            html = bodyMatch.Groups[1].Value;
        }
        
        // Remove special characters
        html = System.Text.RegularExpressions.Regex.Replace(html, @"[\p{C}\u00A0\u2007\u202F]", "");

        // Remove Unicode-encoded HTML fragments
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\\u[0-9,A-Z,a,z]{4,}", "");
        
        // Remove <style> tags and their content
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<style[^>]*>.*?</style>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
    
        // Remove inline style attributes
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+style\s*=\s*""[^""]*""", "");
    
        // Remove image links
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<img[^>]+>", "");
    
        // Remove URLs over 100 characters long
        html = System.Text.RegularExpressions.Regex.Replace(html, @"https?://\S{100,}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove structural HTML tags, keeping only text content
        html = System.Text.RegularExpressions.Regex.Replace(html, @"</?(?:div|span|p|br|table|tr|td|thead|tbody|tfoot)[^>]*>", "");
    
        // Remove excess whitespace
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ").Trim();
    
        // Remove HTML comments
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<!--.*?-->", "");
        
        // Remove wiki style images
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Remove &nbsp;
        html = System.Text.RegularExpressions.Regex.Replace(html, @"&nbsp;", "");

        // Remove DOCTYPE
        html = System.Text.RegularExpressions.Regex.Replace(html, "<!DOCTYPE[^>]*>", "");
        
        // Remove html opening tag
        html = System.Text.RegularExpressions.Regex.Replace(html, "<html[^>]*>", "");
        
        // Remove html close tag
        html = System.Text.RegularExpressions.Regex.Replace(html, "<\\/html>", "");
        
        // Remove body opening tag
        html = System.Text.RegularExpressions.Regex.Replace(html, "<body[^>]*>", "");
        
        // Remove body close tag
        html = System.Text.RegularExpressions.Regex.Replace(html, "<\\/body>", "");
        
        // Remove head opening tag
        html = System.Text.RegularExpressions.Regex.Replace(html, "<head>.+<\\/head>", "");
        
        return html;
    }
}