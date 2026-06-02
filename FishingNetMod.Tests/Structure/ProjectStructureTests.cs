using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace FishingNetMod.Tests.Structure;

public sealed class ProjectStructureTests
{
    [Fact]
    public void ModProjectDeclaresContentPackForBundling()
    {
        string projectPath = FindRepoFile("FishingNetMod", "FishingNetMod.csproj");
        XDocument project = XDocument.Load(projectPath);

        bool hasContentPack = project
            .Descendants("ContentPacks")
            .Any(element =>
                string.Equals((string?)element.Attribute("Include"), "[CP] FishingNetMod", StringComparison.OrdinalIgnoreCase)
                && string.Equals((string?)element.Attribute("Version"), "$(Version)", StringComparison.Ordinal));

        Assert.True(hasContentPack, "The C# mod project must declare the CP pack so build/deploy/zip includes it automatically.");
    }

    [Fact]
    public void BuildZipContainsContentPackFiles()
    {
        string repoRoot = FindRepoRoot();
        string version = GetModVersion(repoRoot);
        string zipPath = Path.Combine(repoRoot, "FishingNetMod", "bin", "Debug", "net6.0", $"FishingNetMod {version}.zip");

        Assert.True(File.Exists(zipPath), $"Expected build zip at {zipPath}.");

        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        var entries = zip.Entries.Select(entry => NormalizePath(entry.FullName)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("FishingNetMod/FishingNetMod/manifest.json", entries);
        Assert.Contains("FishingNetMod/FishingNetMod/FishingNetMod.dll", entries);
        Assert.Contains("FishingNetMod/[CP] FishingNetMod/manifest.json", entries);
        Assert.Contains("FishingNetMod/[CP] FishingNetMod/content.json", entries);
        Assert.Contains("FishingNetMod/[CP] FishingNetMod/assets/fishing_net.png", entries);
        Assert.Contains("FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json", entries);
    }

    [Fact]
    public void ProjectFilesDoNotHardCodeLocalGameInstallPath()
    {
        string modProject = File.ReadAllText(FindRepoFile("FishingNetMod", "FishingNetMod.csproj"));
        string testProject = File.ReadAllText(FindRepoFile("FishingNetMod.Tests", "FishingNetMod.Tests.csproj"));

        Assert.DoesNotContain(@"E:\SteamLibrary", modProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"E:\SteamLibrary", testProject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestProjectUsesModBuildConfigForGameReferences()
    {
        string projectPath = FindRepoFile("FishingNetMod.Tests", "FishingNetMod.Tests.csproj");
        XDocument project = XDocument.Load(projectPath);

        Assert.DoesNotContain(project.Descendants("Reference"), reference =>
            reference.Attribute("HintPath") is not null
            && string.Equals((string?)reference.Attribute("Include"), "Stardew Valley", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(project.Descendants("Reference"), reference =>
            reference.Attribute("HintPath") is not null
            && string.Equals((string?)reference.Attribute("Include"), "MonoGame.Framework", StringComparison.OrdinalIgnoreCase));

        bool hasBuildConfig = project
            .Descendants("PackageReference")
            .Any(element => string.Equals((string?)element.Attribute("Include"), "Pathoschild.Stardew.ModBuildConfig", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasBuildConfig, "The test project should use the SMAPI build package instead of hard-coded local game DLL paths.");
    }

    [Fact]
    public void CraftingRecipesQualifyCustomFishingNetObjectIds()
    {
        string contentPath = FindRepoFile("FishingNetMod", "[CP] FishingNetMod", "content.json");
        using JsonDocument content = JsonDocument.Parse(File.ReadAllText(contentPath));
        JsonElement recipes = content.RootElement
            .GetProperty("Changes")
            .EnumerateArray()
            .Single(change =>
                string.Equals(change.GetProperty("Action").GetString(), "EditData", StringComparison.OrdinalIgnoreCase)
                && string.Equals(change.GetProperty("Target").GetString(), "Data/CraftingRecipes", StringComparison.OrdinalIgnoreCase))
            .GetProperty("Entries");

        foreach (JsonProperty recipe in recipes.EnumerateObject())
        {
            string recipeData = recipe.Value.GetString() ?? string.Empty;
            string[] fields = recipeData.Split('/');
            Assert.True(fields.Length >= 3, $"Recipe {recipe.Name} should have a crafting output field.");

            string[] ingredients = fields[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ingredients.Length; i += 2)
                AssertCustomFishingNetObjectIdIsQualified(recipe.Name, "ingredient", ingredients[i]);

            AssertCustomFishingNetObjectIdIsQualified(recipe.Name, "output", fields[2]);
        }
    }

    private static string FindRepoFile(params string[] relativePathParts)
    {
        string root = FindRepoRoot();
        return Path.Combine(new[] { root }.Concat(relativePathParts).ToArray());
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "FishingNetMod", "FishingNetMod.csproj");
            if (File.Exists(candidate))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static string GetModVersion(string repoRoot)
    {
        XDocument project = XDocument.Load(Path.Combine(repoRoot, "FishingNetMod", "FishingNetMod.csproj"));
        return project.Descendants("Version").Single().Value;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static void AssertCustomFishingNetObjectIdIsQualified(string recipeName, string fieldName, string itemId)
    {
        string unqualifiedId = itemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
            ? itemId[3..]
            : itemId;

        if (!unqualifiedId.StartsWith("ChenJianCan.FishingNetMod_", StringComparison.OrdinalIgnoreCase))
            return;

        Assert.StartsWith("(O)", itemId, StringComparison.OrdinalIgnoreCase);
    }
}
