using System.Text.Json.Serialization;
using UntrackedTorrents.Models;

namespace UntrackedTorrents.Utilities;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Configuration))]
public partial class SourceGenerationContextWriteIndented : JsonSerializerContext
{
}

[JsonSerializable(typeof(Torrent))]
[JsonSerializable(typeof(TorrentTracker))]
[JsonSerializable(typeof(List<Torrent>))]
[JsonSerializable(typeof(List<TorrentTracker>))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
