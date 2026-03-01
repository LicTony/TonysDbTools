using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public interface IJoinFinderService : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Encuentra todas las formas posibles de hacer JOIN entre las tablas indicadas.
    /// </summary>
    /// <param name="tableNames">
    /// Nombres de tablas. Acepta "Tabla" o "Schema.Tabla".
    /// </param>
    /// <param name="maxDepth">
    /// Profundidad máxima de búsqueda para JOINs indirectos (por defecto 4).
    /// </param>
    Task<JoinResult> FindJoinPathsAsync(string[] tableNames, int maxDepth = 4);

    /// <summary>
    /// Obtiene todos los nombres de tablas (schema.tabla) disponibles que tienen relaciones FK.
    /// </summary>
    Task<List<string>> GetAllTablesWithRelationsAsync();

    /// <summary>
    /// Obtiene también las relaciones por coincidencia de nombres de columnas.
    /// </summary>
    Task<List<ForeignKeyRelation>> FindImplicitJoinsAsync(string[] tableNames);
}
