using System.Collections.Generic;
using System.Linq;

namespace TonysDbTools.Models.Join;

/// <summary>
/// Resultado del análisis de JOIN entre tablas.
/// </summary>
public sealed class JoinResult
{
    public required string[] RequestedTables { get; init; }
    public bool CanJoin => Paths.Count > 0;
    public int TotalPaths => Paths.Count;
    public List<JoinPath> Paths { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}
