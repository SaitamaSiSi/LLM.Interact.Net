using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaResMsg
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OllamaFunc> ToolCalls { get; set; } = new List<OllamaFunc>();
    }
}
