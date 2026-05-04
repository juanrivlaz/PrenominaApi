using PrenominaApi.Models.Dto;

namespace PrenominaApi.Services.Utilities
{
    public static class NameFormatter
    {
        public static string Format(string? name, string? lastName, string? mLastName, NameOrder order)
        {
            var n = (name ?? string.Empty).Trim();
            var ln = (lastName ?? string.Empty).Trim();
            var mln = (mLastName ?? string.Empty).Trim();

            return order switch
            {
                NameOrder.LastNameFirst => string.Join(" ", new[] { ln, mln, n }.Where(p => !string.IsNullOrWhiteSpace(p))),
                _ => string.Join(" ", new[] { n, ln, mln }.Where(p => !string.IsNullOrWhiteSpace(p))),
            };
        }
    }
}
