using System;
using System.Collections.Generic;
using System.Linq;

namespace TonysDbTools.Models.Join;

/// <summary>
/// Representa un camino de JOIN entre dos tablas,
/// puede ser directo o a través de tablas intermedias.
/// </summary>
public sealed class JoinPath
{
    public List<JoinStep> Steps { get; init; } = [];
    public int Depth => Steps.Count;
    public bool IsDirect => Steps.Count == 1;

    public string Sql => GenerateSql();

    public string GenerateSql()
    {
        if (Steps.Count == 0) return string.Empty;

        var firstTable = Steps[0].FromTable;
        var lines = new List<string>
        {
            $"SELECT *",
            $"FROM {firstTable}"
        };

        foreach (var step in Steps)
        {
            lines.Add(
                $"  INNER JOIN {step.ToTable} ON {step.FromTable}.{step.FromColumn} = {step.ToTable}.{step.ToColumn}"
            );
        }

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString()
    {
        var path = string.Join(" → ",
            Steps.Select(s => $"{s.FromTable}.{s.FromColumn} = {s.ToTable}.{s.ToColumn} ({s.ConstraintName})")
        );
        return $"[Profundidad: {Depth}] {path}";
    }
}

public sealed record JoinStep
{
    public required string ConstraintName { get; init; }
    public required string FromTable { get; init; }
    public required string FromColumn { get; init; }
    public required string ToTable { get; init; }
    public required string ToColumn { get; init; }
}
