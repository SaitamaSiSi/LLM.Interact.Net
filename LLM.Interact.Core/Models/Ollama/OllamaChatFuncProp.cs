using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatFuncProp
    {
        public string Type { get; set; } = "string";

        public string Description { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Enum { get; set; }
    }
}
