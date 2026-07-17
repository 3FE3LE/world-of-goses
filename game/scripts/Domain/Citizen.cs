using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// The single person entity in the domain. A citizen may accumulate any
/// number of competencies, roles, recognitions, memberships, and ranks
/// over time. None of those concepts are modelled as subclasses: this
/// prototype composes them on the citizen so that a former miner can
/// later become a doctor, and so that hero status is a recognition rather
/// than a specialisation.
///
/// The citizen model exposes only what the current vertical slice
/// needs. It is intentionally extensible: future prototypes may add
/// health, professional history, aptitudes, relationships, and
/// expedition records without changing the citizen class's identity.
/// </summary>
public sealed class Citizen
{
    private readonly Dictionary<CompetencyId, CompetencyEntry> _competencies = new();
    private readonly List<Role> _roles = new();

    public CitizenId Id { get; }
    public string Name { get; }
    public int AppearanceSeed { get; }
    public BuildingId? CurrentAssignment { get; private set; }
    public Availability Availability => CurrentAssignment.HasValue
        ? Availability.Assigned
        : Availability.Available;

    public IReadOnlyDictionary<CompetencyId, CompetencyEntry> Competencies =>
        _competencies;
    public IReadOnlyList<Role> Roles => _roles;

    public Citizen(CitizenId id, string name, int appearanceSeed)
    {
        Id = id;
        Name = name;
        AppearanceSeed = appearanceSeed;
    }

    /// <summary>
    /// Attaches the citizen to a building as their primary workplace.
    /// Domain logic only; the presentation layer must not bypass this.
    /// </summary>
    internal void AssignTo(BuildingId buildingId) => CurrentAssignment = buildingId;

    /// <summary>
    /// Detaches the citizen from any current workplace assignment.
    /// </summary>
    internal void ClearAssignment() => CurrentAssignment = null;

    /// <summary>
    /// Records or updates the citizen's accumulated experience in a
    /// competency. New competencies are added; existing ones are updated.
    /// </summary>
    public void AddExperience(CompetencyId competency, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (_competencies.TryGetValue(competency, out var existing))
        {
            _competencies[competency] = existing.WithExperience(existing.Experience + amount);
        }
        else
        {
            _competencies[competency] = new CompetencyEntry(competency, amount);
        }
    }

    /// <summary>
    /// Returns the citizen's experience in a competency, or zero if
    /// the citizen has no recorded experience in it.
    /// </summary>
    public int GetExperience(CompetencyId competency)
    {
        return _competencies.TryGetValue(competency, out var entry) ? entry.Experience : 0;
    }

    /// <summary>
    /// Attaches a role or recognition to the citizen. Re-granting an
    /// already-held role refreshes its granted tick.
    /// </summary>
    public void GrantRole(RoleId role, int atTick)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                _roles[i] = new Role(role, atTick);
                return;
            }
        }
        _roles.Add(new Role(role, atTick));
    }

    /// <summary>
    /// Removes a previously-attached role. Returns true if a role
    /// was removed; false if the citizen did not hold it.
    /// </summary>
    public bool RevokeRole(RoleId role)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                _roles.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public bool HasRole(RoleId role)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                return true;
            }
        }
        return false;
    }
}
