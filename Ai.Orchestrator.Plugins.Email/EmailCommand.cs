using Ai.Orchestrator.Common.Extensions;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Plugins.Email.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MimeKit;

namespace Ai.Orchestrator.Plugins.Email;

public class EmailCommand: ICommand
{
    public string Name => "Email";
    public string Description => "Send/Read email";

    public async Task<object> Execute(object request, string configString)
    {
        var serviceRequest = request.GetServiceRequest<ServiceRequest>();
        var config = configString.ReadConfig<ServiceConfig>();
        
        if (!string.Equals(serviceRequest.Method, "read", StringComparison.InvariantCultureIgnoreCase) &&
            !string.Equals(serviceRequest.Method, "send", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception("Invalid email method specified");
        }

        if (string.Equals(serviceRequest.Method, "read", StringComparison.InvariantCultureIgnoreCase))
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
            
        var success = SendEmail(serviceRequest, config);
        return Task.FromResult((object)new
        {
            Success = success
        });
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
        message.To.Add(new MailboxAddress(request.RecipientName, request.Destination));

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
            var messageCount = client.Inbox.Count;
            
            Console.WriteLine($"Total messages in INBOX: {messageCount}");

            List<MailMessage> messages = new();
            for (var i = 0; i < messageCount; i++)
            {
                var inboxMessage = await client.Inbox.GetMessageAsync(i);
                var body = GetEmailBody(inboxMessage);
                var hasAttachments = false;
                List<string> attachments = new();
                
                if (inboxMessage.Attachments != null && inboxMessage.Attachments.Any())
                {
                    hasAttachments = true;
                    attachments.AddRange(inboxMessage.Attachments.Select(attachment => attachment is MimePart part ? part.FileName : "unknown"));
                }
                
                var message = new MailMessage
                {
                    Body = body,
                    HasAttachments = hasAttachments,
                    Attachments = attachments,
                    MessageId = inboxMessage.MessageId,
                    Date = inboxMessage.Date,
                    Subject = inboxMessage.Subject,
                    Priority = inboxMessage.Priority,
                    Sender = inboxMessage.Sender?.Address,
                    From = string.Join(", ", inboxMessage.From?.Select(s => s.Name) ?? new List<string> ()),
                    ReplyTo = string.Join(", ", inboxMessage.ReplyTo?.Select(s => s.Name) ?? new List<string> ()),
                    To = string.Join(", ", inboxMessage.To?.Select(s => s.Name) ?? new List<string> ()),
                    Bcc = string.Join(", ", inboxMessage.Bcc?.Select(s => s.Name) ?? new List<string> ())
                };
                
                messages.Add(message);
            }
            
            return messages;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting email: {e.Message}");
            throw;
        }
        finally
        {
            client.Disconnect(true);
        }
    }
    
    static string GetEmailBody(MimeMessage message)
    {
        // If the email contains multiple parts (multipart), find the text/plain or text/html part
        if (message.Body is TextPart textPart)
        {
            return textPart.Text;
        }
        else if (message.Body is Multipart multipart)
        {
            foreach (var part in multipart)
            {
                if (part is TextPart tp && tp.IsHtml)
                    return tp.Text; // Return HTML body if available
                else if (part is TextPart tpPlain)
                    return tpPlain.Text; // Return plain text body if available
            }
        }

        return string.Empty; // No body found
    }
}