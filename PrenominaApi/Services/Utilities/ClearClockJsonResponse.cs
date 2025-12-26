using System.Text.RegularExpressions;

namespace PrenominaApi.Services.Utilities
{
    class ClearClockJsonResponse
    {
        public static string OutputJson(string output)
        {
            var match = Regex.Match(output, @"^\s*(\[[\s\S]*?\}\]|\[[\s\S]*?\]|\{[\s\S]*?\})");

            if (match.Success)
            {
                string jsonArray = match.Groups[1].Value;

                return jsonArray;
            }

            return string.Empty;
        }
    }
}