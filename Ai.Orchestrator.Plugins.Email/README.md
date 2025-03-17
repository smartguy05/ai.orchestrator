**Below is auto-generated documentation created by ChatGPT, it has not been review yet for accuracy**

# Ai.Orchestrator.Plugins.Email

**Ai.Orchestrator.Plugins.Email** is a plugin designed to integrate email sending capabilities into the [Ai.Orchestrator](https://github.com/smartguy05/ai.orchestrator) framework. This plugin provides a consistent interface for email operations, supports multiple email clients (such as **Smtp** and **MimeKit**), and integrates seamlessly with dependency injection modules.

## Table of Contents

1. [Features](#features)
2. [Requirements](#requirements)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Usage](#usage)
    - [Registering the Plugin](#registering-the-plugin)
    - [Sending an Email](#sending-an-email)
6. [Available Email Clients](#available-email-clients)
7. [Validation](#validation)
8. [Contributing](#contributing)
9. [License](#license)
---

## Features

- **Multiple Email Providers**: Uses either **Smtp** (via `System.Net.Mail`) or **MimeKit** for sending emails.
- **Flexible Configuration**: Easily switch between email providers by changing settings.
- **Dependency Injection**: Offers a preconfigured Autofac module (`EmailModule`) for streamlined integration with Ai.Orchestrator or other .NET apps.
- **Validation**: Uses [FluentValidation](https://docs.fluentvalidation.net/en/latest/) to ensure email client settings are correctly configured.
- **Test Coverage**: Contains unit tests (in `Ai.Orchestrator.Plugins.Email.Tests`) for both the Smtp and MimeKit implementations.

---

## Requirements

- [.NET 6.0 or higher](https://dotnet.microsoft.com/en-us/download/dotnet)
- [Ai.Orchestrator](https://github.com/smartguy05/ai.orchestrator) (optional but recommended for seamless orchestration features)
- [FluentValidation](https://docs.fluentvalidation.net/en/latest/) for setting validations (already included as NuGet references)

---

## Installation

If you are using this as part of the larger **Ai.Orchestrator** solution, the plugin is already included in the repository. If you want to reference it in another solution:

1. Clone or download [this repository](https://github.com/smartguy05/ai.orchestrator).
2. Add the `Ai.Orchestrator.Plugins.Email` project to your `.sln` or reference it via a project reference.
3. Alternatively, if a [NuGet package](https://learn.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio) is published for this plugin, you could add it through the NuGet Package Manager (check your repository’s deployment/packaging instructions).

---

## Configuration

**EmailClientSetting.cs** defines the essential settings for sending email:

```csharp
public class EmailClientSetting
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseSsl { get; set; }
    public string AuthType { get; set; }
    public int Timeout { get; set; }
}
```

**Key properties**:

- **SmtpServer**: The SMTP host address (e.g., `smtp.gmail.com`).
- **Port**: The SMTP port (e.g., `587`).
- **Username** & **Password**: Credentials for authentication.
- **UseSsl**: Indicates whether to use an encrypted connection (true or false).
- **AuthType**: Allows specifying the authentication method (if needed).
- **Timeout**: Connection timeout in milliseconds.

> **Note**: These settings should be stored in the Configs folder and be named the same as the plugin. Eg. Ai.Orchestrator.Plugins.Email.json
Example Email Config File
```json
{
   "name": "email",
   "description": "A plugin to interact with email via smtp and imap",
   "tools": [
      {
         "type": "function",
         "function": {
            "name": "get_email",
            "description": "Use this function to read emails from the user's already set up email account, for example when asked something like 'Who sent that last email?', or 'When did I get that email from Google'",
            "parameters": {
               "type": "object",
               "properties": {
                  "account": {
                     "type": "string",
                     "description": "The email account you want to access. This is optional, if not supplied the default account will be used."
                  },
                  "searchSubject": {
                     "type": "string",
                     "description": "The subject of the email to search for. This is optional."
                  }
               },
               "required": []
            }
         }
      },
      {
         "type": "function",
         "function": {
            "name": "send_email",
            "description": "Use this function to send emails from the user's already set up email account, for example when asked something like 'Send an email to mom telling her I will be late'",
            "parameters": {
               "type": "object",
               "properties": {
                  "account": {
                     "type": "string",
                     "description": "The email account you want to access. This is optional, if not supplied the default account will be used."
                  },
                  "destination": {
                     "type": "string",
                     "description": "The email address to send the email to. This MUST be a valid email address."
                  },
                  "subject": {
                     "type": "string",
                     "description": "The subject of the email to send."
                  },
                  "body": {
                     "type": "string",
                     "description": "The HTML formatted body of the email to send."
                  },
                  "sender": {
                     "type": "string",
                     "description": "The name of the person that sent the email. If left empty the default value stored in settings will be used."
                  }
               },
               "required": ["destination", "subject", "body"]
            }
         }
      },
      {
         "type": "function",
         "function": {
            "name": "delete_email",
            "description": "Use this function to delete emails from the user's already set up email account, for example when asked something like 'delete that email', or 'delete my last email'",
            "parameters": {
               "type": "object",
               "properties": {
                  "account": {
                     "type": "string",
                     "description": "The email account you want to access. This is optional, if not supplied the default account will be used."
                  },
                  "messageId": {
                     "type": "string",
                     "description": "The message Id of the email message to delete"
                  }
               },
               "required": [
                  "messageId"
               ]
            }
         }
      }
   ],
   "toolFunctions": [
      "send_email",
      "get_email",
      "delete_email"
   ],
   "contract": {
      "method": "read,send",
      "account": "string?",
      "destination": "string?",
      "subject": "string?",
      "body": "string?",
      "sender": "string?",
      "searchSubject": "string?"
   },
   "emailAccounts": [
      {
         "name": "main",
         "displayName": "Display name to show in email",
         "default": true,
         "imap": "imap.server.com",
         "imapPort": 993,
         "smtp": "smtp.server.com",
         "smtpPort": 465,
         "username": "email@server.com",
         "email": "email@server.com",
         "password": "password",
         "useSsl": true
      }
   ]
}
```

---

## Usage

### Registering the Plugin

If you’re using **Autofac** within Ai.Orchestrator (or any .NET project), you can register the email plugin by calling the `EmailModule`. Typically, you add this in your `Startup.cs` or wherever you configure your container:

```csharp
using Autofac;
using Ai.Orchestrator.Plugins.Email;

public class Startup
{
    public void ConfigureContainer(ContainerBuilder builder)
    {
        // Register other modules and services

        builder.RegisterModule<EmailModule>(); 
        // This automatically configures IEmailClient and setting validators
    }
}
```

Alternatively, you can register everything manually (for advanced scenarios):

```csharp
builder.RegisterType<SmtpEmailClient>()
       .As<IEmailClient>()
       .InstancePerLifetimeScope();

builder.RegisterType<EmailClientSettingValidators>()
       .AsSelf()
       .SingleInstance();
```

### Sending an Email

After registration, you can inject `IEmailClient` into your classes/services:

```csharp
public class EmailService
{
    private readonly IEmailClient _emailClient;
    private readonly EmailClientSetting _emailSettings;

    public EmailService(IEmailClient emailClient, EmailClientSetting emailSettings)
    {
        _emailClient = emailClient;
        _emailSettings = emailSettings;
    }

    public async Task SendTestEmailAsync()
    {
        var emailMessage = new EmailMessage
        {
            From = "sender@example.com",
            To = "recipient@example.com",
            Subject = "Test Email",
            Body = "This is a test email sent via Ai.Orchestrator.Plugins.Email!"
        };

        var isSent = await _emailClient.SendEmailAsync(emailMessage, _emailSettings);
        if (isSent)
        {
            Console.WriteLine("Email sent successfully!");
        }
        else
        {
            Console.WriteLine("Failed to send email.");
        }
    }
}
```

Where `EmailMessage` typically looks like:

```csharp
public class EmailMessage
{
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    // You can add attachments or other metadata as needed
}
```

---

## Available Email Clients

1. **SmtpEmailClient**
    - Uses **System.Net.Mail** under the hood.
    - Recommended for quick setups or if you rely on standard .NET libraries.

2. **MimeKitEmailClient**
    - Uses the popular **MimeKit** library for more advanced email formatting, attachments, and MIME operations.
    - Great for complex emails (e.g., multi-part MIME, embedded images).

### Switching Email Clients

You can switch between **SmtpEmailClient** and **MimeKitEmailClient** by changing the registration in `EmailModule` or in your own container registration logic:

```csharp
builder.RegisterType<MimeKitEmailClient>()
       .As<IEmailClient>()
       .InstancePerLifetimeScope();
```

---

## Validation

`EmailClientSettingValidators` implements [FluentValidation](https://docs.fluentvalidation.net/en/latest/) to ensure your settings (like `SmtpServer`, `Port`, etc.) are valid. This helps catch configuration issues early.

If you want to manually trigger validation:

```csharp
var validator = new EmailClientSettingValidators();
var validationResult = validator.Validate(myEmailClientSetting);
if (!validationResult.IsValid)
{
    // Handle validation errors
}
```

---

## Contributing

We welcome contributions to improve **Ai.Orchestrator.Plugins.Email**! Whether you want to fix bugs, add features, or enhance documentation:

1. Fork the [repository](https://github.com/smartguy05/ai.orchestrator).
2. Create a new branch for your feature or bugfix.
3. Make your changes and update/add tests if needed.
4. Submit a pull request describing your changes.

Please ensure you adhere to the repository’s coding standards and that all existing tests pass before submitting a PR.

---

## License

This project is licensed under the [MIT License](../LICENSE). See the [LICENSE](../LICENSE) file for details.

---

### Questions or Feedback?

Open an [issue](https://github.com/smartguy05/ai.orchestrator/issues) or start a discussion in the repository. We’re happy to help with setup, usage, or any additional troubleshooting you might need.

Enjoy orchestrating your AI-driven email workflows with **Ai.Orchestrator.Plugins.Email**!