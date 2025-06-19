using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaModelDetial
    {
        public string Format { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Families { get; set; }
        [JsonPropertyName("parameter_size")]
        public string ParameterSize { get; set; } = string.Empty;
        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel { get; set; } = string.Empty;
    }
}
