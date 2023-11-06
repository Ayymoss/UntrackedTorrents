using System.Reflection;
using System.Text.Json;

namespace UntrackedTorrents;

public class ConfigurationSetup
{
    private readonly string? _workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private readonly JsonSerializerOptions _jsonOptions = new() {WriteIndented = true};
    private const string ConfigurationName = "TrackerConfiguration.json";

    public Configuration? GetConfiguration()
    {
        if (_workingDirectory is null) throw new NullReferenceException("Unable to retrieve working directory.");

        Configuration? configuration;
        var configurationFile = Path.Combine(_workingDirectory, ConfigurationName);
        if (!File.Exists(configurationFile))
        {
            configuration = SetConfiguration();
            return configuration;
        }

        var configurationJson = File.ReadAllText(configurationFile);
        configuration = JsonSerializer.Deserialize<Configuration>(configurationJson);
        return configuration;
    }

    private Configuration SetConfiguration()
    {
        if (_workingDirectory is null) throw new NullReferenceException("Unable to retrieve working directory.");

        var configuration = new Configuration
        {
            BaseUrl = "Enter the base URL for your qBittorrent instance:".PromptString(defaultValue: "http://localhost:8080"),
            Username = "Enter the username for your qBittorrent instance:".PromptString(defaultValue: "admin"),
            Password = "Enter the password for your qBittorrent instance:".PromptString(defaultValue: "adminadmin")
        };
        
        if (configuration.BaseUrl.EndsWith('/')) configuration.BaseUrl = configuration.BaseUrl[..^1];

        var configurationJson = JsonSerializer.Serialize(configuration, _jsonOptions);
        var configurationFile = Path.Combine(_workingDirectory, ConfigurationName);
        File.WriteAllText(configurationFile, configurationJson);

        return configuration;
    }
}
