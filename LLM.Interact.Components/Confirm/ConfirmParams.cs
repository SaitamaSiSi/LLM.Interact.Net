namespace LLM.Interact.Components.Confirm
{
    public class ConfirmParams
    {
        public string Title { get; set; } = "提示";
        public string Message { get; set; } = "是否继续？";
        public bool ShowConfirm { get; set; } = true;
        public bool ShowCancel { get; set; } = true;
    }
}
