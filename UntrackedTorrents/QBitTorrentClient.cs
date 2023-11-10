using System.Text.Json;
using UntrackedTorrents.Models;

namespace UntrackedTorrents;

public class QBitTorrentClient(Configuration configuration)
{
    private readonly HttpClient _client = new();

    public async Task<bool> Login()
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                {"username", configuration.Username},
                {"password", configuration.Password}
            };

            var content = new FormUrlEncodedContent(data);
            var response = await _client.PostAsync($"{configuration.BaseUrl}/api/v2/auth/login", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

    public async Task<IEnumerable<Torrent>?> GetTorrentList()
    {
        var response = await _client.GetAsync($"{configuration.BaseUrl}/api/v2/torrents/info");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Unable to retrieve torrent list.");

        var content = await response.Content.ReadAsStringAsync();
        var torrents = JsonSerializer.Deserialize<List<Torrent>>(content);
        return torrents;
    }

    public async Task<IEnumerable<TorrentTracker>?> GetTrackersForTorrent(string torrentHash)
    {
        var response = await _client.GetAsync($"{configuration.BaseUrl}/api/v2/torrents/trackers?hash=" + torrentHash);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Unable to retrieve trackers for torrent {torrentHash}.");

        var content = await response.Content.ReadAsStringAsync();
        var trackers = JsonSerializer.Deserialize<List<TorrentTracker>>(content);
        return trackers;
    }
}
