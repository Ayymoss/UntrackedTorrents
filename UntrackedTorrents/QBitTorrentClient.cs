using System.Text.Json;
using UntrackedTorrents.Models;

namespace UntrackedTorrents;

internal class QBitTorrentClient
{
    private readonly HttpClient _client = new();
    private readonly string _username;
    private readonly string _baseUrl;
    private readonly string _password;

    public QBitTorrentClient(string baseUrl, string username, string password)
    {
        _username = username;
        _baseUrl = baseUrl;
        _password = password;
    }

    public async Task<bool> Login()
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                {"username", _username},
                {"password", _password}
            };

            var content = new FormUrlEncodedContent(data);
            var response = await _client.PostAsync($"{_baseUrl}/api/v2/auth/login", content);

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
        var response = await _client.GetAsync($"{_baseUrl}/api/v2/torrents/info");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Unable to retrieve torrent list.");

        var content = await response.Content.ReadAsStringAsync();
        var torrents = JsonSerializer.Deserialize<List<Torrent>>(content);
        return torrents;
    }

    public async Task<IEnumerable<TorrentTracker>?> GetTrackersForTorrent(string torrentHash)
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/v2/torrents/trackers?hash=" + torrentHash);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Unable to retrieve trackers for torrent {torrentHash}.");

        var content = await response.Content.ReadAsStringAsync();
        var trackers = JsonSerializer.Deserialize<List<TorrentTracker>>(content);
        return trackers;
    }
}
