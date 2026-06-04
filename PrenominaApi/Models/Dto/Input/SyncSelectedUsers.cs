namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Request body to sync selected users.
    /// </summary>
    public class SyncSelectedUsers
    {
        /// <summary>
        /// Selected employee (enroll) numbers. If null or empty, all users are synced.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
