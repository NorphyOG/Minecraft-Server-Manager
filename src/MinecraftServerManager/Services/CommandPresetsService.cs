using System.Text.Json;
using System.Text.Json.Serialization;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public sealed class CommandPresetsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PresetFile? _file;

    public void LoadFromStream(Stream stream)
    {
        _file = JsonSerializer.Deserialize<PresetFile>(stream, JsonOptions);
    }

    public IEnumerable<(string Label, string Command)> GetPresets(ServerLoaderKind loader)
    {
        if (_file == null)
            yield break;

        var list = loader switch
        {
            ServerLoaderKind.Velocity => _file.Velocity,
            _ => _file.Paper
        };

        if (list == null)
            yield break;

        foreach (var p in list)
            yield return (p.Label, p.Command);
    }

    private sealed class PresetFile
    {
        public List<PresetItem>? Paper { get; set; }
        public List<PresetItem>? Velocity { get; set; }
    }

    private sealed class PresetItem
    {
        public string Label { get; set; } = "";
        public string Command { get; set; } = "";
    }
}
