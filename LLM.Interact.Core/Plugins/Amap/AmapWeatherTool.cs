using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using LLM.Interact.Core.Models.Amap;
using System.Diagnostics;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapWeatherTool : AmapBase
    {
        public AmapWeatherTool()
        {
            ApiUrl = "v3/weather/weatherInfo";
        }

        [KernelFunction("maps_weather")]
        [Description("根据城市名称或者标准adcode查询指定城市的天气")]
        public object MapsWeather(
            [Description("城市名称或者adcode")] string city
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("city", city);
                Parameters.TryAdd("extensions", "all");
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapWeatherResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status == 1)
                    {
                        var result = new
                        {
                            responseContent.ForeCasts.FirstOrDefault().City,
                            responseContent.ForeCasts.FirstOrDefault().Casts,
                        };
                        Debug.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                        return new
                        {
                            result = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        };
                        cmpResponse.Content.Add(new ContentItem
                        {
                            Type = "json",
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        });
                    }
                    else
                    {
                        cmpResponse.IsError = true;
                        cmpResponse.Content.Add(new ContentItem()
                        {
                            Text = $"Get weather failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Get weather failed: request failed"
                    });
                }
                return new
                {
                    result = JsonSerializer.Serialize(cmpResponse, new JsonSerializerOptions { WriteIndented = true })
                };
            }
        }
    }
}
