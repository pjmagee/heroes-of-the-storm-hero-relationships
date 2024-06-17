using System.Text.Json;
using System.Text.Json.Serialization;
using Heroes.Icons.HeroesData;
using Heroes.Models;

var data = args[0];
var output = args[1];

var dataDirectory = new HeroesDataDirectory(data);
var heroData = dataDirectory.HeroData(dataDirectory.NewestVersion!, includeGameStrings: true, localization: Localization.ENUS);
var heroes = heroData.GetIds.Select(heroId => heroData.GetHeroById(heroId, true, true, true, false)).Distinct().ToList();

Node.Load(heroes);
Node.Write(output);

public class Node
{
    private static JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };
    
    public static void Write(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(Heroes.Concat(Tags), Options));
    }

    private static Node[] Heroes { get; set; } = Array.Empty<Node>();

    private static Node[] Tags { get; set; } = Array.Empty<Node>();

    public static void Load(List<Hero> heroes)
    {
        Heroes = heroes.AsParallel().Select(Node.FromHero).ToArray();
        Tags = Heroes.AsParallel().SelectMany(x => x.Imports).Distinct().Select(FromImport).ToArray();
    }

    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("imports")] public required HashSet<string> Imports { get; set; } = new();

    public static Node FromHero(Hero hero)
    {
        return new Node
        {
            Name = $"hero.name.{hero.Name!.Replace(" ", "").Replace(".", "")}",
            Imports = GetImports(hero).ToHashSet()
        };
    }

    public static Node FromImport(string import)
    {
        return new Node
        {
            Name = import,
            Imports = Heroes.Where(x => x.Imports.Contains(import)).Select(x => x.Name).ToHashSet()
        };
    }

    static IEnumerable<string> GetImports(Hero hero)
    {
        foreach (var role in hero.Roles)
            yield return $"hero.Roles.{role}";

        yield return $"hero.Roles.{hero.ExpandedRole}";
        yield return $"hero.Types.{hero.Type.PlainText}";
        yield return $"hero.Difficulties.{hero.Difficulty}";
        yield return $"hero.Energies.{hero.Energy.EnergyType?.Replace(" ", "") ?? "None"}";

        yield return hero.Gender.HasValue ? $"hero.Gender.{hero.Gender.Value}" : $"hero.Gender.None";
        yield return $"hero.Radius.Size-{hero.Radius.ToString().Replace(".", "_")}";
        
        // yield return $"hero.Rarity.{hero.Rarity}";
        // yield return "hero.Sight.Sight-" + hero.Sight.ToString().Replace(".", "_");
        // yield return "hero.LifeType." + hero.Life.LifeType ?? "";	

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Sleep", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Sleep*";

        if (hero.Talents.Any(t =>
                t.Tooltip.FullTooltip?.PlainText.Contains("Attack Speed", StringComparison.OrdinalIgnoreCase) == true &&
                t.Tooltip.FullTooltip?.PlainText.Contains("Gain", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Attack speed*";

        if (hero.Talents.Any(t =>
                t.Tooltip.FullTooltip?.PlainText.Contains("Stores", StringComparison.OrdinalIgnoreCase) == true &&
                t.Tooltip.FullTooltip?.PlainText.Contains("Charges", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Stored charges*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Shield", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Shield*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Gambit", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Gambit*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Taunt", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Taunt*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Unstoppable", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Unstoppable*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Teleport", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Teleport*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Stasis", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Stasis*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Activate to reset", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Activate to reset*";

        if (hero.Talents.Any(t =>
                t.Tooltip.FullTooltip?.PlainText.Contains("Revive", StringComparison.OrdinalIgnoreCase) == true ||
                t.Tooltip.FullTooltip?.PlainText.Contains("Resurrect", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Revive*";

        if (hero.Talents.Any(t =>
                t.Tooltip.FullTooltip?.PlainText.Contains("Takedown", StringComparison.OrdinalIgnoreCase) == true &&
                t.Tooltip.FullTooltip?.PlainText.Contains("Reset", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.Reset on Takedown*";

        if (hero.Talents.Any(t => t.Tooltip.FullTooltip?.PlainText.Contains("Bribe", StringComparison.OrdinalIgnoreCase) == true))
            yield return "hero.Talents.CampBribe*";

        if (hero.Talents.Any(t => t.IsActive))
            yield return "hero.Talents.ActiveTalent*";

        if (hero.Talents.Any(t => t.IsQuest))
            yield return "hero.Talents.QuestTalent*";

        yield return hero.UsesMount ? "hero.Mount.Mount" : "hero.Mount.NoMount";

        yield return $"hero.Ratings.Complexity-{hero.Ratings.Complexity:0#}";
        yield return $"hero.Ratings.Damage-{hero.Ratings.Damage:0#}";
        yield return $"hero.Ratings.Survivability-{hero.Ratings.Survivability:0#}";
        yield return $"hero.Ratings.Utility-{hero.Ratings.Utility:0#}";

        yield return $"hero.Franchises." + hero.Franchise;

        if (hero.SearchText == null) yield break;

        /* Macro */
        if (hero.SearchText.Contains("Double Soak")) yield return "hero.Macro.Double Soak";
        if (hero.SearchText.Contains("Offlaner")) yield return "hero.Macro.Offlaner";
        if (hero.SearchText.Contains("Push")) yield return "hero.Macro.Push";
        if (hero.SearchText.Contains("Camps")) yield return "hero.Macro.Camps";
        if (hero.SearchText.Contains("Clear")) yield return "hero.Macro.Wave clear";
        if (hero.SearchText.Contains("Mobile") || hero.SearchText.Contains("Mobility")) yield return "hero.Macro.Mobility";

        /* Style */
        if (hero.SearchText.Contains("Mage")) yield return "hero.Styles.Mage";
        if (hero.SearchText.Contains("Marksman")) yield return "hero.Styles.Marksman";
        if (hero.SearchText.Contains("Sustain")) yield return "hero.Styles.Sustain";
        if (hero.SearchText.Contains("Artillery")) yield return "hero.Styles.Artillery";
        if (hero.SearchText.Contains("Summoner")) yield return "hero.Styles.Summoner";

        /* Control */
        if (hero.SearchText.Contains("Displace")) yield return "hero.CC.Displace";
        if (hero.SearchText.Contains("Stun")) yield return "hero.CC.Stun";
        if (hero.SearchText.Contains("Root")) yield return "hero.CC.Root";
        if (hero.SearchText.Contains("Slow")) yield return "hero.CC.Slow";
        if (hero.SearchText.Contains("Sleep")) yield return "hero.CC.Sleep";
        if (hero.SearchText.Contains("Silence")) yield return "hero.CC.Silence";
        if (hero.SearchText.Contains("Blind")) yield return "hero.CC.Blind";
        if (hero.SearchText.Contains("Polymorph")) yield return "hero.CC.Polymorph";
        if (hero.SearchText.Contains("Disable")) yield return "hero.CC.Disable";
        if (hero.SearchText.Contains("Time Stop")) yield return "hero.CC.Time Stop";

        /* Micro */
        if (hero.SearchText.Contains("Roam")) yield return "hero.Micro.Roam";
        if (hero.SearchText.Contains("Gank")) yield return "hero.Micro.Gank";
        if (hero.SearchText.Contains("Dive")) yield return "hero.Micro.Dive";
        if (hero.SearchText.Contains("Burst")) yield return "hero.Micro.Burst";
        if (hero.SearchText.Contains("Stealth")) yield return "hero.Micro.Stealth";
        if (hero.SearchText.Contains("Escape")) yield return "hero.Micro.Escape";
        if (hero.SearchText.Contains("Initiation")) yield return "hero.Micro.Initiation";
    }
}