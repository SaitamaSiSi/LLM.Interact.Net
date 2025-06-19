using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LLM.Interact.Core.Core;
using LLM.Interact.Core.Models;
using LLM.Interact.UI.DTO;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Yitter.IdGenerator;
using System.Net.Http.Json;
using LLM.Interact.Core.Models.Ollama;
using Microsoft.Extensions.AI;

namespace LLM.Interact.UI
{
    public partial class MainWindow : Window
    {
        private readonly ChatManager _chatManager = new();
        private AiType CurrentChatType = AiType.Ollama;

        public ObservableCollection<MessageModel> Messages { get; } = [];
        public ObservableCollection<ImageModel> Images { get; } = [];

        public MainWindow()
        {
            InitializeComponent();

            ai_con.IsEnabled = false;
            ai_dis.IsEnabled = false;
            ai_send.IsEnabled = false;
            ai_img.IsEnabled = false;
            ai_tools.IsEnabled = false;
            ai_model.IsEnabled = false;
            ai_ask.IsEnabled = false;
            ai_url.Text = "http://192.168.100.198:11434";
            // qwen2.5:7b、gemma3:4b、deepseek-r1:8b
            // 我想知道重庆明天的天气情况?
            // 为什么天空是蓝色的?
            // 图片中有什么,用中文回答?
            // ai_ask.Text = "图片中有什么,用中文回答?";

            ai_type.SelectedIndex = 0;
            ai_type.SelectionChanged += OnAiTypeChanged;
            ai_tools.IsCheckedChanged += OnAiToolsChecked;
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
            ai_model.IsEnabled = flag;
            ai_con.IsEnabled = flag;
            ai_dis.IsEnabled = !flag;
            ai_send.IsEnabled = !flag;
            ai_img.IsEnabled = !flag;
            ai_tools.IsEnabled = !flag;
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

        private void OnAiToolsChecked(object? sender, RoutedEventArgs e)
        {
            ChatManager.ChatModels.TryGetValue(CurrentChatType, out var oldValue);
            if (oldValue != null)
            {
                var config = oldValue;
                config.IsUseTools = !config.IsUseTools;
                ChatManager.ChatModels.TryUpdate(CurrentChatType, config, oldValue);
            }
        }

        private void ConfirmClick(object? sender, RoutedEventArgs e)
        {
            string url = ai_url.Text ?? string.Empty;
            try
            {
                using (var httplient = new HttpClient { BaseAddress = new Uri(url) })
                {
                    using var response = httplient.GetAsync("/api/tags").GetAwaiter().GetResult();
                    var tags = response.Content.ReadFromJsonAsync<OllamaTags>().GetAwaiter().GetResult();
                    if (tags != null)
                    {
                        if (tags.Models.Count == 0)
                        {
                            SendTipMsg("该地址中不存在任何模型");
                        }
                        else
                        {
                            foreach (var model in tags.Models)
                            {
                                if (!string.IsNullOrEmpty(model.Name))
                                {
                                    ai_model.Items.Add(model.Name);
                                }
                            }
                            ai_type.IsEnabled = false;
                            ai_url.IsEnabled = false;
                            ai_confirm.IsEnabled = false;
                            ai_con.IsEnabled = true;
                            ai_dis.IsEnabled = true;
                            ai_send.IsEnabled = true;
                            ai_img.IsEnabled = true;
                            ai_tools.IsEnabled = true;
                            ai_model.IsEnabled = true;
                            ai_ask.IsEnabled = true;
                        }
                    }
                    else
                    {
                        SendTipMsg("获取模型数据为null");
                    }
                }
            }
            catch (Exception ex)
            {
                SendTipMsg($"获取模型失败:{ex.Message}");
            }
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            string modelName = (string)(ai_model.SelectedValue ?? string.Empty);
            if (string.IsNullOrEmpty(modelName))
            {
                SendTipMsg("请选择模型");
                return;
            }
            string url = ai_url.Text ?? string.Empty;
            if (string.IsNullOrEmpty(url))
            {
                SendTipMsg("请填写服务地址");
                return;
            }
            AIConfig config = new();
            config.Id = YitIdHelper.NextId();
            config.AiType = AiType.Ollama;
            config.Url = url;
            config.ModelName = modelName;
            config.IsUseTools = ai_tools.IsChecked ?? false;
            _chatManager.AddService(config);

            SetEnabled(false);
        }

        private void DisClick(object? sender, RoutedEventArgs e)
        {
            _chatManager.RemoveHistory(CurrentChatType);
            Messages.Clear();
            Images.Clear();

            SetEnabled(true);
        }

        private void SendClick(object? sender, RoutedEventArgs e)
        {
            if (_chatManager.IsContainsWorker(CurrentChatType))
            {
                string question = ai_ask.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(question))
                {
                    _ = Task.Factory.StartNew(async (obj) =>
                    {
                        Tuple<string>? tuple = (Tuple<string>?)obj;
                        void action() { ai_send.IsEnabled = true; ai_img.IsEnabled = true; ai_tools.IsEnabled = true; Images.Clear(); ai_communication.ScrollIntoView(Messages.Last()); }
                        if (tuple != null)
                        {
                            void action2()
                            {
                                ai_send.IsEnabled = false;
                                ai_img.IsEnabled = false;
                                ai_tools.IsEnabled = false;
                                ai_ask.Clear();
                                ai_communication.ScrollIntoView(Messages.Last());
                            }
                            AddMessage(tuple.Item1, true, action2);
                            await foreach (string ret in _chatManager.AskStreamingQuestionAsync(CurrentChatType, tuple.Item1, [.. Images.Select(model => model.Data)]))
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

        private async void ImgPickClick(object? sender, RoutedEventArgs e)
        {
            // 从当前控件获取 TopLevel。或者，您也可以使用 Window 引用。
            var topLevel = TopLevel.GetTopLevel(this);

            // 启动异步操作以打开对话框。
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image File",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll],
            });

            if (files.Count >= 1)
            {
                ImageModel pickImg = new ImageModel();
                pickImg.AbsolutePath = files[0].Path.LocalPath;
                byte[] bytes = await File.ReadAllBytesAsync(pickImg.AbsolutePath);
                pickImg.Data = Convert.ToBase64String(bytes); // 显式转换为 Base64
                Images.Add(pickImg);
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