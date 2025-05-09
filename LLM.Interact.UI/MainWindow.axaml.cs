using Avalonia;
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
using Yitter.IdGenerator;

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

            // 创建 IdGeneratorOptions 对象，可在构造函数中输入 WorkerId：
            var options = new IdGeneratorOptions();
            // options.WorkerIdBitLength = 10; // 默认值6，限定 WorkerId 最大值为2^6-1，即默认最多支持64个节点。
            // options.SeqBitLength = 6; // 默认值6，限制每毫秒生成的ID个数。若生成速度超过5万个/秒，建议加大 SeqBitLength 到 10。
            // options.BaseTime = Your_Base_Time; // 如果要兼容老系统的雪花算法，此处应设置为老系统的BaseTime。
            // ...... 其它参数参考 IdGeneratorOptions 定义。

            // 保存参数（务必调用，否则参数设置不生效）：
            YitIdHelper.SetIdGenerator(options);
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
                // 为什么天空是蓝色的?
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
                            // 带插件、非流式
                            // string ret = await chatHelper.AskQuestionAsync(tuple.Item1);
                            // SendTipMsg(ret, action, 1);
                            // 不带插件、流式
                            await foreach (string ret in chatHelper.AskStreamingQuestionAsync(tuple.Item1))
                            {
                                SendTipMsg(ret, null, 2);
                            }
                            SendTipMsg("", action, 2);
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

        public void AddStreamingMessage(string content)
        {
            var lastMsg = Messages.Last();
            if (lastMsg.IsUserMessage)
            {
                Messages.Add(new MessageModel
                {
                    IsUserMessage = false,
                    Content = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {content}",
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                lastMsg.Content += content;
            }
        }

        /// <summary>
        /// 0：提示，1：消息，2：流式消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="func"></param>
        /// <param name="showType"></param>
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
                    case 2:
                        AddStreamingMessage(msg);
                        break;
                }
                func?.Invoke();
            });
        }
    }
}