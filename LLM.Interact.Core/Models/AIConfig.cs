namespace LLM.Interact.Core.Models
{
    public class AIConfig
    {
        public string Url { get; set; } = "http://127.0.0.1:11434";

        public string ModelName { get; set; } = "qwen2:7b";

        public string ApiKey { get; set; } = string.Empty;
    }
}
