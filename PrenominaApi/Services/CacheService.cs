using Microsoft.Extensions.Caching.Memory;

namespace PrenominaApi.Services
{
    /// <summary>
    /// Servicio de caché en memoria para datos de acceso frecuente.
    /// Reduce la carga en la base de datos y mejora el rendimiento.
    /// </summary>
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        void RemoveByPrefix(string prefix);
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _keys;
        private readonly object _lockObject = new();
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
            _keys = new HashSet<string>();
        }

        public T? Get<T>(string key)
        {
            return _cache.TryGetValue(key, out T? value) ? value : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                lock (_lockObject)
                {
                    _keys.Remove(evictedKey.ToString()!);
                }
            });

            _cache.Set(key, value, options);

            lock (_lockObject)
            {
                _keys.Add(key);
            }
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            lock (_lockObject)
            {
                _keys.Remove(key);
            }
        }

        public void RemoveByPrefix(string prefix)
        {
            List<string> keysToRemove;
            lock (_lockObject)
            {
                keysToRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                return cached!;
            }

            var value = factory();
            Set(key, value, expiration);
            return value;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                return cached!;
            }

            var value = await factory();
            Set(key, value, expiration);
            return value;
        }
    }

    /// <summary>
    /// Claves de caché predefinidas para consistencia.
    /// </summary>
    public static class CacheKeys
    {
        public const string SystemConfig = "sys_config";
        public const string YearOperation = "year_operation";
        public const string IncidentCodes = "incident_codes";
        public const string Payrolls = "payrolls_{0}"; // {0} = companyId
        public const string Periods = "periods_{0}_{1}"; // {0} = companyId, {1} = year
        public const string PeriodStatus = "period_status";
        public const string Companies = "companies";
        public const string Centers = "centers_{0}"; // {0} = companyId
        public const string Supervisors = "supervisors_{0}"; // {0} = companyId
        public const string Roles = "roles";

        public static string GetPayrollsKey(int companyId) => string.Format(Payrolls, companyId);
        public static string GetPeriodsKey(int companyId, int year) => string.Format(Periods, companyId, year);
        public static string GetCentersKey(int companyId) => string.Format(Centers, companyId);
        public static string GetSupervisorsKey(int companyId) => string.Format(Supervisors, companyId);
    }
}
