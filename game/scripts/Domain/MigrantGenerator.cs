#nullable enable
using System;
using System.Collections.Generic;
// The seeded PRNG lives under Combat because an encounter needed it first, but
// nothing about SplitMix64 is combat-specific and a second implementation would
// be a second sequence to keep stable across runtimes.
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Domain;

/// <summary>
/// Builds the identity of an arriving migrant.
/// </summary>
/// <remarks>
/// <para>
/// Every axis used to be <c>seed % catalogueCount</c> over the citizen id, and
/// the citizen id of the first migrant in a fresh city is always 2. Two
/// different cities therefore received the same first migrant — same lineage,
/// same gender, same affinity, same combat style, same name — and the three
/// aptitudes were three <em>consecutive</em> catalogue entries, so they were
/// never a combination either. The cube was the only genuinely varied part,
/// because it was the only one already using a hash.
/// </para>
/// <para>
/// The fix is not randomness. Offline progression and save replay require the
/// world to reproduce from what it stored, so generation stays a pure function
/// of a seed; what changes is that the seed now carries the city and the moment
/// of arrival, and that every axis is drawn from an independent stream instead
/// of from consecutive remainders of one small integer.
/// </para>
/// </remarks>
public static class MigrantGenerator
{
    /// <summary>
    /// Display names available to an arriving migrant.
    /// </summary>
    /// <remarks>
    /// Deliberately not keyed by lineage. The eight lineage documents define a
    /// visual and sonic grammar but none of them defines a naming convention,
    /// and inventing one per lineage here would be inventing lore in a domain
    /// file. Until <c>docs/world/lineages/</c> says how a Kovari name differs
    /// from a Theryn one, the pool stays common to all of them.
    /// </remarks>
    private static readonly string[] Names =
    {
        "Inara", "Tovan", "Mirel", "Sada", "Orin", "Veya", "Cael", "Neris",
        "Ashen", "Belu", "Corrin", "Dala", "Eshan", "Fenn", "Girel", "Haro",
        "Ilva", "Joran", "Kest", "Lumen", "Maren", "Noa", "Ovel", "Pira",
        "Quen", "Rovan", "Selu", "Tamsin", "Uriel", "Vesk", "Wren", "Yara",
    };

    /// <summary>
    /// Competencies a migrant may arrive already practised in. These are the
    /// professions the city actually runs on, so an arrival is someone who did
    /// a job somewhere else rather than a blank.
    /// </summary>
    private static readonly CompetencyId[] SeedableCompetencies =
    {
        CompetencyId.Mining,
        CompetencyId.Farming,
        CompetencyId.Smithing,
        CompetencyId.Construction,
        CompetencyId.Foraging,
        CompetencyId.Survival,
    };

    /// <summary>Most competencies a single migrant arrives with experience in.</summary>
    public const int MaximumSeededCompetencies = 3;

    /// <summary>
    /// Highest city-competency level a migrant can arrive at. Well below
    /// <see cref="CityCompetency.MaximumLevel"/>: an arrival can be useful on
    /// day one but never better than a citizen the player actually trained.
    /// </summary>
    public const int MaximumSeededLevel = 4;

    /// <summary>
    /// A stable number for this particular city, folded out of the founder.
    /// </summary>
    /// <remarks>
    /// The world has no seed of its own, and the founder is the one thing that
    /// is unique per playthrough and fixed for its whole life. Two cities whose
    /// founders answered the onboarding differently generate different
    /// migrants; the same save always regenerates its own.
    /// </remarks>
    public static int CitySeed(Citizen? founder)
    {
        if (founder is null) return 0;
        unchecked
        {
            uint hash = 2166136261;
            hash = Fold(hash, founder.Name);
            hash = Fold(hash, founder.Profile.Lineage.Value);
            FounderCubeProfile cube = founder.Profile.CubeProfile;
            foreach (int face in new[]
            {
                cube.Body, cube.Bond, cube.Stability,
                cube.Impulse, cube.Domain, cube.Reach,
            })
            {
                hash = (hash ^ (uint)face) * 16777619;
            }
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    /// <summary>
    /// The deterministic stream this arrival draws its whole identity from.
    /// </summary>
    public static ulong ArrivalSeed(int citySeed, int arrivalTick, int citizenId)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)citySeed) * 16777619;
            hash = (hash ^ (uint)arrivalTick) * 16777619;
            hash = (hash ^ (uint)citizenId) * 16777619;
            // Second pass so the low bits, which the first pass barely moves
            // for small ids, are as mixed as the high ones.
            hash ^= hash >> 13;
            hash *= 2654435761;
            hash ^= hash >> 16;
            return hash;
        }
    }

    /// <summary>Builds an arriving migrant's profile from one seeded stream.</summary>
    public static CitizenProfile Profile(int citySeed, int arrivalTick, int citizenId)
    {
        var random = new DeterministicRandom(ArrivalSeed(citySeed, arrivalTick, citizenId));
        return Profile(random);
    }

    /// <summary>Builds an arriving migrant's profile from an explicit stream.</summary>
    /// <remarks>
    /// Draw order is part of the contract: reordering these lines changes every
    /// migrant every existing save would regenerate. Append, never insert.
    /// </remarks>
    public static CitizenProfile Profile(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);

        LineageDefinition lineage =
            ProfileCatalog.Lineages[random.NextInt(ProfileCatalog.Lineages.Count)];
        GenderId gender = random.NextInt(2) == 0 ? GenderId.Feminine : GenderId.Masculine;
        FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(
            lineage.Id,
            random.NextInt(int.MaxValue));

        bool created = CitizenProfile.TryCreate(
            lineage.Id,
            gender,
            PickDistinct(ProfileCatalog.Aptitudes, 3, random),
            // The six DEC-0013 fields below are legacy: TryCreate still demands
            // them and nothing mechanical reads them. They are drawn rather than
            // hand-picked only so a generated citizen is not identifiably the
            // fixture citizen; when the fields go, so do these five lines.
            PickDistinct(ProfileCatalog.ProfessionFamilies, 3, random),
            ProfileCatalog.ElementalAffinities[
                random.NextInt(ProfileCatalog.ElementalAffinities.Count)].Id,
            ProfileCatalog.CombatStyles[random.NextInt(ProfileCatalog.CombatStyles.Count)].Id,
            PickDistinct(ProfileCatalog.WeaponPreferences, 2, random),
            PickDistinct(ProfileCatalog.PersonalityTraits, 3, random),
            ProfileCatalog.PoliticalOrientations[
                random.NextInt(ProfileCatalog.PoliticalOrientations.Count)].Id,
            ProfileCatalog.SpiritualPostures[
                random.NextInt(ProfileCatalog.SpiritualPostures.Count)].Id,
            cube,
            out CitizenProfile? profile,
            out string error);

        return created
            ? profile!
            : throw new InvalidOperationException(
                $"Generated migrant profile was invalid: {error}");
    }

    /// <summary>Display name for an arriving migrant.</summary>
    public static string Name(int citySeed, int arrivalTick, int citizenId) =>
        Names[(int)(ArrivalSeed(citySeed, arrivalTick, citizenId) % (ulong)Names.Length)];

    /// <summary>
    /// The professions this migrant already practised before arriving, and how
    /// much experience each carries.
    /// </summary>
    /// <remarks>
    /// Experience, not level, because experience is what the citizen stores;
    /// the level falls out of it and of whatever the migrant's own aptitudes do
    /// to the requirement. A migrant with a matching aptitude therefore arrives
    /// a little further along on the same history, which is the same rule that
    /// governs everyone afterwards.
    /// </remarks>
    public static IReadOnlyList<(CompetencyId Competency, int Experience)> SeededExperience(
        CitizenProfile profile,
        int citySeed,
        int arrivalTick,
        int citizenId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        // A second, independent stream: drawing this from the profile stream
        // would make a migrant's trade a function of their lineage draw.
        var random = new DeterministicRandom(
            ArrivalSeed(citySeed, arrivalTick, citizenId) ^ 0x5EEDC0DEUL);

        int count = random.NextInt(MaximumSeededCompetencies + 1);
        if (count <= 0) return Array.Empty<(CompetencyId, int)>();

        var remaining = new List<CompetencyId>(SeedableCompetencies);
        var seeded = new List<(CompetencyId, int)>(count);
        for (int index = 0; index < count && remaining.Count > 0; index++)
        {
            int pick = random.NextInt(remaining.Count);
            CompetencyId competency = remaining[pick];
            remaining.RemoveAt(pick);

            int level = 1 + random.NextInt(MaximumSeededLevel);
            int experience = CityCompetency.ExperienceForLevel(
                level,
                AptitudeLearning.LearningFactor(profile, competency));
            if (experience > 0) seeded.Add((competency, experience));
        }
        return seeded;
    }

    private static TId[] PickDistinct<TId>(
        IReadOnlyList<ProfileOption<TId>> options,
        int count,
        IRandomSource random)
        where TId : struct
    {
        // Sampling without replacement over a copy. The previous helper took
        // three *consecutive* catalogue entries, so a profile could only ever
        // hold one of ten adjacent triples rather than one of a hundred and
        // twenty combinations.
        var pool = new List<ProfileOption<TId>>(options);
        var picked = new TId[Math.Min(count, pool.Count)];
        for (int index = 0; index < picked.Length; index++)
        {
            int pick = random.NextInt(pool.Count);
            picked[index] = pool[pick].Id;
            pool.RemoveAt(pick);
        }
        return picked;
    }

    private static uint Fold(uint hash, string value)
    {
        unchecked
        {
            for (int index = 0; index < value.Length; index++)
            {
                hash = (hash ^ value[index]) * 16777619;
            }
            return hash;
        }
    }
}
