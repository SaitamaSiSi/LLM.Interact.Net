using LLM.Interact.Core.Plugins;
using LLM.Interact.Core.Services;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using LLM.Interact.Core.Models;

namespace LLM.Interact.Core
{
    public class ChatHelper
    {
        private readonly AIConfig _config;
        private readonly IKernelBuilder _kernelBuilder;
        private readonly Kernel _kernel;
        private readonly IList<KernelFunctionMetadata> _plugins;

        public ChatHelper(AIConfig config)
        {
            _config = config;
            _kernelBuilder = Kernel.CreateBuilder();
            // 修改服务注册方式，注入HttpClient和模型名称
            _kernelBuilder.Services.AddKeyedSingleton<IChatCompletionService>("ollamaChat", new OllamaChatService(_config));

            // 插入自定义插件
            _kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
            _kernel = _kernelBuilder.Build();
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromType<MenuPlugin>());
            // 获取刚才定义的插件函数的元数据，用于后续创建prompt
            _plugins = _kernel.Plugins.GetFunctionsMetadata();
        }

        public async Task<string> AskQuestionAsync(string question)
        {
            // Console.WriteLine($"User> \n{question}");

            //定义一个对话历史
            ChatHistory history = new ChatHistory();
            //生成函数调用提示词，引导模型根据用户请求去调用函数
            var functionsPrompt = CreateFunctionsMetaObject(_plugins);
            //创建系统提示词，插入刚才生成的提示词
            var prompt = @$"
                      You have access to the following functions. Use them if required:
                      {functionsPrompt}
                      If function calls are used, ensure the output is in JSON format; otherwise, output should be in text format.
                      ";
            //添加系统提示词
            history.AddSystemMessage(prompt);
            //创建一个对话服务实例
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            //添加用户的提问
            history.AddUserMessage(question);

            //链式执行kernel
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: null,
                kernel: _kernel);
            //打印回调内容
            // Console.WriteLine($"Assistant> \n{result}");
            return result.Content!;

            // 非流式调用
            //var result = await chat.GetChatMessageContentsAsync(history);
            //ret = result.Last().Content ?? string.Empty;
            //Console.Write(ret);

            // 流式调用
            //await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
            //{
            //    Console.WriteLine(chunk.Content);
            //    ret += chunk.Content;
            //}
        }

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

        static JObject CreateFunctionMetaObject(KernelFunctionMetadata plugin)
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
