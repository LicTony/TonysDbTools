using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public sealed class JoinFinderService : IJoinFinderService, IDisposable, IAsyncDisposable
{
    private readonly IMetadataProvider _metadataProvider;

    // Cache de todas las FK de la base de datos
    private List<ForeignKeyRelation>? _allRelations;

    // Grafo de adyacencia: tabla -> lista de (tabla vecina, relación FK)
    private Dictionary<string, List<(string Neighbor, ForeignKeyRelation Relation)>>? _graph;

    public JoinFinderService(IMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// Carga todas las relaciones FK de la base de datos y construye un grafo bidireccional.
    /// </summary>
    private async Task EnsureRelationsLoadedAsync()
    {
        if (_allRelations is not null) return;

        _allRelations = [];
        _graph = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            // 1. Cargar todas las tablas primero para asegurar que existan en el grafo
            var allTables = await _metadataProvider.GetAllTablesAsync();
            foreach (var fullName in allTables)
            {
                if (!_graph.ContainsKey(fullName))
                    _graph[fullName] = [];
            }

            // 2. Cargar las relaciones FK
            var relations = await _metadataProvider.GetForeignKeyRelationsAsync();

            foreach (var relation in relations)
            {
                _allRelations.Add(relation);

                // Grafo bidireccional: la FK va en ambas direcciones para JOIN
                AddEdge(relation.FromFullName, relation.ToFullName, relation);

                // Crear relación inversa para navegar en ambos sentidos
                var inverseRelation = new ForeignKeyRelation
                {
                    ConstraintName = relation.ConstraintName,
                    FromSchema = relation.ToSchema,
                    FromTable = relation.ToTable,
                    FromColumn = relation.ToColumn,
                    ToSchema = relation.FromSchema,
                    ToTable = relation.FromTable,
                    ToColumn = relation.FromColumn,
                };
                AddEdge(relation.ToFullName, relation.FromFullName, inverseRelation);
            }
        }
        catch (Exception)
        {
            // Silently fail or handle error - for now reset cache to allow retry
            _allRelations = null;
            _graph = null;
            throw;
        }
    }

    private void AddEdge(string from, string to, ForeignKeyRelation relation)
    {
        if (!_graph!.ContainsKey(from))
            _graph[from] = [];

        _graph[from].Add((to, relation));
    }

    public async Task<List<string>> GetAllTablesWithRelationsAsync()
    {
        await EnsureRelationsLoadedAsync();
        return _graph!.Keys.OrderBy(k => k).ToList();
    }

    /// <summary>
    /// Resuelve el nombre completo de la tabla (schema.table).
    /// </summary>
    private async Task<(string? FullName, string? Error)> ResolveTableNameAsync(string input)
    {
        await EnsureRelationsLoadedAsync();

        if (string.IsNullOrWhiteSpace(input))
            return (null, "Nombre de tabla vacío.");

        // Si ya tiene schema
        if (input.Contains('.'))
        {
            var normalized = input.Trim('[', ']').Replace("[", "").Replace("]", "");
            if (_graph!.ContainsKey(normalized))
                return (normalized, null);

            // Buscar case-insensitive
            var match = _graph.Keys
                .FirstOrDefault(k => k.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            
            if (match is not null)
                return (match, null);

            return (null, $"La tabla '{input}' no se encontró o no tiene relaciones FK.");
        }

        // Buscar por nombre de tabla sin schema
        var candidates = _graph!.Keys
            .Where(k => k.Split('.')[1].Equals(input, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return candidates.Count switch
        {
            0 => (null, $"La tabla '{input}' no se encontró o no tiene relaciones FK."),
            1 => (candidates[0], null),
            _ => (null, $"La tabla '{input}' es ambigua. Existe en múltiples schemas: {string.Join(", ", candidates)}. Usa schema.tabla.")
        };
    }

    public async Task<JoinResult> FindJoinPathsAsync(string[] tableNames, int maxDepth = 4)
    {
        ArgumentNullException.ThrowIfNull(tableNames);

        if (tableNames.Length < 2)
        {
            return new JoinResult
            {
                RequestedTables = tableNames,
                Errors = ["Se necesitan al menos 2 tablas para buscar JOINs."]
            };
        }

        await EnsureRelationsLoadedAsync();

        var result = new JoinResult
        {
            RequestedTables = tableNames
        };

        // Resolver nombres completos
        var resolvedNames = new List<string>();
        foreach (var name in tableNames)
        {
            var (fullName, error) = await ResolveTableNameAsync(name);
            if (error is not null)
            {
                result.Errors.Add(error);
            }
            else
            {
                resolvedNames.Add(fullName!);
            }
        }

        if (result.Errors.Count > 0)
            return result;

        // Para 2 tablas: buscar todos los caminos entre ellas
        if (resolvedNames.Count == 2)
        {
            var paths = FindAllPaths(resolvedNames[0], resolvedNames[1], maxDepth);
            result.Paths.AddRange(paths);
        }
        else
        {
            // Para N tablas: buscar caminos que conecten TODAS las tablas
            var allPairPaths = FindPathsConnectingAllTables(resolvedNames, maxDepth);
            result.Paths.AddRange(allPairPaths);
        }

        return result;
    }

    /// <summary>
    /// Para N tablas, busca caminos que las conecten a todas.
    /// Genera JOINs encadenados visitando cada par consecutivo.
    /// </summary>
    private List<JoinPath> FindPathsConnectingAllTables(List<string> tables, int maxDepth)
    {
        var results = new List<JoinPath>();

        // Generar todas las permutaciones del orden de tablas
        var permutations = GetPermutations(tables);

        foreach (var perm in permutations)
        {
            // Para cada permutación, buscar caminos entre pares consecutivos
            var chainPaths = FindChainPaths(perm.ToList(), maxDepth);
            results.AddRange(chainPaths);
        }

        // Eliminar duplicados
        return results
            .DistinctBy(p => string.Join("|", p.Steps.Select(s => $"{s.ConstraintName}:{s.FromTable}.{s.FromColumn}->{s.ToTable}.{s.ToColumn}")))
            .OrderBy(p => p.Depth)
            .ToList();
    }

    /// <summary>
    /// Dada una lista ordenada de tablas [A, B, C], busca:
    /// caminos A→B, luego B→C, y los concatena.
    /// </summary>
    private List<JoinPath> FindChainPaths(List<string> orderedTables, int maxDepth)
    {
        // pathsPerSegment[i] = lista de caminos del segmento i→i+1
        var pathsPerSegment = new List<List<JoinPath>>();

        for (int i = 0; i < orderedTables.Count - 1; i++)
        {
            var segmentPaths = FindAllPaths(orderedTables[i], orderedTables[i + 1], maxDepth);
            if (segmentPaths.Count == 0)
                return []; // No se puede conectar este par

            pathsPerSegment.Add(segmentPaths);
        }

        // Producto cartesiano de todos los segmentos
        return CartesianProduct(pathsPerSegment)
            .Select(combination =>
            {
                var allSteps = combination.SelectMany(p => p.Steps).ToList();
                return new JoinPath { Steps = allSteps };
            })
            // Limitar profundidad total para evitar explosión
            .Where(p => p.Depth <= maxDepth * (orderedTables.Count - 1))
            .ToList();
    }

    /// <summary>
    /// Producto cartesiano de listas de JoinPath.
    /// </summary>
    private static IEnumerable<List<JoinPath>> CartesianProduct(List<List<JoinPath>> lists)
    {
        if (lists.Count == 0)
        {
            yield return [];
            yield break;
        }

        var first = lists[0];
        var rest = lists.Skip(1).ToList();

        foreach (var item in first)
        {
            foreach (var combo in CartesianProduct(rest))
            {
                var result = new List<JoinPath> { item };
                result.AddRange(combo);
                yield return result;
            }
        }
    }

    /// <summary>
    /// Genera todas las permutaciones de una lista.
    /// </summary>
    private static IEnumerable<IEnumerable<T>> GetPermutations<T>(List<T> list)
    {
        if (list.Count <= 1)
        {
            yield return list;
            yield break;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var element = list[i];
            var remaining = list.Where((_, index) => index != i).ToList();

            foreach (var perm in GetPermutations(remaining))
            {
                yield return new[] { element }.Concat(perm);
            }
        }
    }

    private List<JoinPath> FindAllPaths(string source, string target, int maxDepth)
    {
        var results = new List<JoinPath>();
        var queue = new Queue<(string CurrentNode, List<JoinStep> Steps, HashSet<string> Visited)>();

        var initialVisited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { source };
        queue.Enqueue((source, [], initialVisited));

        while (queue.Count > 0)
        {
            var (current, steps, visited) = queue.Dequeue();

            if (steps.Count > maxDepth)
                continue;

            if (current.Equals(target, StringComparison.OrdinalIgnoreCase) && steps.Count > 0)
            {
                results.Add(new JoinPath { Steps = [.. steps] });
                continue;
            }

            if (steps.Count >= maxDepth)
                continue;

            if (!_graph!.TryGetValue(current, out var neighbors))
                continue;

            foreach (var (neighbor, relation) in neighbors)
            {
                if (visited.Contains(neighbor) && !neighbor.Equals(target, StringComparison.OrdinalIgnoreCase))
                    continue;

                var step = new JoinStep
                {
                    ConstraintName = relation.ConstraintName,
                    FromTable = relation.FromFullName,
                    FromColumn = relation.FromColumn,
                    ToTable = relation.ToFullName,
                    ToColumn = relation.ToColumn,
                };

                var nextSteps = new List<JoinStep>(steps) { step };
                
                if (neighbor.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new JoinPath { Steps = nextSteps });
                    continue;
                }

                var nextVisited = new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase) { neighbor };
                queue.Enqueue((neighbor, nextSteps, nextVisited));
            }
        }

        return results
            .DistinctBy(p => string.Join("|", p.Steps.Select(s => $"{s.ConstraintName}:{s.FromTable}.{s.FromColumn}->{s.ToTable}.{s.ToColumn}")))
            .OrderBy(p => p.Depth)
            .ToList();
    }


    /// <summary>
    /// Obtiene también las relaciones por coincidencia de nombres de columnas
    /// (columnas con mismo nombre sugieren un posible JOIN natural).
    /// </summary>
    public async Task<List<ForeignKeyRelation>> FindImplicitJoinsAsync(string[] tableNames)
    {
        return await _metadataProvider.FindImplicitJoinsAsync(tableNames);
    }


    public void Dispose()
    {
        _allRelations = null;
        _graph = null;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
