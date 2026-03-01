namespace TonysDbTools.Models.Join;

/// <summary>
/// Representa una relación de FK entre dos tablas.
/// </summary>
public sealed record ForeignKeyRelation
{
    public required string ConstraintName { get; init; }
    public required string FromSchema { get; init; }
    public required string FromTable { get; init; }
    public required string FromColumn { get; init; }
    public required string ToSchema { get; init; }
    public required string ToTable { get; init; }
    public required string ToColumn { get; init; }

    public string FromFullName => $"{FromSchema}.{FromTable}";
    public string ToFullName => $"{ToSchema}.{ToTable}";

    public override string ToString() =>
        $"[{ConstraintName}] {FromFullName}.{FromColumn} → {ToFullName}.{ToColumn}";
}
