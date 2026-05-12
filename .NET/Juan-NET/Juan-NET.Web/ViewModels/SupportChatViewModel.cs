namespace Juan_NET.Web.ViewModels
{
    public class SupportChatViewModel
    {
        public int? TicketId { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string OperatorFullName { get; set; } = "Juan Support";

        public string OperatorRole { get; set; } = "Support Operator";

        public bool IsWaitingForOperator { get; set; }

        public IReadOnlyList<SupportMessageViewModel> Messages { get; set; } = [];
    }
}
