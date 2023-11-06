using UntrackedTorrents.Enums;
using UntrackedTorrents.Models;

namespace UntrackedTorrents;

public class UntrackedTorrents
{
    public static async Task Main()
    {
        var configurationSetup = new ConfigurationSetup();
        var configuration = configurationSetup.GetConfiguration();
        if (configuration is null) throw new Exception("Failed to load configuration. Delete the configuration and run this again");

        var qBitTorrentClient = new QBitTorrentClient(configuration.BaseUrl, configuration.Username, configuration.Password);
        var loginSuccess = await qBitTorrentClient.Login().ConfigureAwait(false);
        if (!loginSuccess) throw new Exception("Failed to login to qBitTorrent");

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
        Console.WriteLine("========================");
        Console.WriteLine(" Untracked Torrents");
        Console.WriteLine(" By Amos - Discord: ayymoss");
        Console.WriteLine("========================");

        var badTorrents = torrents.ToList();
        if (badTorrents.Count is 0)
        {
            Console.WriteLine("No bad torrents found.");
        }
        else
        {
            Console.WriteLine("\nBad torrents\n========================");
            foreach (var torrent in badTorrents)
            {
                Console.WriteLine($"Name: {torrent.Name}");
                Console.WriteLine($"Hash: {torrent.Hash}");
                Console.WriteLine($"Reason: {torrent.FailReason}");
            }

            Console.WriteLine($"========================\nTotal: {badTorrents.Count}");
        }

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
