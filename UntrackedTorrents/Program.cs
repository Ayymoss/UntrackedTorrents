using System.Text.Json;
using System.Text.Json.Serialization;

namespace UntrackedTorrents;

public class UntrackedTorrents
{
    public static async Task Main()
    {
        var qBitTorrentClient = new QBitTorrentClient("http://10.10.1.7:8080", "<USER>", "<PASSWORD>");
        if (!await qBitTorrentClient.Login().ConfigureAwait(false)) throw new Exception("Failed to login to qBitTorrent.");

        var torrents = await qBitTorrentClient.GetTorrentList().ConfigureAwait(false);
        if (torrents is null) throw new NullReferenceException("Failed to retrieve torrent list, or no torrents found");

        var processTasks = torrents.Select(torrent => ProcessTorrentAsync(qBitTorrentClient, torrent)).ToList();
        var badTorrents = (await Task.WhenAll(processTasks)).SelectMany(x => x).ToList();

        PrintResult(badTorrents);
    }

    private static async Task<IEnumerable<Torrent>> ProcessTorrentAsync(QBitTorrentClient client, Torrent torrent)
    {
        var trackers = await client.GetTrackersForTorrent(torrent.Hash).ConfigureAwait(false) ?? Enumerable.Empty<TorrentTracker>();
        var trackerList = trackers.ToList();

        if (trackerList.Count is not 0) return Enumerable.Empty<Torrent>();

        torrent.Trackers = trackerList;
        torrent.Public = trackerList.Any(x => x.Tier == 0 && x.Status != TorrentTrackerStatus.Unknown);
        torrent.FailReason = DetermineFailReason(trackerList, torrent.Public);

        return torrent.FailReason != FailReason.Unknown ? new[] {torrent} : Enumerable.Empty<Torrent>();
    }

    private static FailReason DetermineFailReason(IReadOnlyCollection<TorrentTracker> trackers, bool isPublic)
    {
        var hasWorkingTracker = trackers.Any(x => x.Status == TorrentTrackerStatus.Working);
        var hasUnregisteredTracker = trackers.Any(x =>
            x.Status == TorrentTrackerStatus.NotWorking && x.Message.Contains("Torrent not registered with this tracker"));

        if (isPublic) return FailReason.Unknown;
        if (!hasWorkingTracker) return FailReason.PrivateNoWorkingTrackers;
        return hasUnregisteredTracker ? FailReason.PrivateTorrentNotRegistered : FailReason.Unknown;
    }

    private static void PrintResult(IEnumerable<Torrent> torrents)
    {
        var badTorrents = torrents.ToList();
        if (badTorrents.Count is 0)
        {
            Console.WriteLine("No bad torrents found.");
            return;
        }

        Console.WriteLine("\nBad torrents\n========================");
        foreach (var torrent in badTorrents)
        {
            Console.WriteLine($"Name: {torrent.Name}");
            Console.WriteLine($"Hash: {torrent.Hash}");
            Console.WriteLine($"Reason: {torrent.FailReason}");
        }

        Console.WriteLine($"========================\nTotal: {badTorrents.Count}");
    }
}

internal class QBitTorrentClient(string baseUrl, string username, string password)
{
    private readonly HttpClient _client = new();

    public async Task<bool> Login()
    {
        var data = new Dictionary<string, string>
        {
            {"username", username},
            {"password", password}
        };

        var content = new FormUrlEncodedContent(data);
        var response = await _client.PostAsync($"{baseUrl}/api/v2/auth/login", content);

        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<Torrent>?> GetTorrentList()
    {
        var response = await _client.GetAsync($"{baseUrl}/api/v2/torrents/info");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Unable to retrieve torrent list.");

        var content = await response.Content.ReadAsStringAsync();
        var torrents = JsonSerializer.Deserialize<List<Torrent>>(content);
        return torrents;
    }

    public async Task<IEnumerable<TorrentTracker>?> GetTrackersForTorrent(string torrentHash)
    {
        var response = await _client.GetAsync($"{baseUrl}/api/v2/torrents/trackers?hash=" + torrentHash);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Unable to retrieve trackers for torrent {torrentHash}.");

        var content = await response.Content.ReadAsStringAsync();
        var trackers = JsonSerializer.Deserialize<List<TorrentTracker>>(content);
        return trackers;
    }
}

public class Torrent
{
    [JsonIgnore] public bool Public { get; set; }
    [JsonIgnore] public FailReason FailReason { get; set; }
    [JsonIgnore] public IEnumerable<TorrentTracker> Trackers { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("hash")] public string Hash { get; set; }
}

public class TorrentTracker
{
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("status")] public TorrentTrackerStatus Status { get; set; }
    [JsonPropertyName("tier")] public int Tier { get; set; }
    [JsonPropertyName("msg")] public string Message { get; set; }
}

public enum TorrentTrackerStatus
{
    Unknown = 0,
    NotContacted = 1,
    Working = 2,
    Updating = 3,
    NotWorking = 4
}

public enum FailReason
{
    Unknown,
    PrivateNoWorkingTrackers,
    PrivateTorrentNotRegistered,
}
