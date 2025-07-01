using LLM.Interact.Core.Core;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LLM.Interact.Core.Plugins
{
    public enum Brightness
    {
        low,
        medium,
        high
    }

    public sealed class TestPlugin
    {
        [KernelFunction, Description("修改路口当前方案配时")]
        public string ChangeCurRoadStageTime(
            [Description("路口编号")] string roadId,
            //[Description("修改日期，可配置多个日期，通过逗号分割")] string dates,
            [Description("路口方案中各个阶段配置时间，单位秒，通过逗号分割，格式要求: 1:20,2:30,3:40，从1开始")] string times
            //[Description("第一阶段配时，单位秒")] int firstStageTime,
            //[Description("第二阶段配时，单位秒")] int secondStageTime,
            //[Description("第三阶段配时，单位秒")] int thirdStageTime,
            //[Description("是否为管理员")] bool isAdmin,
            //[Description("操作亮度")] Brightness brightness,
            )
        {
            if (!int.TryParse(roadId, out _))
            {
                return $"路口编号【{roadId}】不符合要求，请提供正确的路口编号。";
            }
            return $"路口【{roadId}】当前方案配时【{times}】修改成功。";
            //return new
            //{
            //    status = "success",
            //    result = $"路口【{roadId}】,日期【{dates}】,阶段配时【{firstStageTime}|{secondStageTime}|{thirdStageTime}】,是否管理员【{isAdmin}】,操作亮度【{brightness}】,当前方案配时修改成功。"
            //};
        }



        [KernelFunction, Description("打开CMD命令行终端")]
        public string OpenCmdWindow([Description("打开窗口数量")] int? num = 1)
        {
            // 设置默认值
            num ??= 1;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    for (int i = 0; i < num; i++)
                    {
                        SystemHelper.OpenWindowsTerminal();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    for (int i = 0; i < num; i++)
                    {
                        SystemHelper.OpenLinuxTerminal();
                    }
                }
                else
                {
                    return "不支持的操作系统。";
                }
                return "打开命令行窗口成功。";
            }
            catch (Exception e)
            {
                return "终端打开失败：" + e.Message;
            }

        }
    }
}
