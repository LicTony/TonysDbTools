using System.Collections.Generic;
using System.Threading.Tasks;
using TonysDbTools.Models;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public interface IMetadataProvider
{
    // Para JoinFinderService
    Task<List<string>> GetAllTablesAsync();
    Task<List<ForeignKeyRelation>> GetForeignKeyRelationsAsync();
    Task<List<ForeignKeyRelation>> FindImplicitJoinsAsync(string[] tableNames);

    // Para BuscarEnSPsViewModel
    Task<List<SpSearchResult>> SearchInSpsAsync(string spFilter, string textToFind);
    Task<string> GetSpCodeAsync(string spName);

    // Para BuscarTextoViewModel
    Task<List<DbSearchResult>> SearchTextInTablesAsync(string textToFind, bool exactMatch, int topPerTable);

    // Para BuscarNumeroViewModel
    Task<List<DbSearchResult>> SearchNumberInTablesAsync(decimal valueToFind, int topPerTable);

    // Para probar la conexión
    Task<bool> TestConnectionAsync();
}
