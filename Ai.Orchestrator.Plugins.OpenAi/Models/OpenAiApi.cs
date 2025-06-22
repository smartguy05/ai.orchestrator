namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public class OpenAiApi
{
    public string Name { get; set; }
    public bool Default { get; set; }
    public string Description { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string DefaultSystemPrompt { get; set; }
    public string Model { get; set; }
    public bool ToolsEnabled { get; set; }
}