using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaFuncCall
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}
