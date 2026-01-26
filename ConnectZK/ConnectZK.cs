using ModelClock;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using zkemkeeper;

namespace ConnectZK
{
    public class ConnectZK : IDisposable
    {
        private static readonly ConcurrentDictionary<string, ConnectZK> _connectionPool = new ConcurrentDictionary<string, ConnectZK>();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        private IZKEM zkemkeeper;
        private string Ip;
        private int Port;
        private bool _isConnected;
        private DateTime _lastUsed;
        private bool _disposed;

        private ConnectZK(string Ip, int port)
        {
            this.Ip = Ip;
            this.Port = port;
            this._lastUsed = DateTime.UtcNow;

            Type typeZKEM = Type.GetTypeFromProgID("zkemkeeper.ZKEM");

            if (typeZKEM == null)
            {
                throw new Exception("La libreria ZK no se encuentra registrada");
            }

            zkemkeeper = (IZKEM)Activator.CreateInstance(typeZKEM, true);
        }

        public static ConnectZK GetInstance(string ip, int port = 4370)
        {
            string key = $"{ip}:{port}";
            
            if (_connectionPool.TryGetValue(key, out var instance))
            {
                instance._lastUsed = DateTime.UtcNow;
                return instance;
            }

            var newInstance = new ConnectZK(ip, port);
            _connectionPool.TryAdd(key, newInstance);
            
            return newInstance;
        }

        public static void CleanupIdleConnections(int idleMinutes = 5)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-idleMinutes);
            var keysToRemove = new List<string>();

            foreach (var kvp in _connectionPool)
            {
                if (kvp.Value._lastUsed < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                if (_connectionPool.TryRemove(key, out var instance))
                {
                    instance.Dispose();
                }
            }
        }

        private void EnsureConnected()
        {
            if (_isConnected)
            {
                return;
            }

            bool connected = zkemkeeper.Connect_Net(Ip, Port);

            if (!connected)
            {
                int ErrorCode = 0;
                zkemkeeper.GetLastError(ref ErrorCode);
                throw new Exception($"No se pudo conectar con el reloj, codigo: {ErrorCode}");
            }

            _isConnected = true;
        }

        private void Disconnect()
        {
            if (_isConnected)
            {
                zkemkeeper.Disconnect();
                _isConnected = false;
            }
        }

        public IEnumerable<User> GetUsers()
        {
            var result = new List<User>();

            try
            {
                EnsureConnected();

                string enrollNumber = string.Empty;
                string name = string.Empty;
                string password = string.Empty;
                int privilege = 0;
                bool enabled = false;

                if (!zkemkeeper.ReadAllUserID(1))
                {
                    int ErrorCode = 0;
                    zkemkeeper.GetLastError(ref ErrorCode);
                    throw new Exception($"No se pudo obtener la información de los usuarios, codigo: {ErrorCode}");
                }

                while (zkemkeeper.SSR_GetAllUserInfo(1, out enrollNumber, out name, out password, out privilege, out enabled))
                {
                    var user = new User()
                    {
                        EnrollNumber = enrollNumber,
                        Name = name,
                        Password = password,
                        Privilege = privilege,
                        Enabled = enabled,
                        UserFingers = new List<UserFinger>(),
                    };

                    result.Add(user);
                }
            }
            finally
            {
                Disconnect();
            }

            return result;
        }

        public IEnumerable<User> GetFullUsers()
        {
            var result = new List<User>();

            try
            {
                EnsureConnected();

                string enrollNumber = string.Empty;
                string name = string.Empty;
                string password = string.Empty;
                int privilege = 0;
                bool enabled = false;

                if (!zkemkeeper.ReadAllUserID(1))
                {
                    int ErrorCode = 0;
                    zkemkeeper.GetLastError(ref ErrorCode);
                    throw new Exception($"No se pudo obtener la información de los usuarios, codigo: {ErrorCode}");
                }

                if (!zkemkeeper.ReadAllTemplate(1))
                {
                    int ErrorCode = 0;
                    zkemkeeper.GetLastError(ref ErrorCode);
                    throw new Exception($"No se pudo obtener las huellas de los usuarios, codigo: {ErrorCode}");
                }

                while (zkemkeeper.SSR_GetAllUserInfo(1, out enrollNumber, out name, out password, out privilege, out enabled))
                {
                    zkemkeeper.GetStrCardNumber(out string cardNumber);
                    string faceData = "";
                    int faceLength = 0;
                    zkemkeeper.GetUserFaceStr(1, enrollNumber, 50, ref faceData, ref faceLength);

                    var user = new User()
                    {
                        EnrollNumber = enrollNumber,
                        Name = name,
                        Password = password,
                        Privilege = privilege,
                        Enabled = enabled,
                        CardNumber = cardNumber,
                        FaceBase64 = faceData,
                        FaceLength = faceLength,
                        UserFingers = new List<UserFinger>(),
                    };

                    for (int fingerIndex = 0; fingerIndex < 10; fingerIndex++)
                    {
                        if (zkemkeeper.GetUserTmpExStr(1, enrollNumber, fingerIndex, out int Flag, out string TmpData, out int TmpLength))
                        {
                            (user.UserFingers as List<UserFinger>).Add(new UserFinger()
                            {
                                EnrollNumber = enrollNumber,
                                FingerBase64 = TmpData,
                                FingerIndex = fingerIndex,
                                FingerLength = TmpLength,
                                Flag = Flag,
                            });
                        }
                    }

                    result.Add(user);
                }
            }
            finally
            {
                Disconnect();
            }

            return result;
        }

        public IEnumerable<CheckIn> GetCheckIns()
        {
            var result = new List<CheckIn>();

            try
            {
                EnsureConnected();

                string enrollNumber = string.Empty;
                int year = 0;
                int month = 0;
                int day = 0;
                int hour = 0;
                int minute = 0;
                int second = 0;
                int verifyMode = 0;
                int inOutMode = 0;
                int workCode = 0;

                zkemkeeper.ReadGeneralLogData(1);

                while (zkemkeeper.SSR_GetGeneralLogData(1, out enrollNumber, out verifyMode, out inOutMode, out year, out month, out day, out hour, out minute, out second, ref workCode))
                {
                    result.Add(new CheckIn()
                    {
                        EnrollNumber = enrollNumber,
                        Day = day,
                        Hour = hour,
                        InOutMode = inOutMode,
                        Minute = minute,
                        Month = month,
                        Second = second,
                        VerifyMode = verifyMode,
                        WorkCode = workCode,
                        Year = year,
                    });
                }
            }
            finally
            {
                Disconnect();
            }

            return result;
        }

        public void ClearCheckIns()
        {
            try
            {
                EnsureConnected();
                zkemkeeper.ClearGLog(1);
            }
            finally
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void EnableOrDisableUser(int enrollNumber, bool enable)
        {
            try
            {
                EnsureConnected();
                zkemkeeper.EnableUser(1, enrollNumber, 1, 0, enable);
            } finally
            {
                Disconnect();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Disconnect();
                
                if (zkemkeeper != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(zkemkeeper);
                    zkemkeeper = null;
                }
            }

            _disposed = true;
        }

        ~ConnectZK()
        {
            Dispose(false);
        }
    }
}
