using LLM.Interact.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Json;

namespace LLM.Interact.Core.Services
{
    public class OllamaChatService : IChatCompletionService
    {
        public static Dictionary<string, SearchResult> DicSearchResult = new Dictionary<string, SearchResult>();
        public static void GetDicSearchResult(Kernel kernel)
        {
            DicSearchResult = new Dictionary<string, SearchResult>();
            foreach (var functionMetaData in kernel.Plugins.GetFunctionsMetadata())
            {
                string functionName = functionMetaData.Name;
                if (DicSearchResult.ContainsKey(functionName))
                    continue;
                var searchResult = new SearchResult
                {
                    FunctionName = functionName,
                    KernelFunction = kernel.Plugins.GetFunction(null, functionName)
                };
                functionMetaData.Parameters.ToList().ForEach(x => searchResult.FunctionParams.Add(x.Name, null));
                DicSearchResult.Add(functionName, searchResult);
            }
        }
        public IReadOnlyDictionary<string, object?> Attributes => throw new NotImplementedException();
        static Stopwatch sw = new Stopwatch();

        private readonly HttpClient _httpClient;
        private readonly string _modelName;

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

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            GetDicSearchResult(kernel!);
            var prompt = HistoryToText(chatHistory);

            // 构建Ollama API请求
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                stream = false
            };

            sw.Start();
            var response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                requestBody,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            sw.Stop();

            Console.WriteLine($"调用耗时:{Math.Round(sw.Elapsed.TotalSeconds, 2)}秒");
            var chatResponse = responseContent?.Message?.Content ?? "";
            try
            {
                JToken jToken = JToken.Parse(chatResponse);
                jToken = ConvertStringToJson(jToken);
                var searchs = DicSearchResult.Values.ToList();
                if (TryFindValues(jToken, ref searchs))
                {
                    // 参数完整性检查
                    //if (searchs.Any(s =>
                    //    s.FunctionParams.Any(p => p.Value == null)))
                    //{
                    //    throw new ArgumentException("函数参数不完整");
                    //}
                    //var firstFunc = searchs.First();
                    var firstFunc = searchs.Where(x => x.SearchFunctionNameSucc).First();
                    var funcCallResult = await firstFunc.KernelFunction.InvokeAsync(kernel!, firstFunc.FunctionParams);
                    chatHistory.AddMessage(AuthorRole.Assistant, chatResponse);
                    chatHistory.AddMessage(AuthorRole.Tool, funcCallResult.ToString());
                    return await GetChatMessageContentsAsync(chatHistory, kernel: kernel);
                }
                else
                {

                }
            }
            catch (Exception e)
            {

            }
            chatHistory.AddMessage(AuthorRole.Assistant, chatResponse);
            return new List<ChatMessageContent> { new ChatMessageContent(AuthorRole.Assistant, chatResponse) };
        }

        // 新增Ollama响应模型
        private class OllamaResponse
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string Content { get; set; } = "";
        }

        JToken ConvertStringToJson(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                // 遍历对象的每个属性
                JObject obj = new JObject();
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    obj.Add(prop.Name, ConvertStringToJson(prop.Value));
                }
                return obj;
            }
            else if (token.Type == JTokenType.Array)
            {
                // 遍历数组的每个元素
                JArray array = new JArray();
                foreach (JToken item in token.Children())
                {
                    array.Add(ConvertStringToJson(item));
                }
                return array;
            }
            else if (token.Type == JTokenType.String)
            {
                // 尝试将字符串解析为 JSON
                string value = token.ToString();
                try
                {
                    return JToken.Parse(value);
                }
                catch (Exception)
                {
                    // 解析失败时返回原始字符串
                    return token;
                }
            }
            else
            {
                // 其他类型直接返回
                return token;
            }
        }

        bool TryFindValues(JToken token, ref List<SearchResult> searches)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var child in token.Children<JProperty>())
                {
                    foreach (var search in searches)
                    {
                        if (child.Value.ToString().ToLower().Equals(search.FunctionName.ToLower()) && search.SearchFunctionNameSucc != true)
                            search.SearchFunctionNameSucc = true;
                        foreach (var par in search.FunctionParams)
                        {
                            if (child.Name.ToLower().Equals(par.Key.ToLower()) && par.Value == null)
                                search.FunctionParams[par.Key] = child.Value.ToString().ToLower();
                        }
                    }
                    if (searches.Any(x => x.SearchFunctionNameSucc == false || x.FunctionParams.Any(x => x.Value == null)))
                        TryFindValues(child.Value, ref searches);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    if (searches.Any(x => x.SearchFunctionNameSucc == false || x.FunctionParams.Any(x => x.Value == null)))
                        TryFindValues(item, ref searches);
                }
            }
            return searches.Any(x => x.SearchFunctionNameSucc && x.FunctionParams.All(x => x.Value != null));
        }

        public virtual string HistoryToText(ChatHistory history)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var message in history)
            {
                if (message.Role == AuthorRole.User)
                {
                    sb.AppendLine($"User: {message.Content}");
                }
                else if (message.Role == AuthorRole.System)
                {
                    sb.AppendLine($"System: {message.Content}");
                }
                else if (message.Role == AuthorRole.Assistant)
                {
                    sb.AppendLine($"Assistant: {message.Content}");
                }
                else if (message.Role == AuthorRole.Tool)
                {
                    sb.AppendLine($"Tool: {message.Content}");
                }
            }
            return sb.ToString();
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

}
