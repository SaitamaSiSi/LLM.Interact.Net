using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Ollama
{
    public class OllamaTags
    {
        public List<OllamaModel> Models { get; set; } = new List<OllamaModel>();
    }
}
