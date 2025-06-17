using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using LLM.Interact.Core.Models.Amap;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapGeoTool : AmapBase
    {
        public AmapGeoTool()
        {
            ApiUrl = "v3/geocode/geo";
        }

        [KernelFunction("maps_geo")]
        [Description("将详细的结构化地址转换为经纬度坐标。支持对地标性名胜景区、建筑物名称解析为经纬度坐标")]
        public object MapsGeo(
            [Description("待解析的结构化地址信息")] string address,
            [Description("指定查询的城市")] string city
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("address", address);
                Parameters.TryAdd("city", city);
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapGeoResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status == 1)
                    {
                        var result = new
                        {
                            Results = responseContent.Geocodes
                        };
                        cmpResponse.Content.Add(new ContentItem
                        {
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        });
                    }
                    else
                    {
                        cmpResponse.IsError = true;
                        cmpResponse.Content.Add(new ContentItem()
                        {
                            Text = $"Geocoding failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Geocoding failed: request failed"
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
