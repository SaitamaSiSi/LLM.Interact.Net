using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LLM.Interact.Core.Core;
using LLM.Interact.Core.Models;
using LLM.Interact.Core.Plugins;
using LLM.Interact.Core.Plugins.Amap;
using LLM.Interact.UI.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Yitter.IdGenerator;

namespace LLM.Interact.UI
{
    public partial class MainWindow : Window
    {
        private readonly ChatManager _chatManager = new();
        private AiType CurrentChatType = AiType.Ollama;
        private ConcurrentDictionary<AiType, AIConfig> ChatConfigs = new();

        public ObservableCollection<MessageModel> Messages { get; } = [];

        public MainWindow()
        {
            InitializeComponent();

            ai_test.IsVisible = false;

            ai_type.SelectedIndex = 0;
            ai_type.SelectionChanged += OnAiTypeChanged;
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

        private void SetEnabled(bool flag)
        {
            ai_url.IsEnabled = flag;
            model_name.IsEnabled = flag;
            ai_con.IsEnabled = flag;
            ai_dis.IsEnabled = !flag;
            ai_send.IsEnabled = !flag;
        }

        private void OnAiTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                switch (ai_type.SelectedIndex)
                {
                    case 0:
                        // Ollama
                        CurrentChatType = AiType.Ollama;
                        break;
                }

                if (_chatManager.IsContainsWorker(CurrentChatType))
                {
                    SetEnabled(false);
                }
                else
                {
                    SetEnabled(true);
                }
            }
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            if (!ChatConfigs.ContainsKey(CurrentChatType))
            {
                AIConfig config = new();
                config.Id = YitIdHelper.NextId();
                config.AiType = AiType.Ollama;
                config.Url = ai_url.Text ?? config.Url;
                config.ModelName = model_name.Text ?? config.ModelName;
                _chatManager.AddService(config);
                ChatConfigs.TryAdd(CurrentChatType, config);

                SetEnabled(false);
            }
        }

        private void DisClick(object? sender, RoutedEventArgs e)
        {
            ChatConfigs.Remove(CurrentChatType, out _);
            _chatManager.RemoveWorker(CurrentChatType);
            Messages.Clear();

            SetEnabled(true);
        }

        private void TestClick(object? sender, RoutedEventArgs e)
        {
            AmapWeatherTool t = new AmapWeatherTool();
            t.MapsWeather("重庆");
        }

        private void SendClick(object? sender, RoutedEventArgs e)
        {
            if (_chatManager.IsContainsWorker(CurrentChatType))
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
                        void action() { ai_send.IsEnabled = true; ai_communication.ScrollIntoView(Messages.Last()); }
                        if (tuple != null)
                        {
                            void action2()
                            {
                                ai_send.IsEnabled = false;
                                ai_ask.Clear();
                                ai_communication.ScrollIntoView(Messages.Last());
                            }
                            AddMessage(tuple.Item1, true, action2);
                            await foreach (string ret in _chatManager.AskStreamingQuestionAsync(CurrentChatType, tuple.Item1))
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