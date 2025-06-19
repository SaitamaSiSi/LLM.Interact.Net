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
            // qwen2.5:7b��gemma3:4b��deepseek-r1:8b
            // ����֪������������������?
            // Ϊʲô�������ɫ��?
            // ͼƬ����ʲô,�����Ļش�?
            // ai_ask.Text = "ͼƬ����ʲô,�����Ļش�?";

            ai_type.SelectedIndex = 0;
            ai_type.SelectionChanged += OnAiTypeChanged;
            ai_tools.IsCheckedChanged += OnAiToolsChecked;
            ai_communication.ItemsSource = Messages;

            // ���� IdGeneratorOptions ���󣬿��ڹ��캯�������� WorkerId��
            var options = new IdGeneratorOptions();
            // options.WorkerIdBitLength = 10; // Ĭ��ֵ6���޶� WorkerId ���ֵΪ2^6-1����Ĭ�����֧��64���ڵ㡣
            // options.SeqBitLength = 6; // Ĭ��ֵ6������ÿ�������ɵ�ID�������������ٶȳ���5���/�룬����Ӵ� SeqBitLength �� 10��
            // options.BaseTime = Your_Base_Time; // ���Ҫ������ϵͳ��ѩ���㷨���˴�Ӧ����Ϊ��ϵͳ��BaseTime��
            // ...... ���������ο� IdGeneratorOptions ���塣

            // �����������ص��ã�����������ò���Ч����
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
                            SendTipMsg("�õ�ַ�в������κ�ģ��");
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
                        SendTipMsg("��ȡģ������Ϊnull");
                    }
                }
            }
            catch (Exception ex)
            {
                SendTipMsg($"��ȡģ��ʧ��:{ex.Message}");
            }
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            string modelName = (string)(ai_model.SelectedValue ?? string.Empty);
            if (string.IsNullOrEmpty(modelName))
            {
                SendTipMsg("��ѡ��ģ��");
                return;
            }
            string url = ai_url.Text ?? string.Empty;
            if (string.IsNullOrEmpty(url))
            {
                SendTipMsg("����д�����ַ");
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
                            SendTipMsg("����ʧ�ܣ�ʵ����󲻿�Ϊ��", action);
                        }
                    }, Tuple.Create(question));
                }
            }
        }

        private async void ImgPickClick(object? sender, RoutedEventArgs e)
        {
            // �ӵ�ǰ�ؼ���ȡ TopLevel�����ߣ���Ҳ����ʹ�� Window ���á�
            var topLevel = TopLevel.GetTopLevel(this);

            // �����첽�����Դ򿪶Ի���
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
                pickImg.Data = Convert.ToBase64String(bytes); // ��ʽת��Ϊ Base64
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
        /// 0����ʾ��1����Ϣ��2����ʽ��Ϣ
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