using UntrackedTorrents.Enums;
using UntrackedTorrents.Models;

namespace UntrackedTorrents;

public class Main(Configuration configuration, QBitTorrentClient client)
{
    public async Task Run()
    {
        Console.WriteLine("Logging in to qBitTorrent...");
        var loginSuccess = await client.Login().ConfigureAwait(false);
        if (!loginSuccess) throw new Exception("Failed to login to qBitTorrent");

        Console.WriteLine("Retrieving torrent list...");
        var torrents = await client.GetTorrentList().ConfigureAwait(false);
        if (torrents is null) throw new NullReferenceException("Failed to retrieve torrent list, or no torrents found");

        var torrentsList = torrents.ToList();
        var torrentBatches = SplitList(torrentsList, configuration.BatchSize);
        var badTorrents = new List<Torrent>();

        Console.WriteLine($"\nProcessing {torrentsList.Count:N0} torrents in {torrentBatches.Count:N0} batches.");

        for (var i = 0; i < torrentBatches.Count; i++)
        {
            Console.WriteLine($"Processing batch {i + 1:N0}/{torrentBatches.Count:N0}");
            var processTasks = torrentBatches[i].Select(ProcessTorrentAsync).ToList();
            var batchResults = (await Task.WhenAll(processTasks)).SelectMany(x => x).ToList();
            badTorrents.AddRange(batchResults);
        }

        PrintResult(badTorrents);
    }

    private async Task<IEnumerable<Torrent>> ProcessTorrentAsync(Torrent torrent)
    {
        var trackers = await client.GetTrackersForTorrent(torrent.Hash).ConfigureAwait(false) ?? Enumerable.Empty<TorrentTracker>();
        var trackerList = trackers.ToList();

        if (trackerList.Count is not 0) return Enumerable.Empty<Torrent>();

        torrent.Trackers = trackerList;
        torrent.Public = trackerList.Any(x => x.Tier == 0 && x.Status != TorrentTrackerStatus.Unknown);
        torrent.FailReason = DetermineFailReason(trackerList, torrent.Public);

        return torrent.FailReason is not FailReason.Unknown ? new[] {torrent} : Enumerable.Empty<Torrent>();
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

    private static List<List<Torrent>> SplitList(List<Torrent> torrents, int batchSize)
    {
        var list = new List<List<Torrent>>();
        for (var i = 0; i < torrents.Count; i += batchSize)
        {
            list.Add(torrents.GetRange(i, Math.Min(batchSize, torrents.Count - i)));
        }

        return list;
    }

    private static void PrintResult(IEnumerable<Torrent> torrents)
    {
        Console.WriteLine("\n========================");
        Console.WriteLine(" Untracked Torrents");
        Console.WriteLine(" By Amos - Discord: ayymoss");
        Console.WriteLine("========================\n");

        var badTorrents = torrents.ToList();
        if (badTorrents.Count is 0)
        {
            Console.WriteLine("No bad torrents found.");
        }
        else
        {
            Console.WriteLine("Bad torrents");
            foreach (var torrent in badTorrents)
            {
                Console.WriteLine($"\nName: {torrent.Name}");
                Console.WriteLine($"Hash: {torrent.Hash}");
                Console.WriteLine($"Reason: {torrent.FailReason}");
            }

            Console.WriteLine($"\nTotal: {badTorrents.Count:N0}");
        }

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
