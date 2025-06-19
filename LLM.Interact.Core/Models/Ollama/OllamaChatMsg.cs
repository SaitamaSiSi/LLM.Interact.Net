using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatMsg
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public List<string>? Images { get; set; }
    }
}
