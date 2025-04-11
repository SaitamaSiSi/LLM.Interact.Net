using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LLM.Interact.Core;
using LLM.Interact.Core.Models;
using System;
using System.Threading.Tasks;

namespace LLM.Interact.UI
{
    public partial class MainWindow : Window
    {
        private ChatHelper? chatHelper;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            if (chatHelper == null)
            {
                AIConfig config = new AIConfig();
                config.Url = "http://192.168.100.198:11434";
                chatHelper = new ChatHelper(config);
            }
        }

        private void TestClick(object? sender, RoutedEventArgs e)
        {
            if (chatHelper != null)
            {
                // 我想知道重庆今天白天的天气情况
                // What is the price of the soup special?
                string question = "我想知道重庆今天白天的天气情况";
                _ = Task.Factory.StartNew(async (obj) =>
                {
                    Tuple<string>? tuple = (Tuple<string>?)obj;
                    void action() { int a = 1; }
                    if (tuple != null)
                    {
                        string ret = await chatHelper.AskQuestionAsync(tuple.Item1);
                        ChangeShowMsg(ret);
                    }
                    else
                    {
                        ChangeShowMsg("生成失败，实体对象不可为空", action);
                    }
                }, Tuple.Create(question));
            }
        }

        private void ChangeShowMsg(string msg, Action? func = null)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                show_msg.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {msg}";
                ToolTip.SetTip(show_msg, show_msg.Text);
                func?.Invoke();
            });
        }
    }
}