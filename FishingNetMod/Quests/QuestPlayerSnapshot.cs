using StardewValley;

namespace FishingNetMod.Quests;

internal sealed record QuestPlayerSnapshot(
    int FishingLevel,
    IReadOnlySet<string> KnownRecipes,
    IReadOnlySet<string> KnownMailFlags,
    IReadOnlySet<string>? ReceivedMailFlags = null)
{
    public static QuestPlayerSnapshot FromFarmer(Farmer player)
    {
        var knownRecipes = new HashSet<string>(player.craftingRecipes.Keys, StringComparer.OrdinalIgnoreCase);
        var receivedMailFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string mail in player.mailReceived)
            receivedMailFlags.Add(mail);

        foreach (string mail in player.mailbox)
            receivedMailFlags.Add(mail);

        var mailFlags = new HashSet<string>(receivedMailFlags, StringComparer.OrdinalIgnoreCase);

        foreach (string mail in player.mailForTomorrow)
            mailFlags.Add(mail);

        return new QuestPlayerSnapshot(player.fishingLevel.Value, knownRecipes, mailFlags, receivedMailFlags);
    }
}
