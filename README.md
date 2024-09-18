# Ai.Orchestrator
The purpose of this application is to orchestrate requests from various services so they can interact with other hosted services

## Plugins
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

- Example Plugin Configs
  - Test_Plugin
    ```
    {
      "description": "A plugin to test orchestrator",
      "contract": {
        "name": "string",
        "value": "number"
      },
      "testName": "Test Name",
      "testName2": "Test Name 2"
    }
  - Ai.Orchestrator.Plugins.Webhook
    ```
    {
      "description": "A plugin for calling configured webhooks",
      "contract": {
        "name": "string",
        "value": "string"
      },
      "webhooks": [
        {
          "name": "HomeAssistant1",
          "url": "http://URL/api/webhook/-webhookId"
        }
      ]
    }

Example Swagger Requests
  - webhooks
```
  {
     "service": "ai.orchestrator.plugins.webhook",
     "serviceRequest": { 
      "webhookName": "HomeAssistant1", 
      "Value": "this is a test notification" 
      },
     "data": null
  }
 ```
  - test_plugin
```
  {
    "service": "test_plugin",
    "serviceRequest": { 
      "name": "Test Name", 
      "Value": 0 
    },
    "data": null
  }
```