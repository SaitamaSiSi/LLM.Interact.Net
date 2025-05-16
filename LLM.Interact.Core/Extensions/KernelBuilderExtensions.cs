using LLM.Interact.Core.Plugins.Amap;
using Microsoft.SemanticKernel;

namespace LLM.Interact.Core.Extensions
{
    public static class KernelBuilderExtensions
    {
        public static IKernelBuilder AddAmapPlugin(this IKernelBuilder kernelBuilder)
        {
            kernelBuilder.Plugins.AddFromType<AmapWeatherTool>();
            kernelBuilder.Plugins.AddFromType<AmapGeoTool>();
            kernelBuilder.Plugins.AddFromType<AmapRegeocodeTool>();
            kernelBuilder.Plugins.AddFromType<AmapIpLocationTool>();
            kernelBuilder.Plugins.AddFromType<AmapSearchDetialTool>();
            kernelBuilder.Plugins.AddFromType<AmapDistanceTool>();
            kernelBuilder.Plugins.AddFromType<AmapTextSearchTool>();
            return kernelBuilder;
        }
    }
}
