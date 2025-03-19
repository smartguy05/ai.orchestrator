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
- **Request Stream**: Use stream to allow updating periodically to the user
- **File Upload**: Allow file upload for context/plugin purposes
- **RAG**: Add RAG functionality using [Support Channel KB](https://github.com/smartguy05/support_channel_kb) (soon to be made open source)
- **Long-term chat memory**: Remember details about the user to help with context in future requests
- **Request Security**: Validate user or use different configs based on user
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

### **Example Process using plugins:**

NOTE: This example is using the TextController endpoint
1. User Request:
```
{
  "userPrompt": "Respond to my last email, ask them when we can meet up.",
  "systemPrompt": "You are a helpful assistant. Your job is to help me with handling emails.",
  "conversationId": null
}
```
*conversationId is null unless you are continuing an in progress conversation. A successful result will return a Conversation Id you can use to do multi-shot prompting instead of single shot as shown in this example*

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

15. The API responds that it was successful (or not) based on the value returned from the Email Plugin then returns a message stating success or not:
```
{~~~~
  "conversationId": "da8a5ffd-7a1e-4abe-8c07-6399e130c21d",
  "result": "I have sent the reply to Jordan Reynolds requesting a meeting on Friday."
}
```

## Getting Started

### Prerequisites

- **.NET 7+** (or whichever version your project supports)
- A modern **IDE** or text editor (e.g., Visual Studio, Rider, VS Code)
- Basic knowledge of C# and .NET Core
- Instance of Redis running (I use docker desktop on Windows)

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

### Creating Plugins

- Edit .csproj file, add EnableDynamicLoading

`<EnableDynamicLoading>true</EnableDynamicLoading>`

- Example csproj settings:
  ```
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net7.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <EnableDynamicLoading>true</EnableDynamicLoading>
    </PropertyGroup>
    <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
      <OutputPath>..\..\Ai.Orchestrator\bin\Debug\net7.0\Plugins</OutputPath>
    </PropertyGroup>
    <PropertyGroup>
        <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
        <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    </PropertyGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\Ai.Orchestrator.Common\Ai.Orchestrator.Common.csproj">
        </ProjectReference>
        <ProjectReference Include="..\..\Ai.Orchestrator.Models\Ai.Orchestrator.Models.csproj">
            <Private>false</Private>
            <ExcludeAssets>runtime</ExcludeAssets>
        </ProjectReference>
    </ItemGroup>
  </Project>
    ```

- Example Plugin Configs
  - Test_Plugin
  ```
  {
      "name": "test",
      "description": "A plugin to test orchestrator",
      "contract": { },
      "tools": [
        {
          "type": "function",
          "function": {
            "name": "test_plugin",
            "description": "A test plugin to see if plugins are working. You should not use this plugin unless specifially asked to do so.",
            "parameters": {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "description": "A test name value"
                },
                "value": {
                  "type": "string",
                  "description": "A test description value"
                }
              },
              "required": []~~~~
            } 
          }
        }
      ],
      "toolFunctions": [
        "test_plugin"
      ],
      "testName": "Test Name",
      "testName2": "Test Name 2"
  }
  ```
- Ai.Orchestrator.Plugins.Webhook
  ```
    {
      "name": "webhook",
      "description": "A plugin for calling configured webhooks",
      "contract": { },
      "tools": [
          {
              "type": "function",
              "function": {
                  "name": "webhook",
                  "description": "Use this function to call a specified webhook.",
                  "properties": {
                      "type": "object",
                      "properties": {
                          "name": {
                              "type": "string",
                              "description": "The name of the webhook to use"
                          },
                          "value": {
                              "type": "string",
                              "description": "The value to send to the webhook"
                          }
                      },
                      "required": ["name","value"]
                  }
              }
          }
      ],
      "toolFunctions": [
          "webhook"
      ],
      "webhooks": [
          {
              "name": "HomeAssistant",
              "url": "http://homeassistant_ip:port/api/webhook/webhook-id"
          }	
      ]
  }~~~~
  ```
- Ai.Orchestrator.Plugins.OpenAI
    ```
    {
        "name": "openai",
        "contract": {},
        "description": "Open AI integration",
        "toolFunctions": [],
        "openAiApiKey": "sk-proj-your-key-here",
        "openAiUrl": "https://api.openai.com/v1/chat/completions",
        "redisConnectionString": "localhost:6379"
    }
    ```
~~~~
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
