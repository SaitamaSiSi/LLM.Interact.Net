using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatFuncParams
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, OllamaChatFuncProp> Properties { get; set; } = new Dictionary<string, OllamaChatFuncProp>();
        public List<string> Required { get; set; } = new List<string>();
    }
}
