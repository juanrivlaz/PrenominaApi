namespace PrenominaApi.Models.Dto.Input.ApproverDelegation
{
    public class SaveApproverDelegation
    {
        public required Guid UserId { get; set; }
        public required Guid DelegateUserId { get; set; }
        public required DateOnly FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}
