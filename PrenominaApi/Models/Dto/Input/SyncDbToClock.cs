namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Syncs (writes) the users stored in the database to a clock.
    /// </summary>
    public class SyncDbToClock
    {
        public Guid ClockId { get; set; }

        /// <summary>
        /// Selected employee (enroll) numbers to sync.
        /// If null or empty, all users are synced.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
