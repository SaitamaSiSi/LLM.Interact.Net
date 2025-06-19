using LLM.Interact.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Json;
using System.IO;
using System.Runtime.CompilerServices;
using LLM.Interact.Core.Core;
using LLM.Interact.Core.Models.Ollama;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace LLM.Interact.Core.Services
{
    public class OllamaChatService : IChatCompletionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private const AiType aiType = AiType.Ollama;
        public static Dictionary<string, SearchResult> DicSearchResult = new Dictionary<string, SearchResult>();
        public static List<OllamaChatTool> OllamaChatTools = new List<OllamaChatTool>();

        public OllamaChatService(AIConfig config)
        {
            var handler = new StandardSocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 10
            };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(config.Url) };
            // 在请求头中添加协议版本
            _httpClient.DefaultRequestHeaders.Add("Zyh-Mcp-Version", "1.0");
            _modelName = config.ModelName;
        }

        public static void GetDicSearchResult(Kernel kernel)
        {
            DicSearchResult = new Dictionary<string, SearchResult>();
            var plugins = kernel.Plugins.GetFunctionsMetadata();
            foreach (var functionMetaData in plugins)
            {
                string functionName = functionMetaData.Name;
                if (!DicSearchResult.ContainsKey(functionName))
                {
                    var searchResult = new SearchResult
                    {
                        FunctionName = functionName,
                        KernelFunction = kernel.Plugins.GetFunction(null, functionName)
                    };
                    functionMetaData.Parameters.ToList().ForEach(x => searchResult.FunctionParams.Add(x.Name, null));
                    DicSearchResult.Add(functionName, searchResult);
                }
                if (OllamaChatTools.Find(m => m.Function.Name == functionName) == null)
                {
                    var funcTool = new OllamaChatTool();
                    funcTool.Type = "function";
                    funcTool.Function.Name = functionName;
                    funcTool.Function.Description = functionMetaData.Description;
                    funcTool.Function.Parameters.Type = "object";
                    foreach (var parameter in functionMetaData.Parameters)
                    {
                        funcTool.Function.Parameters.Properties[parameter.Name] = new OllamaChatFuncProp
                        {
                            Type = parameter.ParameterType?.ToString() ?? string.Empty,
                            Description = parameter.Description
                        };
                        if (parameter.IsRequired)
                        {
                            funcTool.Function.Parameters.Required.Add(parameter.Name);
                        }
                    }
                    OllamaChatTools.Add(funcTool);
                }
            }
        }

        bool TryFindValues(List<OllamaFunc> calls, ref List<SearchResult> searches)
        {
            foreach (var call in calls)
            {
                var function = call.Function;
                foreach (var search in searches)
                {
                    if (function.Name.ToLower().Equals(search.FunctionName.ToLower()) && search.SearchFunctionNameSucc != true)
                    {
                        search.SearchFunctionNameSucc = true;
                    }
                    foreach (var par in search.FunctionParams)
                    {
                        if (function.Arguments.TryGetValue(par.Key, out var value) && par.Value == null)
                        {
                            search.FunctionParams[par.Key] = value?.ToString().ToLower();
                        }
                    }
                }
            }

            // return searches.Any(x => x.SearchFunctionNameSucc && x.FunctionParams.All(x => x.Value != null));
            return searches.Any(x => x.SearchFunctionNameSucc);
        }

        private OllamaChatParams HistoryToRequestBody(ChatHistory history, List<string>? images, bool isStream)
        {
            OllamaChatParams res = new OllamaChatParams();
            res.Model = _modelName;
            res.Stream = isStream;
            if (history.Any())
            {
                res.Messages = new List<OllamaChatMsg>();
                foreach (var message in history)
                {
                    res.Messages.Add(new OllamaChatMsg
                    {
                        Role = message.Role.Label,
                        Content = message.Content,
                    });
                }
                res.Messages[^1].Images = images;
            }
            res.Tools = OllamaChatTools;
            return res;
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            GetDicSearchResult(kernel!);

            // 构建Ollama API请求
            List<string>? images = ChatManager.ChatImages.TryGetValue(aiType, out var img) ? img : null;
            OllamaChatParams requestBody = HistoryToRequestBody(chatHistory, images, true);

            var response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                requestBody,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var completeResponse = new StringBuilder();

            List<OllamaFunc> ollamaFuncs = new List<OllamaFunc>();
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(1);
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var chunk = JsonSerializer.Deserialize<OllamaResponse>(line, options);
                if (chunk == null)
                {
                    continue;
                }
                if (chunk.Done)
                {
                    break;
                }
                else
                {
                    completeResponse.Append(chunk.Message?.Content.ToString());
                    ollamaFuncs.AddRange(chunk.Message?.ToolCalls ?? new List<OllamaFunc>());
                }
                string msg = chunk.Message?.Content ?? "";
                yield return new StreamingChatMessageContent(
                    AuthorRole.Assistant,
                    content: msg);
            }

            if (ollamaFuncs.Count > 0)
            {
                var searchs = DicSearchResult.Values.ToList();
                if (TryFindValues(ollamaFuncs, ref searchs))
                {
                    var firstFunc = searchs.Where(x => x.SearchFunctionNameSucc).First();
                    var funcCallResult = await firstFunc.KernelFunction.InvokeAsync(kernel!, firstFunc.FunctionParams);
                    chatHistory.AddMessage(AuthorRole.Tool, funcCallResult.ToString());
                    await foreach (var result in GetStreamingChatMessageContentsAsync(chatHistory, kernel: kernel))
                    {
                        yield return new StreamingChatMessageContent(
                    AuthorRole.Assistant,
                    result.Content!);
                    }
                }
            }
            else
            {
                var finalResponse = completeResponse.ToString();
                chatHistory.AddMessage(AuthorRole.Assistant, finalResponse);
            }
        }

        #region 暂不实现

        public IReadOnlyDictionary<string, object?> Attributes => throw new NotImplementedException();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
