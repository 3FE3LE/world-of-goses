#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Catalog of the authored first night's main dialogue
/// (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>). The catalog holds
/// stable <see cref="IDialogueNode.Id"/>s and translation keys only;
/// the displayed text lives in the .po files keyed by
/// <see cref="Tr.FirstNight"/>. The UI resolves <see cref="IDialogueNode.BodyKey"/>
/// via the active <c>LocaleManager</c>.
///
/// <para>
/// Every body key is suffixed with the founder's
/// <see cref="LineageId"/>, so the spirit reacts to each lineage
/// without branching the route or exposing internal labels (doc 19
/// §13–14). The route itself is strictly linear — <see cref="IDialogueNode.Choices"/>
/// is empty and <see cref="IDialogueNode.Next"/> is <c>null</c> — and
/// the only way to advance is <c>CityWorld.TryCloseFirstNightDialogue</c>,
/// which the runner guards against closing before its trigger fires.
///
/// </para>
/// <para>
/// The catalog reuses <see cref="IDialogueNode"/> from <see cref="Dialogue"/>
/// but does NOT depend on <see cref="DialogueRunner"/>: that runner is
/// <c>async</c> and stores its position across <c>await</c>s, which
/// would break invariant 13 of doc 19 (a save interrupted mid-line
/// must resume on the same line). The runner remains intact for the
/// first NPC slice (backlog H-31); the first night persists
/// <see cref="FirstNightState.CurrentDialogueNodeId"/> instead.
/// </para>
/// </summary>
public static class FireSpiritDialogueCatalog
{
    /// <summary>Stage = <see cref="FirstNightStage.Manifested"/>. Greeting before the spirit arrives.</summary>
    public const string ManifestedGreetingId = "firstnight.manifested.greeting";

    /// <summary>Stage = <see cref="FirstNightStage.SpiritArrived"/>. Spirit greets and explains the cold.</summary>
    public const string SpiritArrivedId = "firstnight.spirit_arrived";

    /// <summary>Stage = <see cref="FirstNightStage.CampfireBuilt"/>. Spirit moves into the flame.</summary>
    public const string CampfireBuiltId = "firstnight.campfire_built";

    /// <summary>Stage = <see cref="FirstNightStage.ShelterBuilt"/>. Minimum cover acknowledged.</summary>
    public const string ShelterBuiltId = "firstnight.shelter_built";

    /// <summary>Stage = <see cref="FirstNightStage.OtherLightTold"/>. The second light and the expedition motive.</summary>
    public const string OtherLightToldId = "firstnight.other_light_told";

    /// <summary>Stage = <see cref="FirstNightStage.Sleeping"/>. The founder gives in to exhaustion.</summary>
    public const string SleepingId = "firstnight.sleeping";

    /// <summary>Every stable id the catalog exposes, in canonical order.</summary>
    public static IReadOnlyList<string> NodeIds { get; } = new[]
    {
        ManifestedGreetingId,
        SpiritArrivedId,
        CampfireBuiltId,
        ShelterBuiltId,
        OtherLightToldId,
        SleepingId,
    };

    /// <summary>The eight lineage suffixes the catalog supports.</summary>
    public static IReadOnlyList<LineageId> Lineages { get; } = new[]
    {
        LineageId.Ardhen,
        LineageId.Eirune,
        LineageId.Kovari,
        LineageId.Myrven,
        LineageId.Vaelun,
        LineageId.Orveth,
        LineageId.Caelith,
        LineageId.Theryn,
    };

    /// <summary>
    /// The node that opens at <paramref name="stage"/>, or <c>null</c>
    /// when the stage waits on a module or has no main-dialogue node
    /// (the two module-waiting stages <see cref="FirstNightStage.ColdExplained"/>
    /// and <see cref="FirstNightStage.ShelterExplained"/>, and the
    /// absorbing <see cref="FirstNightStage.Concluded"/>).
    /// </summary>
    public static IDialogueNode? NodeFor(FirstNightStage stage, LineageId lineage)
    {
        string? id = IdForStage(stage);
        if (id is null) return null;
        return new FireSpiritNode(id, BodyKeyFor(id, lineage));
    }

    /// <summary>
    /// The translation key for the body of <paramref name="nodeId"/>
    /// as spoken to a founder of <paramref name="lineage"/>. Falls back
    /// to the Ardhen variant when the lineage has no dedicated entry,
    /// so an unknown lineage never produces a missing-key error in the
    /// UI — the worst case is a generic greeting.
    /// </summary>
    public static string BodyKeyFor(string nodeId, LineageId lineage)
    {
        if (NodeBodyKeys.TryGetValue(nodeId, out IReadOnlyDictionary<string, string>? byLineage)
            && byLineage.TryGetValue(lineage.Value, out string? key))
        {
            return key;
        }
        if (NodeBodyKeys.TryGetValue(nodeId, out byLineage)
            && byLineage.TryGetValue(LineageId.Ardhen.Value, out string? fallback))
        {
            return fallback;
        }
        throw new ArgumentOutOfRangeException(
            nameof(nodeId), nodeId, "Unknown first-night dialogue node id.");
    }

    /// <summary>
    /// Validates that <paramref name="nodeId"/> is one of the catalog's
    /// stable ids. Used by the controller when restoring a save with
    /// a persisted <see cref="FirstNightState.CurrentDialogueNodeId"/>
    /// so a hand-edited or corrupt save does not crash the UI.
    /// </summary>
    public static bool IsKnownId(string? nodeId)
    {
        if (nodeId is null) return false;
        return NodeBodyKeys.ContainsKey(nodeId);
    }

    private static string? IdForStage(FirstNightStage stage) => stage switch
    {
        FirstNightStage.Manifested => ManifestedGreetingId,
        FirstNightStage.SpiritArrived => SpiritArrivedId,
        FirstNightStage.CampfireBuilt => CampfireBuiltId,
        FirstNightStage.ShelterBuilt => ShelterBuiltId,
        FirstNightStage.OtherLightTold => OtherLightToldId,
        FirstNightStage.Sleeping => SleepingId,
        FirstNightStage.ColdExplained => null,
        FirstNightStage.ShelterExplained => null,
        FirstNightStage.Concluded => null,
        _ => null,
    };

    // Frozen lookup resolved at type-initialisation time: 6 nodes × 8 lineages = 48 entries.
    // No mutation path: the catalog is a static resource, not a registry.
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
        NodeBodyKeys = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [ManifestedGreetingId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.ManifestedBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.ManifestedBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.ManifestedBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.ManifestedBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.ManifestedBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.ManifestedBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.ManifestedBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.ManifestedBodyTheryn,
            },
            [SpiritArrivedId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.SpiritArrivedBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.SpiritArrivedBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.SpiritArrivedBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.SpiritArrivedBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.SpiritArrivedBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.SpiritArrivedBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.SpiritArrivedBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.SpiritArrivedBodyTheryn,
            },
            [CampfireBuiltId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.CampfireBuiltBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.CampfireBuiltBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.CampfireBuiltBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.CampfireBuiltBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.CampfireBuiltBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.CampfireBuiltBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.CampfireBuiltBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.CampfireBuiltBodyTheryn,
            },
            [ShelterBuiltId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.ShelterBuiltBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.ShelterBuiltBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.ShelterBuiltBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.ShelterBuiltBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.ShelterBuiltBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.ShelterBuiltBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.ShelterBuiltBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.ShelterBuiltBodyTheryn,
            },
            [OtherLightToldId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.OtherLightToldBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.OtherLightToldBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.OtherLightToldBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.OtherLightToldBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.OtherLightToldBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.OtherLightToldBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.OtherLightToldBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.OtherLightToldBodyTheryn,
            },
            [SleepingId] = new Dictionary<string, string>
            {
                [LineageId.Ardhen.Value] = Tr.FirstNight.SleepingBodyArdhen,
                [LineageId.Eirune.Value] = Tr.FirstNight.SleepingBodyEirune,
                [LineageId.Kovari.Value] = Tr.FirstNight.SleepingBodyKovari,
                [LineageId.Myrven.Value] = Tr.FirstNight.SleepingBodyMyrven,
                [LineageId.Vaelun.Value] = Tr.FirstNight.SleepingBodyVaelun,
                [LineageId.Orveth.Value] = Tr.FirstNight.SleepingBodyOrveth,
                [LineageId.Caelith.Value] = Tr.FirstNight.SleepingBodyCaelith,
                [LineageId.Theryn.Value] = Tr.FirstNight.SleepingBodyTheryn,
            },
        };

    private sealed record FireSpiritNode(string Id, string BodyKey) : IDialogueNode
    {
        public string SpeakerId => SpeakerFor(Id);
        public IReadOnlyList<IDialogueChoice> Choices => Array.Empty<IDialogueChoice>();
        public IDialogueNode? Next => null;
    }

    /// <summary>Speaker id of the fire spirit. Resolved by the UI to a sprite and a name.</summary>
    public const string FireSpiritSpeakerId = "fire_spirit";

    /// <summary>
    /// The night's narrating voice — the world, not a character. Nodes carrying
    /// this speaker describe what happens; nobody in the scene utters them.
    /// </summary>
    public const string NarratorSpeakerId = "narrator";

    /// <summary>
    /// The nodes the fire spirit actually says out loud. Everything else in the
    /// authored night is narration written in the third person <em>about</em>
    /// the spirit — "El espíritu se detiene, sorprendido", "El espíritu se
    /// desliza entre las piedras" — and every node used to claim the spirit as
    /// its speaker regardless. Presented in a speech balloon that made the
    /// spirit narrate itself.
    ///
    /// <para>
    /// The criterion is mechanical and checkable against the copy: a body that
    /// refers to <em>el espíritu</em> in the third person is narration. Which
    /// beats should be rewritten <em>as</em> speech is a separate narrative
    /// call over 48 keys; this only stops the game from misattributing what is
    /// already written.
    /// </para>
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> SpokenAloudIds =
        new(StringComparer.Ordinal) { ShelterBuiltId };

    /// <summary>Speaker for <paramref name="nodeId"/>: the spirit, or the narrator.</summary>
    public static string SpeakerFor(string nodeId) =>
        SpokenAloudIds.Contains(nodeId) ? FireSpiritSpeakerId : NarratorSpeakerId;
}
