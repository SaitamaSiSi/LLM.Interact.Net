using System.Text.Json.Serialization;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaModel
    {
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Digest { get; set; } = string.Empty;
        public OllamaModelDetial Details { get; set; } = new OllamaModelDetial();
    }
}
