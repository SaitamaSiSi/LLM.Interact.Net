using LLM.Interact.Core.Models;
using LLM.Interact.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IO;
using LLM.Interact.Core.Plugins;
using LLM.Interact.Core.Extensions;

namespace LLM.Interact.Core.Core
{
    public class ChatManager
    {
        private readonly IKernelBuilder _kernelBuilder;
        private Kernel _kernel;

        private readonly ConcurrentDictionary<AiType, ChatHistory> ChatHistories = new ConcurrentDictionary<AiType, ChatHistory>();
        private readonly ConcurrentDictionary<AiType, IChatCompletionService> ChatWorkers = new ConcurrentDictionary<AiType, IChatCompletionService>();
        static public ConcurrentDictionary<AiType, AIConfig> ChatModels = new ConcurrentDictionary<AiType, AIConfig>();

        public ChatManager()
        {
            _kernelBuilder = Kernel.CreateBuilder();
            // 插入自定义插件
            _kernelBuilder.AddAmapPlugin();
            _kernelBuilder.Plugins.AddFromType<TestPlugin>();
            _kernel = _kernelBuilder.Build();
            // _kernel.Plugins.Add(KernelPluginFactory.CreateFromType<WeatherPlugin>());
        }

        public async Task AddService(AIConfig config)
        {
            if (!ChatWorkers.ContainsKey(config.AiType))
            {
                // 修改服务注册方式，注入HttpClient和模型名称
                switch (config.AiType)
                {
                    case AiType.Ollama:
                        _kernelBuilder.Services.AddKeyedSingleton<IChatCompletionService>(config.ServerKey, new OllamaChatService(config));
                        break;
                }
                _kernel = _kernelBuilder.Build();
                if (!ChatHistories.ContainsKey(config.AiType))
                {
                    // 创建聊天历史
                    var history = new ChatHistory();
                    //// 获取刚才定义的插件函数的元数据，用于后续创建prompt
                    //var plugins = _kernel.Plugins.GetFunctionsMetadata();
                    ////生成函数调用提示词，引导模型根据用户请求去调用函数
                    //var functionsPrompt = SystemHelper.CreateFunctionsMetaObject(plugins);
                    ////创建系统提示词，插入刚才生成的提示词
                    //var prompt = @$"
                    //  You have access to the following functions. Use them if required:
                    //  {functionsPrompt}
                    //  If function calls are used, ensure the output is in JSON format; otherwise, output should be in text format.
                    //  ";
                    ////添加系统提示词
                    //history.AddSystemMessage(prompt);
                    ChatHistories.TryAdd(config.AiType, history);
                }

                //创建一个对话服务实例
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(config.ServerKey);
                ChatWorkers.TryAdd(config.AiType, chatCompletionService);
                ChatModels.TryAdd(config.AiType, config);
                await LoadModel(config);
            }
            else
            {
                ChatModels.TryGetValue(config.AiType, out var oldValue);
                ChatModels.TryUpdate(config.AiType, config, oldValue);
                await LoadModel(config);
            }
        }

        public async Task<string> LoadModel(AIConfig config)
        {
            using var httplient = new HttpClient { BaseAddress = new Uri(config.Url) };
            var requestBody = new { model = config.ModelName };
            using var response = await httplient.PostAsJsonAsync("/api/chat", requestBody, CancellationToken.None);
            return response.ReasonPhrase;
        }

        public async Task<string> UnLoadModel(AIConfig config)
        {
            using var httplient = new HttpClient { BaseAddress = new Uri(config.Url) };
            var requestBody = new { model = config.ModelName, keep_alive = 0 };
            using var response = await httplient.PostAsJsonAsync("/api/chat", requestBody, CancellationToken.None);
            return response.ReasonPhrase;
        }

        public bool IsContainsWorker(AiType type)
        {
            return ChatWorkers.ContainsKey(type);
        }

        public async Task RemoveHistory(AiType type)
        {
            ChatModels.TryGetValue(type, out var config);
            if (config != null)
            {
                await UnLoadModel(config);
                ChatHistories.Remove(type, out _);
                var history = new ChatHistory();
                ChatHistories.TryAdd(type, history);
            }
        }

        public async IAsyncEnumerable<string> WebTestAskAsync(AiType type, string question)
        {
            OllamaChatService.IsTest = true;
            var history = new ChatHistory();
            history.AddUserMessage(question);
            await foreach (var result in ChatWorkers[type].GetStreamingChatMessageContentsAsync(
                history,
                executionSettings: null,
                kernel: _kernel))
            {
                yield return result.Content!;
            }
        }

        public async IAsyncEnumerable<string> AskStreamingQuestionAsync(AiType type, string question, List<string>? imgs = null)
        {
            if (ChatHistories.ContainsKey(type) && ChatWorkers.ContainsKey(type))
            {
                var history = ChatHistories[type];
                //添加用户的提问
                var chatCollection = new ChatMessageContentItemCollection
                {
                    // 添加文本内容
                    new TextContent(question)
                };
                if (imgs != null)
                {
                    // 添加图片内容
                    foreach (var img in imgs)
                    {
                        ImageMimeType.MimeTypes.TryGetValue(Path.GetExtension(img).ToLowerInvariant(), out string mime);
                        byte[] bytes = File.ReadAllBytes(img);
                        chatCollection.Add(new ImageContent(bytes, mime));
                    }
                }
                history.AddUserMessage(chatCollection);
                // 流式执行kernel
                await foreach (var result in ChatWorkers[type].GetStreamingChatMessageContentsAsync(
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
    }
}
