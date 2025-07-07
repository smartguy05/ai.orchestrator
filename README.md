# Ai.Orchestrator

**Ai.Orchestrator** is a modular, extensible orchestration platform designed to empower AI agents with robust, plugin-driven access to email, webhooks, notifications, scheduling, memory, and much more. It provides a central controller for automating and coordinating AI-related tasks across multiple domains and services.

---

## Table of Contents

- [What's New](#whats-new)
- [Roadmap](#roadmap)
- [Overview](#overview)
- [Features](#features)
- [Plugins](#plugins)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Running the Orchestrator](#running-the-orchestrator)
- [Configuration](#configuration)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

---

## Roadmap

| Feature | Description | Planned Release |
|--- | --- | --- |
| **Logging** |  |  |
| Improved Message Logging | Log each message in order for easy troubleshooting | September 2025 |
| Per-Agent Conversation History | Save conversation history with each agent for later retrieval | Q4 2025 |
| Agent Tool Calling Logging | Log each tool call with the prompt that started it and each response | TBD |
| Correlation Ids | Add Correlation Ids to logs to help with issue tracking | TBD |
| **A2A** |  |  |
| Per-Agent toolset | Each "mini-agent" can have a specific set of user-configured tools | September 2025 |
| Discovery | How to discover agents | TBD |
| Shared Workspace | Share context between agents | TBD |
| Task delegation | Delegate tasks to other agents then recieve asynchronous response | TBD |
| **Agent Scheduling & Automation** |  |  |
| User configurable Web-Hooks | Allow user to configure webhooks | TBD 2025 |
| Background Task Queuing & Prioritization | Create a queue of pending tasks which the assigned agent can choose prioritize and complete | TBD 2026 |
| Manage scheduled tasks | Allow user to view/edit/create/delete | TBD |
| Error Handling & Retry | Report errors and show status. Allow retrying tasks | TBD |
| **Setup** |  |  |
| Database configs | Store configs in database | July 2025 |
| Multi-user | Using database, allow multiple users with individual configurations | August 2025 |
| First time set-up | Walk through setting up initial settings. creating agents, and choosing the tools available to the agent | TBD 2025 |
| API Key management | Manage API Keys for plugins | TBD 2025 |
| Plugin Marketplace | A marketplace where users can select the plugins they want and add/remove them | TBD 2026 |
| **Security** |  |  |
| Sandboxed tool execution | All agents run their own tools in their own memory space | TBD 2026 |
| Permissions | User/Agent permissions | TBD 2026 |
| Tool Limits | Limit tool usage per agent | TBD 2026 |
| OAuth2 | OAuth2 integration | TBD |
| **Memory Optimization** |  |  |
| Automatic Memory Summarization | Summarize conversation history automatically when the conversation starts to get too long | TBD 2025/2026 |
| RAG Relevance Scoring | Score Vector DB data for better RAG retrieval | TBD 2026 |
| Memory Cleanup & Archving | Clean up Vector DB of stale/ out of date data | TBD 2026 |
| **Agent Performance Dashboard** |  |  |
| Real-time Agent Status | Show current agent status, what it's working on, current log stream | TBD 2026 |
| Resource Usage Monitoring | Show tokens, API calls, execution time, per Agent | TBD 2026 |
| Cost Tracking | Track API costs per Agent | TBD 2026 |
| Success/Failure Tracking | Track how often the Agent is successful | TBD 2026 |
| **Mobile** |  |  |
| Android Notifications | Send notifications to android devices using Firebase Android notification | July 2025 |
| Voice | Speak with agent using your voice | TBD |
| **Misc** |  |  |
| Agent Testing | Framwork for testing agents | TBD 2026 |

## What's New

Recent substantial changes and newly added functionality include:

- **Notification Service**: Added a simple text notification service for outbound alerts and updates. ([#25](https://github.com/smartguy05/ai.orchestrator/pull/25))
- **Confirmation Service**: Enhanced support for user confirmation flows—now supports sending confirmation prompts without requiring options and improved handling in Email workflows. ([#24](https://github.com/smartguy05/ai.orchestrator/pull/24), [#23](https://github.com/smartguy05/ai.orchestrator/pull/23), [#22](https://github.com/smartguy05/ai.orchestrator/pull/22))
- **Plugin Service Performance**: Improved plugin service performance by keeping plugin instances alive and resolving multiple issues with plugin lifecycle management. ([#22](https://github.com/smartguy05/ai.orchestrator/pull/22))
- **Logging Plugins**: Custom logging plugins are now supported—extend the `ILoggingPlugin` interface and configure via dedicated JSON files. Multiple logging plugins are supported in parallel. ([#21](https://github.com/smartguy05/ai.orchestrator/pull/21), [#19](https://github.com/smartguy05/ai.orchestrator/pull/19))
- **Mini-Agent System**: Introduced "mini agents" with configurable toolsets per agent, advancing towards agent-to-agent (A2A) delegation and shared context. ([#20](https://github.com/smartguy05/ai.orchestrator/pull/20))
- **Task Scheduler**: Core support for scheduling complex, multi-step AI tasks triggered by time or events. ([#17](https://github.com/smartguy05/ai.orchestrator/pull/17))
- **Memory Improvements**: Enhanced long-term and short-term memory modules for richer conversational and contextual awareness. ([#18](https://github.com/smartguy05/ai.orchestrator/pull/18))
---

## Overview

Ai.Orchestrator enables you to:

- Centralize and automate AI task execution using a flexible plugin architecture.
- Integrate email, webhooks, notification, scheduling, logging, memory, and more—out of the box or via custom plugins.
- Provide per-agent tool configuration, conversation memory, and event-driven task automation.

---

## Features

- **Modular & Extensible**: Easily add/remove plugins (email, webhook, notifications, calendar, logging, etc.) without disrupting the system.
- **Agent-Oriented**: Assign mini-agents with configurable toolsets and track their conversations and activities.
- **Pluggable Components**: Swap out storage, scheduling, or inference modules.
- **Rich Memory**: Short- and long-term conversational memory, with RAG/KB support.
- **Task Scheduling**: Schedule complex, multi-step workflows for future or recurring execution.
- **Notification & Confirmation**: Built-in services for user notifications and confirmations.
- **Logging & Monitoring**: Multi-plugin logging support, auditing, and monitoring of agent/tool activity.
- **Webhooks & Integrations**: Expose REST endpoints to trigger orchestrator workflows from external services.
- **Secure & Configurable**: Per-plugin configuration with support for API keys, OAuth, and more.

---

## Plugins

Ai.Orchestrator comes with a suite of official plugins, with support for third-party/community plugins as well:

- **Email Plugin**  
  Read, send, and delete emails using SMTP/IMAP and configurable providers. Includes validation, autofac support, and OpenAI tool integration.

- **Webhook Plugin**  
  Register HTTP endpoints that trigger orchestrator workflows from external events/services.

- **Notification Plugin**  
  Manage confirmation flows for user input or action approval or send text notifications to users or external systems.
  
- **Logging Plugins**  
  Support for multiple logging providers, including file and custom logging plugins.

- **Task Scheduler**  
  Schedule and manage future AI tasks and workflows.

- **Memory Plugins**  
  Support both short-term (chat context) and long-term (user data, conversations) memory.

- **RAG, Google Calendar, Python Runner, Memos, Web Search, Home Assistant Assist**  
  Expand system capabilities to knowledge base retrieval, calendar, scripting, note-taking, web search, and smart home control.

*For more details on each plugin, see the respective plugin README or the [Plugins Directory](./Ai.Orchestrator.Plugins).*

---

## Getting Started

### Prerequisites

- [.NET 8+](https://dotnet.microsoft.com/en-us/download/dotnet)
- Redis (for memory, e.g. via Docker Desktop)
- ChromaDB (for long-term memory, e.g. via Docker Desktop)
- Modern IDE (VS, Rider, VS Code)
- Basic C#/.NET Core knowledge

### Installation

```bash
git clone https://github.com/smartguy05/ai.orchestrator.git
cd ai.orchestrator/Ai.Orchestrator
dotnet restore
dotnet build
```

### Running the Orchestrator

```bash
dotnet run
```

---

## Configuration

- **Main orchestrator settings**: Environment variables or `launchSettings.json`.
- **Plugin configs**: Each plugin is configured via its own JSON file in the config directory (see plugin README for schema).
- **Key environment variables**:  
  - `PluginDirectory` — where plugins are loaded from  
  - `ConfigDirectory` — where plugin configs are stored  
  - `ActivePlugins` — comma-separated list of enabled plugins

*See example config files in the repo for reference.*

---

## Usage

- Use the **Text Controller** endpoint to chat or issue requests to the orchestrator with multi-shot conversation support.
- Use the **Webhook Controller** to receive and process HTTP event triggers.
- Use the **Scheduled Task Controller** to view, edit, and manage scheduled tasks.
- Test plugins directly via the **Data Controller**.
- Plugin commands and tool calls can be chained for complex workflows.

---

## Contributing

1. **Fork** the repository.
2. **Create a branch** for your feature/bugfix.
3. **Commit** your changes.
4. **Open a Pull Request** with a clear description.

All contributions, bug reports, and feature requests are welcome!

---

## License

This repository is licensed under the [MIT License](LICENSE).  
See the LICENSE file for details.

---
