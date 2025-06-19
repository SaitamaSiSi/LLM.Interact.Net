namespace LLM.Interact.Core.Models
{
    public class AIConfig
    {
        public long Id { get; set; }

        public AiType AiType { get; set; } = AiType.Ollama;

        public string Url { get; set; } = "http://127.0.0.1:11434";

        public string ModelName { get; set; } = "qwen2:7b";

        public string ServerKey { get; set; } = "AiChat";

        public string ApiKey { get; set; } = string.Empty;

        public bool IsUseTools { get; set; } = false;
    }
}
