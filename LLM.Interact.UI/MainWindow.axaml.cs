using Avalonia.Controls;
using Avalonia.Threading;
using LLM.Interact.UI.Services;
using Yitter.IdGenerator;

namespace LLM.Interact.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ���� IdGeneratorOptions ���󣬿��ڹ��캯�������� WorkerId��
            var options = new IdGeneratorOptions();
            // options.WorkerIdBitLength = 10; // Ĭ��ֵ6���޶� WorkerId ���ֵΪ2^6-1����Ĭ�����֧��64���ڵ㡣
            // options.SeqBitLength = 6; // Ĭ��ֵ6������ÿ�������ɵ�ID�������������ٶȳ���5���/�룬����Ӵ� SeqBitLength �� 10��
            // options.BaseTime = Your_Base_Time; // ���Ҫ������ϵͳ��ѩ���㷨���˴�Ӧ����Ϊ��ϵͳ��BaseTime��
            // ...... ���������ο� IdGeneratorOptions ���塣

            // �����������ص��ã�����������ò���Ч����
            YitIdHelper.SetIdGenerator(options);

            // ���ļ���״̬�仯
            LoadingService.LoadingStateChanged += OnLoadingStateChanged;
            // ȷ���ر�ʱȡ������
            Closed += (s, e) => LoadingService.LoadingStateChanged -= OnLoadingStateChanged;
        }

        private void OnLoadingStateChanged(bool isLoading, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                loadingOverlay.Message = message;
                loadingOverlay.IsActivied = isLoading;
            });
        }
    }
}