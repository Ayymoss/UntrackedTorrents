using System.Text.Json.Serialization;

namespace UntrackedTorrents.Models;


public class Configuration
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int BatchSize { get; set; } = 100;
}
