using LLM.Interact.Core.Extensions;
using LLM.Interact.Core.Models;
using LLM.Interact.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LLM.Interact.Core.Core
{
    public class ChatManager
    {
        private readonly IKernelBuilder _kernelBuilder;
        private Kernel _kernel;

        private readonly ConcurrentDictionary<AiType, ChatHistory> ChatHistories = new ConcurrentDictionary<AiType, ChatHistory>();
        private readonly ConcurrentDictionary<AiType, IChatCompletionService> ChatWorkers = new ConcurrentDictionary<AiType, IChatCompletionService>();
        static public ConcurrentDictionary<AiType, List<string>?> ChatImages = new ConcurrentDictionary<AiType, List<string>?>();

        public ChatManager()
        {
            _kernelBuilder = Kernel.CreateBuilder();
            // 插入自定义插件
            _kernelBuilder.AddAmapPlugin();
            // _kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
            _kernel = _kernelBuilder.Build();
            // _kernel.Plugins.Add(KernelPluginFactory.CreateFromType<WeatherPlugin>());
        }

        public void AddService(AIConfig config)
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
            }
        }

        public bool IsContainsWorker(AiType type)
        {
            return ChatWorkers.ContainsKey(type);
        }

        public bool RemoveWorker(AiType type)
        {
            return ChatHistories.Remove(type, out _) && ChatWorkers.Remove(type, out _);
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
