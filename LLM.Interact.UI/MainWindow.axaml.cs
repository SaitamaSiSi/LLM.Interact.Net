using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LLM.Interact.Core;
using LLM.Interact.Core.Models;
using LLM.Interact.UI.DTO;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LLM.Interact.UI
{
    public partial class MainWindow : Window
    {
        private ChatHelper? chatHelper;
        public ObservableCollection<MessageModel> Messages { get; } = [];

        public MainWindow()
        {
            InitializeComponent();

            ai_communication.ItemsSource = Messages;
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            if (chatHelper == null)
            {
                AIConfig config = new AIConfig();
                config.Url = ai_url.Text ?? config.Url;
                config.ModelName = model_name.Text ?? config.ModelName;
                chatHelper = new ChatHelper(config);

                ai_url.IsEnabled = false;
                model_name.IsEnabled = false;
                ai_con.IsEnabled = false;
            }
        }

        private void DisClick(object? sender, RoutedEventArgs e)
        {
        }

        private void SendClick(object? sender, RoutedEventArgs e)
        {
            if (chatHelper != null)
            {
                // 我想知道重庆今天白天的天气情况
                // What is the price of the soup special?
                string question = ai_ask.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(question))
                {
                    _ = Task.Factory.StartNew(async (obj) =>
                    {
                        Tuple<string>? tuple = (Tuple<string>?)obj;
                        void action() { ai_send.IsEnabled = true; }
                        if (tuple != null)
                        {
                            void action2()
                            {
                                ai_send.IsEnabled = false;
                                ai_ask.Clear();
                                ai_communication.ScrollIntoView(Messages.Last());
                            }
                            AddMessage(tuple.Item1, true, action2);
                            string ret = await chatHelper.AskQuestionAsync(tuple.Item1);
                            SendTipMsg(ret, action, 1);
                        }
                        else
                        {
                            SendTipMsg("生成失败，实体对象不可为空", action);
                        }
                    }, Tuple.Create(question));
                }
            }
        }

        public void AddMessage(string content, bool isUser, Action? func = null)
        {
            Messages.Add(new MessageModel
            {
                IsUserMessage = isUser,
                Content = content,
                Timestamp = DateTime.Now
            });
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                func?.Invoke();
            });
        }

        private void SendTipMsg(string msg, Action? func = null, int showType = 0)
        {
            string showMsg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {msg}";
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (showType)
                {
                    default:
                    case 0:
                        show_msg.Text = showMsg;
                        ToolTip.SetTip(show_msg, show_msg.Text);
                        break;
                    case 1:
                        AddMessage(showMsg, false);
                        break;
                }
                func?.Invoke();
            });
        }
    }
}