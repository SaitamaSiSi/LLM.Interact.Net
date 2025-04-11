using Microsoft.SemanticKernel;

namespace LLM.Interact.Core.Models
{
    public class SearchResult
    {
        public string FunctionName { get; set; } = string.Empty;
        public bool SearchFunctionNameSucc { get; set; }
        public KernelArguments FunctionParams { get; set; } = new KernelArguments();
        public KernelFunction? KernelFunction { get; set; }
    }
}
