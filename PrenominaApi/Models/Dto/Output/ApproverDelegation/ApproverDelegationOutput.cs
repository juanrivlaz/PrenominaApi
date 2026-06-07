namespace PrenominaApi.Models.Dto.Output.ApproverDelegation
{
    public class ApproverDelegationOutput
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required string UserName { get; set; }
        public required Guid DelegateUserId { get; set; }
        public required string DelegateUserName { get; set; }
        public required DateOnly FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public bool IsActive { get; set; }
    }
}
