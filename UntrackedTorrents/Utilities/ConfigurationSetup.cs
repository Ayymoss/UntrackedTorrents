using System.Reflection;
using System.Text.Json;
using Spectre.Console;
using UntrackedTorrents.Models;

namespace UntrackedTorrents.Utilities;

public class ConfigurationSetup
{
    private readonly JsonSerializerOptions _jsonOptions = new() {WriteIndented = true};
    private const string ConfigurationName = "TrackerConfiguration.json";

    public Configuration? GetConfiguration()
    {
#if DEBUG
        var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#else
        var workingDirectory = Directory.GetCurrentDirectory();
#endif
        if (workingDirectory is null) throw new NullReferenceException("Unable to retrieve working directory.");

        Configuration? configuration;
        var configurationFile = Path.Combine(workingDirectory, ConfigurationName);
        if (!File.Exists(configurationFile))
        {
            configuration = SetConfiguration(workingDirectory);
            return configuration;
        }

        var configurationJson = File.ReadAllText(configurationFile);
        configuration = JsonSerializer.Deserialize<Configuration>(configurationJson);
        return configuration;
    }

    private Configuration SetConfiguration(string workingDirectory)
    {
        AnsiConsole.Console.Write(new Rule("[yellow]qBitTorrent Configuration[/]").RuleStyle("grey").LeftJustified());
        var baseUrl = AnsiConsole.Ask<string>("Enter [green]base URL[/]: [grey](Default: http://localhost:8080)[/]");
        var username = AnsiConsole.Ask<string>("Enter [aqua]username[/]: [grey](Default: admin)[/]");
        var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter [red]password[/]: [grey](Default: adminadmin)[/]")
            .PromptStyle("Red").Secret());

        var configuration = new Configuration
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:8080" : baseUrl,
            Username = string.IsNullOrWhiteSpace(username) ? "admin" : username,
            Password = string.IsNullOrWhiteSpace(password) ? "adminadmin" : password,
        };

        if (configuration.BaseUrl.EndsWith('/'))
            configuration.BaseUrl = configuration.BaseUrl[..^1];

        var configurationJson = JsonSerializer.Serialize(configuration, _jsonOptions);
        var configurationFile = Path.Combine(workingDirectory, ConfigurationName);
        File.WriteAllText(configurationFile, configurationJson);

        return configuration;
    }
}
