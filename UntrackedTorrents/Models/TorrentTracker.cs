using System.Text.Json.Serialization;
using UntrackedTorrents.Enums;

namespace UntrackedTorrents.Models;

public class TorrentTracker
{
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("status")] public TorrentTrackerStatus Status { get; set; }
    [JsonPropertyName("tier")] public int Tier { get; set; }
    [JsonPropertyName("msg")] public string Message { get; set; }
}
