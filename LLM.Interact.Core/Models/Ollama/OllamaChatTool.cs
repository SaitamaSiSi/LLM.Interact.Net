namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatTool
    {
        public string Type { get; set; } = "function";
        public OllamaChatFunc Function { get; set; } = new OllamaChatFunc();
    }
}
