using Runescape_tracker.DataStructures;
using System.Text.Json;

namespace Runescape_tracker
{
    public class RuneMetricsClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<PlayerData> GetProfileAsync(string username)
        {
            var url = $"https://apps.runescape.com/runemetrics/profile/profile?user={username}&activities=0";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<PlayerData>(json);

            return profile;
        }
    }
}