namespace FishingNetMod.Quests;

internal sealed record QuestProgress
{
    public const string CurrentVersion = "1";

    public string Version { get; set; } = CurrentVersion;

    public int SilverFishCount { get; set; }

    public int GoldFishCount { get; set; }

    public bool CopperQuestMailQueued { get; set; }

    public HashSet<string> SeasonsFished { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
