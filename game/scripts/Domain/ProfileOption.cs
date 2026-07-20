namespace WorldofGoses.Domain;

/// <summary>Display metadata for one selectable profile value.</summary>
public sealed record ProfileOption<TId>(TId Id, string DisplayName, string Description)
    where TId : struct;
