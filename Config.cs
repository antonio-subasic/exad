using System.Text.Json;
using System.Text.Json.Serialization;

namespace Exad.Config;

public class Config
{
    [JsonIgnore]
    public static string Folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "exad");

    [JsonIgnore]
    public static string Path = System.IO.Path.Combine(Folder, "config.json");

    [JsonIgnore]
    public string ClientSecretsFile = System.IO.Path.Combine(Folder, "client_secrets.json");


    [JsonPropertyName("calendar-id")]
    public string CalendarId { get; init; } = "";

    [JsonPropertyName("schoolurl")]
    public string SchoolUrl { get; init; } = "";

    [JsonPropertyName("schoolname")]
    public string SchoolName { get; init; } = "";

    [JsonPropertyName("username")]
    public string Username { get; init; } = "";

    [JsonPropertyName("password")]
    public string Password { get; init; } = "";


    public static Config Load()
    {
        if (!File.Exists(Path)) { throw new Exception("config file does not exist"); }
        else
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path)) ?? throw new Exception("config file is empty");
            
            if (config is null) { throw new Exception("config file is empty"); }
            else { return config; }
        }
    }
}
