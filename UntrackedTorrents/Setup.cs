using Microsoft.Extensions.DependencyInjection;
using UntrackedTorrents.Utilities;

namespace UntrackedTorrents;

public static class Setup
{
    public static async Task Main()
    {
        var configurationSetup = new ConfigurationSetup();
        var configuration = configurationSetup.GetConfiguration();
        if (configuration is null) throw new Exception("Failed to load configuration. Delete the configuration and run this again");

        var serviceCollection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddSingleton<QBitTorrentClient>()
            .AddSingleton<Main>();

        await serviceCollection
            .BuildServiceProvider()
            .GetRequiredService<Main>().Run();
    }
}
