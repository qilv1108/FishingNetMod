namespace FishingNetMod.Data;

public sealed record NetLevelData(
    NetLevel Level,
    string CommandName,
    string DisplayName,
    int MinCatch,
    int MaxCatch,
    int StaminaCost)
{
    public static IReadOnlyList<NetLevelData> All { get; } = new[]
    {
        new NetLevelData(NetLevel.Copper, "copper", "Copper Fishing Net", 2, 3, 10),
        new NetLevelData(NetLevel.Iron, "iron", "Iron Fishing Net", 3, 4, 8),
        new NetLevelData(NetLevel.Gold, "gold", "Gold Fishing Net", 4, 5, 6),
        new NetLevelData(NetLevel.Iridium, "iridium", "Iridium Fishing Net", 5, 7, 4)
    };

    public static bool TryParse(string? value, out NetLevelData? data)
    {
        string normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        data = All.FirstOrDefault(candidate => candidate.CommandName == normalized);
        return data is not null;
    }

    public static NetLevelData Get(NetLevel level)
    {
        return All.First(data => data.Level == level);
    }
}
