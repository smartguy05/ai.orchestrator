
namespace Ai.Orchestrator.Models.Text;

public class TextRequest
{
    public string UserPrompt { get; set; }
    public string SystemPrompt { get; set; } = $"You are a helpful assistant. Please answer questions the best of your ability. If you do not know the answer and are not able to get the answer using a tool, say so, do not make things up. The current local datetime is {DateTime.Now}.";
}