# Ai.Orchestrator

**Ai.Orchestrator** is a service designed to orchestrate AI-related tasks. There are built-in plugins for 
handling email and webhook requests. The service is extensible using plugins.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Plugins](#plugins)
   - [Example Workflow](#example)
- [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Installation](#installation)
   - [Running the Orchestrator](#running-the-orchestrator)
- [Usage](#usage)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

**Ai.Orchestrator** acts as a central controller for different AI-related tasks:

- **Email**: Read, Send, and Delete emails for the configured email address.
- **Webhook**: Webhook interface for api.
- **OpenAI**: OpenAI API integration. Can be configured to use a local OpenAI compliant API.
- **Plugin/Module Integration**: Easily integrate external modules for additional functionality (logging, notifications, etc.).

Planned:

- (Complete) ~~**Short-term chat memory**: Maintain short-term chat memory for multi-shot prompting~~
- **Task Scheduler**: Manages scheduling for future AI tasks
- **Task Manager**: Handles creation, monitoring, and execution of tasks
- **Event Watcher**: Performs an AI task when a specified event occurs
- **Event Scheduler**: Handles creation, monitoring, and lifecycles of Event Watchers

## Features

- **Modular Architecture**: Add or remove AI-related modules without disrupting the entire system.
- **Pluggable Components**: Swap out scheduling, storage, or model inference modules via configurations.
- **Configurable**: Plugins are configurable by their own config json files

## Plugins

**Ai.Orchestrator** is extensible using plugins placed into the plugins directory as specified in the environment 
variable or launchSettings.json. Plugins should expose an ICommand object which accepts an OrchestratorRequest,
a string containing the json value of the configuration for the plugin, and a list of available tool calls 
which are populated by Orchestrator from the plugin configs. Each plugin can return either an object as a return 
result or another OrchestratorRequest so that more actions may be taken. Using this method a series of commands 
can be made to perform a complex action.

### **Example:**

NOTE: This example is using the TextController endpoint
1. User Request: "Respond to my last email, ask them when we can meet up."
2. The request is passed to the OpenAI compliant API along with a list of the available tool functions.
3. The API responds with a tool call to perform, "get_email", and forwards the request, along with any parameters like date range, subject, etc. to Orchestrator.
4. Orchestrator determines the correct plugin to use and sends the request to the Email Plugin.
5. The Email Plugin will use the values in the config file, as well as the parameters passed to it from the OpenAI tool call, to get the requested email.
6. A new OrchestratorRequest is created (because a tool call id existed in the object) and passed back to Orchestrator.
7. Orchestrator sees that the~~~~ requester is the OpenAI plugin so the information from the Email Plugin is forwarded to the OpenAI Plugin.
8. The OpenAI Plugin takes the data from the Email Plugin, along with the previous prompt information and sends that to the OpenAI compliant API.
9. The API responds with a new tool call to perform, "send_email", and forwards the request, along with any parameters like date range, subject, etc. to Orchestrator.
10. Orchestrator determines the correct plugin to use and sends the request to the Email Plugin.
11. The Email Plugin will use the values in the config file, as well as the parameters passed to it from the OpenAI tool call, to send the requested email. 
12. A new OrchestratorRequest is created (because a tool call id existed in the object) and passed back to Orchestrator with a true (email sent) or false (didn't send) value.
13. Orchestrator sees that the requester is the OpenAI plugin so the information from the Email Plugin is forwarded to the OpenAI Plugin.
14. The OpenAI Plugin takes the data from the Email Plugin, along with the previous prompt information and sends that to the OpenAI compliant API.
15. The API responds that it was successful (or not) based on the value returned from the Email Plugin then returns a message stating success or not. 

Example Request:
```
{
  "userPrompt": "Respond to my last email. Ask them about meeting up on Friday.",
  "systemPrompt": "You are a helpful assistant. Your job is to help me with handling emails.",
  "conversationId": null
}
```
*conversationId is null unless you are continuing an in progress conversation. A successful result will return a Conversation Id you can use to do multi-shot prompting instead of single shot as shown in this example*

Example Successful Response:
```
{
  "conversationId": "da8a5ffd-7a1e-4abe-8c07-6399e130c21d",
  "result": "I have sent the reply to Jordan Reynolds requesting a meeting on Friday."
}
```

## Getting Started

### Prerequisites

- **.NET 7+** (or whichever version your project supports)
- A modern **IDE** or text editor (e.g., Visual Studio, Rider, VS Code)
- Basic knowledge of C# and .NET Core

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/smartguy05/ai.orchestrator.git
   ```

2. **Navigate into the Project**
   ```bash
   cd ai.orchestrator/Ai.Orchestrator
   ```

3. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```
### Running the Orchestrator
`dotnet run`

---

## Usage

- **Orchestrator.cs**: The main service class that orchestrates different tasks and modules.

PLANNED (2025)
- **TaskScheduler.cs**: Manages scheduling for AI tasks.
- **TaskManager.cs**: Handles creation, monitoring, and execution of tasks.

## Configuration

Configuration is handled by environment variables or launchSettings.json 
```
   "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "PluginDirectory": "Plugins",
        "ConfigDirectory": "Configs",
        "ActivePlugins": "Test_Plugin,Ai.Orchestrator.Plugins.Webhook,Ai.Orchestrator.Plugins.Email,Ai.Orchestrator.Plugins.UseMemos,Ai.Orchestrator.Plugins.GoogleCalendar,Ai.Orchestrator.Plugins.OpenAi"
      }
```

Plugin configuration is handled by each plugin with json files in the ConfigDirectory location

---

## Contributing

1. **Fork** the repository
2. **Create a new branch** (`feature/xyz`)
3. **Commit your changes**
4. **Create a Pull Request** describing your changes in detail

We welcome bug reports, feature requests, and pull requests from the community!

---

## License

This repository is licensed under the [MIT License](LICENSE). Feel free to use and modify this project in accordance with the terms of the license.

---