using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaChatParams
    {
        public string? Model { get; set; }
        public bool? Stream { get; set; }
        public bool? Think { get; set; }
        public List<OllamaChatMsg>? Messages { get; set; }
        public List<OllamaChatTool>? Tools { get; set; }
    }

}
