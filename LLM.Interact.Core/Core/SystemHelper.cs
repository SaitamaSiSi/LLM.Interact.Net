using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LLM.Interact.Core.Core
{
    public class SystemHelper
    {
        public static void OpenWindowsTerminal()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/K echo Windows CMD 已打开 && title C#启动的终端",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            Process.Start(startInfo);
            Console.WriteLine("已启动 Windows CMD");
        }

        public static void OpenLinuxTerminal()
        {
            // 尝试多种可能的 Linux 终端
            string[] linuxTerminals = {
            "gnome-terminal",      // GNOME 桌面
            "konsole",              // KDE 桌面
            "xfce4-terminal",       // XFCE 桌面
            "xterm",                // 通用 X11 终端
            "terminator",           // 高级终端
            "alacritty",            // 现代终端
            "tilix"                 // Tiling 终端
        };

            bool terminalOpened = false;

            foreach (var terminal in linuxTerminals)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = terminal,
                        UseShellExecute = true,
                        Arguments = "--working-directory=" + Environment.GetEnvironmentVariable("HOME")
                    });

                    Console.WriteLine($"已启动 {terminal}");
                    terminalOpened = true;
                    break;
                }
                catch (Exception)
                {
                    // 尝试下一个终端
                }
            }

            if (!terminalOpened)
            {
                // 终极回退方案
                try
                {
                    Process.Start("sh", "-c \"echo '未找到图形终端，正在尝试默认终端...' && sleep 1\"");
                    Console.WriteLine("已启动回退终端");
                }
                catch (Exception ex)
                {
                    throw new Exception($"无法启动任何终端: {ex.Message}");
                }
            }
        }



        #region 转换

        public static JToken? CreateFunctionsMetaObject(IList<KernelFunctionMetadata> plugins)
        {
            if (plugins.Count < 1) return null;
            if (plugins.Count == 1) return CreateFunctionMetaObject(plugins[0]);

            JArray promptFunctions = new JArray();
            foreach (var plugin in plugins)
            {
                var pluginFunctionWrapper = CreateFunctionMetaObject(plugin);
                promptFunctions.Add(pluginFunctionWrapper);
            }

            return promptFunctions;
        }

        public static JObject CreateFunctionMetaObject(KernelFunctionMetadata plugin)
        {
            var pluginFunctionWrapper = new JObject()
            {
                { "type", "function" },
            };

            var pluginFunction = new JObject()
            {
                { "name", plugin.Name },
                { "description", plugin.Description },
            };

            var pluginFunctionParameters = new JObject()
            {
                { "type", "object" },
            };
            var pluginProperties = new JObject();
            JArray requiredParameters = new JArray();
            foreach (var parameter in plugin.Parameters)
            {
                var property = new JObject()
                {
                    { "type", parameter.ParameterType?.ToString() },
                    { "description", parameter.Description },
                };

                pluginProperties.Add(parameter.Name, property);
                if (parameter.IsRequired)
                {
                    requiredParameters.Add(parameter.Name);
                }
            }

            pluginFunctionParameters.Add("properties", pluginProperties);
            pluginFunctionParameters.Add("required", requiredParameters);
            pluginFunction.Add("parameters", pluginFunctionParameters);
            pluginFunctionWrapper.Add("function", pluginFunction);

            return pluginFunctionWrapper;
        }

        public static JToken ConvertStringToJson(JToken token)
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

        #endregion
    }
}
