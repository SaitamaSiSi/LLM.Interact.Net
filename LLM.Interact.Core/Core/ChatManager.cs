using LLM.Interact.Core.Models;
using LLM.Interact.Core.Plugins;
using LLM.Interact.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLM.Interact.Core.Core
{
    public class ChatManager
    {
        private readonly IKernelBuilder _kernelBuilder;
        private Kernel _kernel;

        private ConcurrentDictionary<long, ChatHistory> ChatHistories = new ConcurrentDictionary<long, ChatHistory>();
        private ConcurrentDictionary<long, IChatCompletionService> ChatWorkers = new ConcurrentDictionary<long, IChatCompletionService>();

        public ChatManager()
        {
            _kernelBuilder = Kernel.CreateBuilder();
            // 插入自定义插件
            _kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
            _kernel = _kernelBuilder.Build();
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromType<MenuPlugin>());
        }

        public bool IsContainsService(long id)
        {
            return ChatWorkers.ContainsKey(id);
        }

        public void AddService(AIConfig config)
        {
            if (!ChatWorkers.ContainsKey(config.Id))
            {
                // 修改服务注册方式，注入HttpClient和模型名称
                switch (config.AiType)
                {
                    case AiType.Ollama:
                        _kernelBuilder.Services.AddKeyedSingleton<IChatCompletionService>(config.ServerKey, new OllamaChatService(config));
                        break;
                }
                _kernel = _kernelBuilder.Build();
                if (!ChatHistories.ContainsKey(config.Id))
                {
                    // 获取刚才定义的插件函数的元数据，用于后续创建prompt
                    var plugins = _kernel.Plugins.GetFunctionsMetadata();
                    //生成函数调用提示词，引导模型根据用户请求去调用函数
                    var functionsPrompt = CreateFunctionsMetaObject(plugins);
                    // 创建聊天历史
                    var history = new ChatHistory();
                    //创建系统提示词，插入刚才生成的提示词
                    var prompt = @$"
                      You have access to the following functions. Use them if required:
                      {functionsPrompt}
                      If function calls are used, ensure the output is in JSON format; otherwise, output should be in text format.
                      ";
                    //添加系统提示词
                    history.AddSystemMessage(prompt);
                    ChatHistories.TryAdd(config.Id, history);
                }

                //创建一个对话服务实例
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(config.ServerKey);
                ChatWorkers.TryAdd(config.Id, chatCompletionService);
            }
        }

        public async IAsyncEnumerable<string> AskStreamingQuestionAsync(long id, string question)
        {
            if (ChatHistories.ContainsKey(id) && ChatWorkers.ContainsKey(id))
            {
                var history = ChatHistories[id];
                //添加用户的提问
                history.AddUserMessage(question);
                // 流式执行kernel
                await foreach (var result in ChatWorkers[id].GetStreamingChatMessageContentsAsync(
                    history,
                    executionSettings: null,
                    kernel: _kernel))
                {
                    yield return result.Content!;
                }
            }
            else
            {
                yield return "服务不存在";
            }
        }

        public async Task<string> AskQuestionAsync(long id, string question)
        {
            if (ChatHistories.ContainsKey(id) && ChatWorkers.ContainsKey(id))
            {
                var history = ChatHistories[id];
                //添加用户的提问
                history.AddUserMessage(question);
                //链式执行kernel
                var result = await ChatWorkers[id].GetChatMessageContentAsync(
                    history,
                    executionSettings: null,
                    kernel: _kernel);
                return result.Content!;
            }
            return "服务不存在";
        }

        private static JToken? CreateFunctionsMetaObject(IList<KernelFunctionMetadata> plugins)
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
