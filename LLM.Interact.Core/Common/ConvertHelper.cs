using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LLM.Interact.Core.Common
{
    public class ConvertHelper
    {
        public static JToken? CreateFunctionsMetaObject(IList<KernelFunctionMetadata> plugins)
        {
            if (plugins.Count < 1) return null;
            if (plugins.Count == 1) return CreateFunctionMetaObject(plugins[0]);

            JArray promptFunctions = new JArray();
            foreach (var plugin in plugins)
            {
                var pluginFunctionWrapper = CreateFunctionMetaObject(plugin);
                promptFunctions.Add(pluginFunctionWrapper);
            }

            return promptFunctions;
        }

        public static JObject CreateFunctionMetaObject(KernelFunctionMetadata plugin)
        {
            var pluginFunctionWrapper = new JObject()
            {
                { "type", "function" },
            };

            var pluginFunction = new JObject()
            {
                { "name", plugin.Name },
                { "description", plugin.Description },
            };

            var pluginFunctionParameters = new JObject()
            {
                { "type", "object" },
            };
            var pluginProperties = new JObject();
            JArray requiredParameters = new JArray();
            foreach (var parameter in plugin.Parameters)
            {
                var property = new JObject()
                {
                    { "type", parameter.ParameterType?.ToString() },
                    { "description", parameter.Description },
                };

                pluginProperties.Add(parameter.Name, property);
                if (parameter.IsRequired)
                {
                    requiredParameters.Add(parameter.Name);
                }
            }

            pluginFunctionParameters.Add("properties", pluginProperties);
            pluginFunctionParameters.Add("required", requiredParameters);
            pluginFunction.Add("parameters", pluginFunctionParameters);
            pluginFunctionWrapper.Add("function", pluginFunction);

            return pluginFunctionWrapper;
        }
    }
}
