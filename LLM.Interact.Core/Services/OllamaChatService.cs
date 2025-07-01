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
using Newtonsoft.Json.Linq;

namespace LLM.Interact.Core.Services
{
    public class OllamaChatService : IChatCompletionService
    {
        public static bool IsTest = false;
        private readonly HttpClient _httpClient;
        private const AiType aiType = AiType.Ollama;
        private static Dictionary<string, SearchResult> DicSearchResult = new Dictionary<string, SearchResult>();
        private static List<OllamaChatTool> OllamaChatTools = new List<OllamaChatTool>();

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
                    var funcTool = new OllamaChatTool
                    {
                        Type = "function"
                    };
                    funcTool.Function.Name = functionName;
                    funcTool.Function.Description = functionMetaData.Description;
                    funcTool.Function.Parameters.Type = "object";
                    foreach (var parameter in functionMetaData.Parameters)
                    {
                        if (parameter.Schema != null)
                        {
                            JsonElement root = parameter.Schema.RootElement;
                            var prop = new OllamaChatFuncProp();
                            if (root.TryGetProperty("type", out JsonElement typeElement))
                            {
                                prop.Type = typeElement.ValueKind switch
                                {
                                    JsonValueKind.String => typeElement.GetString() ?? string.Empty,
                                    JsonValueKind.Array => string.Join("|", typeElement.EnumerateArray().Select(e => e.GetString())),
                                    _ => "unknown"
                                };
                            }

                            // 2. 解析 description
                            if (root.TryGetProperty("description", out JsonElement descElement) &&
                                descElement.ValueKind == JsonValueKind.String)
                            {
                                prop.Description = descElement.GetString() ?? string.Empty;
                            }

                            // 3. 解析 enum
                            if (root.TryGetProperty("enum", out JsonElement enumElement) &&
                                enumElement.ValueKind == JsonValueKind.Array)
                            {
                                prop.Enum = new List<string>();
                                foreach (JsonElement item in enumElement.EnumerateArray())
                                {
                                    prop.Enum.Add(item.ToString());
                                }
                            }

                            funcTool.Function.Parameters.Properties[parameter.Name] = prop;
                            if (parameter.IsRequired)
                            {
                                funcTool.Function.Parameters.Required.Add(parameter.Name);
                            }
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
                        foreach (var par in search.FunctionParams)
                        {
                            if (function.Arguments.TryGetValue(par.Key, out var value) && par.Value == null)
                            {
                                search.FunctionParams[par.Key] = value?.ToString().ToLower();
                            }
                        }
                    }
                }
            }

            // return searches.Any(x => x.SearchFunctionNameSucc && x.FunctionParams.All(x => x.Value != null));
            return searches.Any(x => x.SearchFunctionNameSucc);
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
            // return searches.Any(x => x.SearchFunctionNameSucc && x.FunctionParams.All(x => x.Value != null));
            return searches.Any(x => x.SearchFunctionNameSucc);
        }

        private OllamaChatParams HistoryToRequestBody(ChatHistory history, bool isStream)
        {
            OllamaChatParams res = new OllamaChatParams();
            ChatManager.ChatModels.TryGetValue(aiType, out var modelCofig);
            res.Model = modelCofig.ModelName;
            res.Stream = isStream;
            if (history.Any())
            {
                res.Messages = new List<OllamaChatMsg>();
                foreach (var message in history)
                {
                    List<string> imgs = new List<string>();
                    OllamaChatMsg chatMsg = new OllamaChatMsg
                    {
                        Role = message.Role.Label,
                        Content = message.Content,
                    };

                    foreach (var item in message.Items)
                    {
                        if (item is ImageContent imgContent && imgContent.Data != null)
                        {
                            imgs.Add(Convert.ToBase64String(imgContent.Data.Value.ToArray()));
                        }
                    }
                    chatMsg.Images = imgs.Count > 0 ? imgs : null;
                    res.Messages.Add(chatMsg);
                }
            }
            if (modelCofig.IsUseTools)
            {
                res.Tools = OllamaChatTools;
            }
            if (res.Model.StartsWith("qwen3") || res.Model.StartsWith("deepseek-r1"))
            {
                res.Think = true;
            }
            return res;
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            GetDicSearchResult(kernel!);

            // 构建Ollama API请求
            OllamaChatParams requestBody = HistoryToRequestBody(chatHistory, true);

            HttpResponseMessage response = new HttpResponseMessage();
            string errMsg = string.Empty;
            try
            {
                response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                requestBody,
                cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                errMsg = e.Message;
            }
            if (!string.IsNullOrEmpty(errMsg))
            {
                yield return new StreamingChatMessageContent(
                    AuthorRole.Assistant,
                    content: $"Error: {errMsg}");
                yield break;
            }

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
                var content = chunk.Message?.Content.ToString();
                if (!string.IsNullOrEmpty(content))
                {
                    completeResponse.Append(chunk.Message?.Content.ToString());
                    yield return new StreamingChatMessageContent(
                        AuthorRole.Assistant,
                        content: content);
                }
                if (chunk.Done)
                {
                    break;
                }
                else
                {
                    ollamaFuncs.AddRange(chunk.Message?.ToolCalls ?? new List<OllamaFunc>());
                }
            }

            if (ollamaFuncs.Count > 0)
            {
                var searchs = DicSearchResult.Values.ToList();
                if (TryFindValues(ollamaFuncs, ref searchs))
                {
                    var firstFunc = searchs.Where(x => x.SearchFunctionNameSucc).First();
                    if (firstFunc.KernelFunction != null)
                    {
                        var funcCallResult = await firstFunc.KernelFunction.InvokeAsync(kernel!, firstFunc.FunctionParams);
                        if (IsTest)
                        {
                            yield return new StreamingChatMessageContent(
                                AuthorRole.Assistant,
                                funcCallResult.ToString());
                            yield break;
                        }
                        chatHistory.AddMessage(AuthorRole.Assistant, funcCallResult.ToString() + "（这是最终结果，不需要再次执行此操作）");
                        await foreach (var result in GetStreamingChatMessageContentsAsync(chatHistory, kernel: kernel))
                        {
                            yield return new StreamingChatMessageContent(
                                AuthorRole.Assistant,
                                result.Content!);
                        }
                    }
                }
            }
            else
            {
                var finalResponse = completeResponse.ToString();
                //JToken? jToken = null;
                //try
                //{
                //    jToken = JToken.Parse(finalResponse);
                //    jToken = SystemHelper.ConvertStringToJson(jToken);
                //}
                //catch
                //{

                //}
                //if (jToken != null)
                //{
                //    var searchs = DicSearchResult.Values.ToList();
                //    if (TryFindValues(jToken, ref searchs))
                //    {
                //        var firstFunc = searchs.Where(x => x.SearchFunctionNameSucc).First();
                //        var funcCallResult = await firstFunc.KernelFunction.InvokeAsync(kernel!, firstFunc.FunctionParams);
                //        chatHistory.AddMessage(AuthorRole.Assistant, finalResponse);
                //        chatHistory.AddMessage(AuthorRole.Tool, funcCallResult.ToString());
                //        await foreach (var result in GetStreamingChatMessageContentsAsync(chatHistory, kernel: kernel))
                //        {
                //            yield return new StreamingChatMessageContent(
                //        AuthorRole.Assistant,
                //        result.Content!);
                //        }
                //    }
                //}
                if (string.IsNullOrEmpty(finalResponse))
                {
                    yield return new StreamingChatMessageContent(
                                AuthorRole.Assistant,
                                chatHistory.Last().Content?.Replace("（这是最终结果，不需要再次执行此操作）", ""));
                }
                else
                {
                    chatHistory.AddMessage(AuthorRole.Assistant, finalResponse);
                }
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
