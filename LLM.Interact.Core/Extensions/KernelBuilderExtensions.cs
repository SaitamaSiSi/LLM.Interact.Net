using LLM.Interact.Core.Plugins.Amap;
using Microsoft.SemanticKernel;

namespace LLM.Interact.Core.Extensions
{
    public static class KernelBuilderExtensions
    {
        public static IKernelBuilder AddAmapPlugin(this IKernelBuilder kernelBuilder)
        {
            kernelBuilder.Plugins.AddFromType<AmapWeatherTool>();
            return kernelBuilder;
        }
    }
}
