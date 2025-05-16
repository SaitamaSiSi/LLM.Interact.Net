using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapWeatherTool : AmapBase
    {
        public AmapWeatherTool()
        {
            ApiUrl = "v3/weather/weatherInfo";
            ApiKey = "";
        }

        [KernelFunction, Description("根据城市名称或者标准adcode查询指定城市的天气")]
        public WeatherResponse MapsWeather(
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

                WeatherResponse weatherResponse = new WeatherResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapWeatherResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status != 1)
                    {
                        var result = new
                        {
                            city = responseContent.ForeCasts.FirstOrDefault().City,
                            forecasts = responseContent.ForeCasts.FirstOrDefault().Casts
                        };
                        weatherResponse.Content.Add(new ContentItem
                        {
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            })
                        });
                    }
                    else
                    {
                        weatherResponse.IsError = true;
                        weatherResponse.Content.Add(new ContentItem()
                        {
                            Text = $"Get weather failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    weatherResponse.IsError = true;
                    weatherResponse.Content.Add(new ContentItem()
                    {
                        Text = "Get weather failed: required failed"
                    });
                }
                return weatherResponse;
            }
        }
        public class WeatherResponse
        {
            public List<ContentItem> Content { get; set; } = new List<ContentItem>();
            public bool IsError { get; set; }
        }

        public class ContentItem
        {
            public string Type { get; set; } = "text";
            public string Text { get; set; } = string.Empty;
        }

        #region 高德天气返回对象

        private class Casts
        {
            public string Date { get; set; } = string.Empty;
            public string Week { get; set; } = string.Empty;
            public string DayWeather { get; set; } = string.Empty;
            public string NightWeather { get; set; } = string.Empty;
            public string DayTemp { get; set; } = string.Empty;
            public string NightTemp { get; set; } = string.Empty;
            public string DayWind { get; set; } = string.Empty;
            public string NightWind { get; set; } = string.Empty;
            public string DayPower { get; set; } = string.Empty;
            public string NightPower { get; set; } = string.Empty;
        }
        private class Forecast
        {
            public string City { get; set; } = string.Empty;
            public IEnumerable<Casts>? Casts { get; set; }
        }
        private class AmapWeatherResponse
        {
            public int Status { get; set; }
            public string Info { get; set; } = string.Empty;
            public string InfoCode { get; set; } = string.Empty;
            public List<Forecast> ForeCasts { get; set; } = new List<Forecast>();
        }

        #endregion

    }
}
