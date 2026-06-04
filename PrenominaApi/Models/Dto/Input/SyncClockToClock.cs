namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Syncs (copies) the users from a source clock to a target clock.
    /// </summary>
    public class SyncClockToClock
    {
        public required Guid SourceClockId { get; set; }
        public required Guid TargetClockId { get; set; }

        /// <summary>
        /// Selected employee (enroll) numbers to copy.
        /// If null or empty, all users of the source clock are copied.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
