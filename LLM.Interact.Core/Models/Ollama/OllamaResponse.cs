using System;
using System.Text.Json.Serialization;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaResponse
    {
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = string.Empty;
        public OllamaResMsg Message { get; set; } = new OllamaResMsg();
        public bool Done { get; set; }
    }

}
