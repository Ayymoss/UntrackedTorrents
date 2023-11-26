using Humanizer;
using Spectre.Console;
using UntrackedTorrents.Enums;
using UntrackedTorrents.Models;

namespace UntrackedTorrents;

public class Main(Configuration configuration, QBitTorrentClient client)
{
    public async Task Run()
    {
        AnsiConsole.Console.Write(new Rule("[yellow]Loading Torrents[/]").RuleStyle("grey").LeftJustified());
        AnsiConsole.Console.MarkupLine("[bold yellow]Logging in to qBitTorrent...[/]");
        try
        {
            await client.Login();
        }
        catch (Exception e)
        {
            AnsiConsole.Console.MarkupLine($"[orangered1]Failed to login to qBitTorrent:[/] [red]{e.Message}[/]");
            AnsiConsole.Console.MarkupLine("\n[dim italic]Press any key to exit.[/]");
            Console.ReadKey();
            return;
        }

        AnsiConsole.Console.MarkupLine("[bold yellow]Retrieving torrent list...[/]");
        List<Torrent> torrentsList;
        try
        {
            torrentsList = await client.GetTorrentList();
        }
        catch (Exception e)
        {
            AnsiConsole.Console.MarkupLine($"[orangered1]Failed to retrieve torrent list:[/] [red]{e.Message}[/]");
            AnsiConsole.Console.MarkupLine("\n[dim italic]Press any key to exit.[/]");
            Console.ReadKey();
            return;
        }

        var torrentBatches = SplitList(torrentsList, configuration.BatchSize);
        var badTorrents = new List<Torrent>();

        AnsiConsole.Console.MarkupLine($"[green]\nProcessing {torrentsList.Count:N0} torrents in {torrentBatches.Count:N0} batches.[/]");

        for (var i = 0; i < torrentBatches.Count; i++)
        {
            AnsiConsole.Console.MarkupLine($"[blue]Processing batch {i + 1:N0} of {torrentBatches.Count:N0}[/]");
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
        torrent.Public = trackerList.Any(x => x.Tier is 0 && x.Status != TorrentTrackerStatus.Unknown);
        torrent.FailReason = DetermineFailReason(trackerList, torrent.Public);

        return torrent.FailReason is not FailReason.Unknown ? new[] {torrent} : Enumerable.Empty<Torrent>();
    }

    private static FailReason DetermineFailReason(IReadOnlyCollection<TorrentTracker> trackers, bool isPublic)
    {
        var hasWorkingTracker = trackers.Any(x => x.Status is TorrentTrackerStatus.Working);
        var hasUnregisteredTracker = trackers.Any(x =>
            x.Status is TorrentTrackerStatus.NotWorking && x.Message.Contains("Torrent not registered with this tracker"));

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
        AnsiConsole.Console.Clear();
        AnsiConsole.Console.Write(new Rule("[yellow]Untracked Torrents[/]").RuleStyle("grey").LeftJustified());

        var badTorrents = torrents.ToList();
        if (badTorrents.Count is 0)
        {
            AnsiConsole.Console.MarkupLine("\n[red]No bad torrents found.[/]");
        }
        else
        {
            AnsiConsole.Console.MarkupLine($"\n[green]Bad torrents found:[/] {badTorrents.Count:N0}");

            var table = new Table();
            table.AddColumn("Index");
            table.AddColumn("Name");
            table.AddColumn("Hash");
            table.AddColumn("Reason");

            for (var i = 0; i < badTorrents.Count; i++)
            {
                var torrent = badTorrents[i];
                table.AddRow($"{i + 1:N0} of {badTorrents.Count:N0}", torrent.Name, torrent.Hash, torrent.FailReason.Humanize());
            }

            AnsiConsole.Console.Write(table);
            AnsiConsole.Console.MarkupLine($"[blue]Total:[/] {badTorrents.Count:N0}");
        }

        AnsiConsole.Console.MarkupLine("\n[bold deepskyblue1] By Amos[/]");
        AnsiConsole.Console.MarkupLine("[deepskyblue2]  Discord: ayymoss[/]");
        AnsiConsole.Console.MarkupLine("\n[dim italic]Press any key to exit.[/]");
        Console.ReadKey();
    }
}
