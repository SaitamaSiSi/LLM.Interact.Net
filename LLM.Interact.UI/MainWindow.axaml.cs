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

            // ���� IdGeneratorOptions ���󣬿��ڹ��캯�������� WorkerId��
            var options = new IdGeneratorOptions();
            // options.WorkerIdBitLength = 10; // Ĭ��ֵ6���޶� WorkerId ���ֵΪ2^6-1����Ĭ�����֧��64���ڵ㡣
            // options.SeqBitLength = 6; // Ĭ��ֵ6������ÿ�������ɵ�ID�������������ٶȳ���5���/�룬����Ӵ� SeqBitLength �� 10��
            // options.BaseTime = Your_Base_Time; // ���Ҫ������ϵͳ��ѩ���㷨���˴�Ӧ����Ϊ��ϵͳ��BaseTime��
            // ...... ���������ο� IdGeneratorOptions ���塣

            // �����������ص��ã�����������ò���Ч����
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
                // ����֪��������������������
                // What is the price of the soup special?
                // Ϊʲô�������ɫ��?
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
                            // �����������ʽ
                            // string ret = await chatHelper.AskQuestionAsync(tuple.Item1);
                            // SendTipMsg(ret, action, 1);
                            // �����������ʽ
                            await foreach (string ret in chatHelper.AskStreamingQuestionAsync(tuple.Item1))
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