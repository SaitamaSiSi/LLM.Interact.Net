namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatFunc
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OllamaChatFuncParams Parameters { get; set; } = new OllamaChatFuncParams();
    }
}
