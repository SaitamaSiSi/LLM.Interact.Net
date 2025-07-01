using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LLM.Interact.Core.Core;
using LLM.Interact.Core.Models;
using LLM.Interact.UI.DTO;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Yitter.IdGenerator;
using System.Net.Http.Json;
using LLM.Interact.Core.Models.Ollama;
using LLM.Interact.UI.Services;
using LLM.Interact.Components.Confirm;
using Avalonia;
using LLM.Interact.Components.Media;
namespace LLM.Interact.UI
{
    public partial class HomePage : UserControl
    {
        private const string HomeTip = @$"
请注意：
Ollama 服务地址必须是正确的，并且服务端口必须开放。
必须选择一个模型才能使用问答功能。
必须选择支持多模态的模型才能使用图片功能。
必须选择支持工具的模型才能使用工具功能。";
        private readonly ChatManager _chatManager = new();
        private AiType CurrentChatType = AiType.Ollama;

        public ObservableCollection<MessageModel> Messages { get; } = [];
        public ObservableCollection<ImageModel> Images { get; set; } = [];

        private Window? _hostWindow;

        public HomePage()
        {
            InitializeComponent();

            // 订阅附加到可视化树的事件
            AttachedToVisualTree += OnAttachedToVisualTree;

            ai_con.IsEnabled = false;
            ai_dis.IsEnabled = false;
            ai_send.IsEnabled = false;
            ai_img.IsEnabled = false;
            check_img.IsEnabled = false;
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
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // 当控件附加到可视化树时获取窗口引用
            _hostWindow = e.Root as Window;

            // 可选：取消订阅事件避免内存泄漏
            AttachedToVisualTree -= OnAttachedToVisualTree;
        }

        private void SetEnabled(bool flag)
        {
            ai_model.IsEnabled = flag;
            ai_con.IsEnabled = flag;
            ai_dis.IsEnabled = !flag;
            ai_send.IsEnabled = !flag;
            ai_img.IsEnabled = !flag;
            check_img.IsEnabled = !flag;
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

        private async void OnAskQuestionClick(object? sender, RoutedEventArgs e)
        {
            _ = await ConfirmWindow.Show(_hostWindow!, new ConfirmParams
            {
                Title = "提示",
                Message = HomeTip,
                ShowConfirm = false,
                ShowCancel = false
            });
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
                            check_img.IsEnabled = true;
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

            LoadingService.Show("正在加载模型...");
            SetEnabled(false);
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await _chatManager.AddService(config);
                }
                catch
                {
                    SetEnabled(true);
                }
                finally
                {
                    LoadingService.Hide();
                }
            });
        }

        private async void DisClick(object? sender, RoutedEventArgs e)
        {
            await _chatManager.RemoveHistory(CurrentChatType);
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
                        void action() { ai_send.IsEnabled = true; ai_img.IsEnabled = true; check_img.IsEnabled = true; ai_tools.IsEnabled = true; Images.Clear(); ai_communication.ScrollIntoView(Messages.Last()); }
                        if (tuple != null)
                        {
                            void action2()
                            {
                                ai_send.IsEnabled = false;
                                ai_img.IsEnabled = false;
                                check_img.IsEnabled = false;
                                ai_tools.IsEnabled = false;
                                ai_ask.Clear();
                                ai_communication.ScrollIntoView(Messages.Last());
                            }
                            AddMessage(tuple.Item1, true, action2);
                            await foreach (string ret in _chatManager.AskStreamingQuestionAsync(CurrentChatType, tuple.Item1, [.. Images.Select(model => model.AbsolutePath)]))
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
                if (Images.Any(img => img.AbsolutePath == files[0].Path.LocalPath))
                {
                    SendTipMsg("已存在相同的图片，请选择其他图片");
                    return;
                }
                ImageModel pickImg = new ImageModel
                {
                    Id = YitIdHelper.NextId(),
                    AbsolutePath = files[0].Path.LocalPath,
                    Data = new Avalonia.Media.Imaging.Bitmap(files[0].Path.LocalPath)
                };
                Images.Add(pickImg);
            }
        }

        private async void ImgCheckClick(object? sender, RoutedEventArgs e)
        {
            // 创建参数
            var @params = new ImageCarouselParams
            {
                Title = "选取图片预览",
                Images = [.. Images]
            };

            // 显示弹窗并获取结果
            var result = await ImageCarousel.Show(_hostWindow!, @params);

            // 处理结果
            if (result.Count > 0)
            {
                Images = [.. result];
            }
            else
            {
                Images.Clear();
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
