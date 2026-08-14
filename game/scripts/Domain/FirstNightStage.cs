namespace WorldofGoses.Domain;

/// <summary>
/// Stages of the authored first night (`docs/systems/first-night.md`).
/// The founder manifests at tick 0, which is Day 1 00:00 and already night, so
/// the sequence needs no clock manipulation to begin.
///
/// Every advance is triggered by a world fact — a module completed, a dialogue
/// node closed — never by a timer alone, so the player cannot lose the sequence
/// by reading slowly. <see cref="Concluded"/> is absorbing: a world in that
/// stage behaves exactly like one that never had a first night, which is also
/// how every pre-v31 save enters.
/// </summary>
public enum FirstNightStage
{
    /// <summary>Control handed over on the lineage mark. No spirit yet.</summary>
    Manifested = 0,

    /// <summary>The fire spirit arrived, drawn by the descending light.</summary>
    SpiritArrived = 1,

    /// <summary>It explained that a mortal form loses heat through the night.</summary>
    ColdExplained = 2,

    /// <summary>The Campfire module is finished; the spirit inhabits the flame.</summary>
    CampfireBuilt = 3,

    /// <summary>It explained that warmth alone answers neither wind nor sleep.</summary>
    ShelterExplained = 4,

    /// <summary>The Bedroll module is finished: the minimum shelter exists.</summary>
    ShelterBuilt = 5,

    /// <summary>They spoke about the fall and the second light.</summary>
    OtherLightTold = 6,

    /// <summary>The founder gave in to exhaustion in the shelter.</summary>
    Sleeping = 7,

    /// <summary>Dawn. The spirit is gone and normal simulation resumes.</summary>
    Concluded = 8,
}
