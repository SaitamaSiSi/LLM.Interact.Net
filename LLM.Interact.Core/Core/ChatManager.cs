using LLM.Interact.Core.Extensions;
using LLM.Interact.Core.Models;
using LLM.Interact.Core.Models.Ollama;
using LLM.Interact.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LLM.Interact.Core.Core
{
    public class ChatManager
    {
        private readonly IKernelBuilder _kernelBuilder;
        private Kernel _kernel;

        private readonly ConcurrentDictionary<AiType, ChatHistory> ChatHistories = new ConcurrentDictionary<AiType, ChatHistory>();
        private readonly ConcurrentDictionary<AiType, IChatCompletionService> ChatWorkers = new ConcurrentDictionary<AiType, IChatCompletionService>();
        static public ConcurrentDictionary<AiType, List<string>?> ChatImages = new ConcurrentDictionary<AiType, List<string>?>();
        static public ConcurrentDictionary<AiType, AIConfig> ChatModels = new ConcurrentDictionary<AiType, AIConfig>();

        public ChatManager()
        {
            _kernelBuilder = Kernel.CreateBuilder();
            // 插入自定义插件
            _kernelBuilder.AddAmapPlugin();
            // _kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
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

        public async Task LoadModel(AIConfig config)
        {
            using var httplient = new HttpClient { BaseAddress = new Uri(config.Url) };
            var requestBody = new { model = config.ModelName };
            using var response = await httplient.PostAsJsonAsync("/api/chat", requestBody, CancellationToken.None);
        }

        public async Task UnLoadModel(AIConfig config)
        {
            using var httplient = new HttpClient { BaseAddress = new Uri(config.Url) };
            var requestBody = new { model = config.ModelName, keep_alive = 0 };
            using var response = await httplient.PostAsJsonAsync("/api/chat", requestBody, CancellationToken.None);
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

        public async IAsyncEnumerable<string> AskStreamingQuestionAsync(AiType type, string question, List<string>? imgs = null)
        {
            if (ChatHistories.ContainsKey(type) && ChatWorkers.ContainsKey(type))
            {
                if (ChatImages.ContainsKey(type))
                {
                    ChatImages[type] = imgs;
                }
                else
                {
                    ChatImages.TryAdd(type, imgs);
                }
                var history = ChatHistories[type];
                //添加用户的提问
                history.AddUserMessage(question);
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
