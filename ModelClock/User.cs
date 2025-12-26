using System.Collections.Generic;

namespace ModelClock
{
    public class User
    {
        public string EnrollNumber { get; set; }
        public string Name { get; set; }
        public int Privilege { get; set; }
        public string Password { get; set; }
        public bool Enabled { get; set; } //= true;
        public string CardNumber { get; set; }
        public string FaceBase64 { get; set; }
        public int FaceLength { get; set; }
        public IEnumerable<UserFinger> UserFingers { get; set; }

        public User()
        {
            Enabled = true;
        }
    }
}
