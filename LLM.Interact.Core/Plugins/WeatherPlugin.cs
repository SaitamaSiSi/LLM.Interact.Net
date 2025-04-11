using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace LLM.Interact.Core.Plugins
{
    public sealed class WeatherPlugin
    {
        [KernelFunction, Description("获取城市的天气状况")]
        public object GetWeather([Description("城市名称")] string CityName, [Description("查询时段，值可以是[白天，夜晚]")] string DayPart)
        {
            // JsonDocument https://api.weather.gov/alerts/active/area/CA
            return new
            {
                CityName,
                DayPart,
                CurrentCondition = "多云",
                LaterCondition = "阴",
                MinTemperature = 19,
                MaxTemperature = 23
            };
        }
    }
}
