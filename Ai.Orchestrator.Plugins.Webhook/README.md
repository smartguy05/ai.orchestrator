**Below is auto-generated documentation created by ChatGPT, it has not been review yet for accuracy**

# Ai.Orchestrator.Plugins.Webhook

**Ai.Orchestrator.Plugins.Webhook** is a plugin for [Ai.Orchestrator](https://github.com/smartguy05/ai.orchestrator) that enables you to easily integrate inbound HTTP requests (webhooks) into your orchestration workflows. This plugin exposes endpoints that can receive data from external services, parse the payload, and trigger Ai.Orchestrator workflows or actions automatically.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
    - [Registering Webhook Endpoints](#registering-webhook-endpoints)
    - [Calling the Webhook from External Services](#calling-the-webhook-from-external-services)
    - [Retrieving Registered Webhooks](#retrieving-registered-webhooks)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

**Ai.Orchestrator.Plugins.Webhook** lets you link external services to your Ai.Orchestrator workflows by exposing HTTP endpoints that can receive JSON payloads. Upon receiving a webhook (e.g., a `POST` request from GitHub, Stripe, custom apps, etc.), this plugin processes the incoming data and triggers user-defined actions or workflows within Ai.Orchestrator.

By leveraging this plugin, you can:

- Automate tasks whenever an external event occurs.
- Pass relevant data from inbound requests to an Ai.Orchestrator workflow for further processing or decision-making.
- Keep track of all registered webhook endpoints in one place.

---

## Features

- **Automatic Endpoint Registration** – On startup, this plugin registers new endpoints under a specified route (`/plugin/webhook/...`).
- **Configurable** – Allows you to configure plugin settings via JSON configuration.
- **Workflow Integration** – Seamlessly calls Ai.Orchestrator workflows or tasks whenever a webhook arrives.
- **Easy Debugging** – Provides an endpoint to list registered webhooks for inspection or debugging.

---

## Requirements

- [.NET 7.0 (or later)](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Ai.Orchestrator](https://github.com/smartguy05/ai.orchestrator) (the core application in which this plugin runs)

---

## Installation

1. **Clone the Repository (or Add as a Submodule)**
   ```bash
   git clone https://github.com/smartguy05/ai.orchestrator.git
   cd ai.orchestrator/Ai.Orchestrator.Plugins.Webhook
   ```

2. **Build the Plugin**  
   Build the project from the `Ai.Orchestrator.Plugins.Webhook` folder:
   ```bash
   dotnet build
   ```

3. **Include the Plugin DLL in Ai.Orchestrator**
    - Copy the `Ai.Orchestrator.Plugins.Webhook.dll` (from the build output) into your Ai.Orchestrator plugins folder.
    - Or reference it directly in your Ai.Orchestrator solution if you are using a monorepo approach.

---

## Configuration

Ai.Orchestrator typically reads its plugin settings from a JSON file (e.g., `appsettings.json` or `plugin.json`). You can enable the **Webhook** plugin by adding an entry similar to the following:

```jsonc
{
  "Plugins": [
    {
      "runtime": "Ai.Orchestrator.Plugins.Webhook.dll",
      "enabled": true,
      "settings": {
        // Add any plugin-specific settings here if needed
      }
    }
  ]
}
```

> **Note**: The `runtime` property must match the filename of the plugin DLL.  
> **Important**: Make sure `"enabled": true` is set to load the plugin at startup.

---

## Usage

Once the plugin is registered and Ai.Orchestrator is running, the plugin will automatically set up two key endpoints:

1. **Action Endpoint**:  
   **Method**: `POST`  
   **Route**: `/plugin/webhook/action`

2. **Registered Webhooks Endpoint**:  
   **Method**: `GET`  
   **Route**: `/plugin/webhook/registered`

### Registering Webhook Endpoints

In most Ai.Orchestrator-based systems, you define tasks or workflow steps that specify which plugin (and function) to call. For example, you might have a workflow configuration specifying that on receiving a certain payload, you want to parse data from `WebhookRequest` and proceed with some action.

A minimal example might look like this (in pseudo-JSON to conceptualize the workflow configuration):

```jsonc
{
  "workflows": [
    {
      "id": "incoming_webhook_workflow",
      "steps": [
        {
          "pluginId": "webhook",
          "function": "HandleInboundData",
          "parameters": {
            "dataKey": "myImportantField"
          }
        },
        {
          "pluginId": "otherPlugin",
          "function": "DoSomethingWithParsedData"
        }
      ]
    }
  ]
}
```

> The actual syntax depends on how you configure Ai.Orchestrator’s workflows.

### Calling the Webhook from External Services

Any external service can trigger your orchestration workflow by sending a `POST` request to:

```
POST /plugin/webhook/action
```

- **Headers**: Typically, use `Content-Type: application/json`.
- **Body**: Provide the JSON payload that your workflow expects. For example:

  ```json
  {
    "eventName": "order.created",
    "data": {
      "orderId": 12345,
      "amount": 99.99
    }
  }
  ```

The plugin will parse the payload into a `WebhookRequest` object internally and pass it along the configured workflow steps.

### Retrieving Registered Webhooks

You can retrieve a list of all webhooks (or endpoints) recognized by the plugin by making a `GET` request to:

```
GET /plugin/webhook/registered
```

It returns information about how many webhooks are active, or any debug information you may have included in your environment.

---

## Project Structure

Here is an overview of the **Ai.Orchestrator.Plugins.Webhook** folder:

```
Ai.Orchestrator.Plugins.Webhook/
├── WebhookPlugin.cs
├── WebhookService.cs
├── WebhookRequest.cs
├── WebhookReply.cs
├── WebhookConstants.cs
├── WebhookFunctionInfo.cs
├── WebhookUtils.cs
├── WebhookType.cs
├── Tests/
│   └── WebhookPluginTests.cs
├── Dockerfile
└── ... (other .csproj / config files)
```

- **WebhookPlugin.cs** – The main entry point implementing the `AiOrchestratorPlugin` interface and registering routes/services.
- **WebhookService.cs** – Contains the core logic for handling inbound requests and bridging to the Ai.Orchestrator workflow.
- **WebhookRequest.cs / WebhookReply.cs** – Model classes to capture inbound payloads and formulate standardized replies.
- **WebhookUtils.cs** – Common helper functions for the plugin (e.g., payload parsing, validations).
- **WebhookPluginTests.cs** – Unit tests to ensure the plugin runs as intended.
- **Dockerfile** – Optional containerization file for deploying the plugin as part of a containerized Ai.Orchestrator environment.

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the [repository](https://github.com/smartguy05/ai.orchestrator).
2. Create a new branch for your feature/fix.
3. Make your changes, add tests if necessary, and run all existing tests to ensure there are no regressions.
4. Submit a Pull Request with a detailed description of your changes.

---

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT). See the main repository’s [LICENSE](https://github.com/smartguy05/ai.orchestrator/blob/main/LICENSE) file for details.

---

**Need Help?**  
If you have any questions about setting up or using this plugin, please open an [issue](https://github.com/smartguy05/ai.orchestrator/issues) on the main repository.