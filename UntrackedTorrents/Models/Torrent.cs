using System.Collections.Generic;
using System.Text.Json.Serialization;
using UntrackedTorrents.Enums;

namespace UntrackedTorrents.Models;

public class Torrent
{
    [JsonIgnore] public bool Public { get; set; }
    [JsonIgnore] public FailReason FailReason { get; set; }
    [JsonIgnore] public IEnumerable<TorrentTracker> Trackers { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("hash")] public string Hash { get; set; }
}
