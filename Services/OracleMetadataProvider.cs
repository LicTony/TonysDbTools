using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TonysDbTools.Models;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public class OracleMetadataProvider : IMetadataProvider
{
    private readonly string _connectionString;

    public OracleMetadataProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Task<List<string>> GetAllTablesAsync()
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<List<ForeignKeyRelation>> GetForeignKeyRelationsAsync()
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<List<ForeignKeyRelation>> FindImplicitJoinsAsync(string[] tableNames)
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<List<SpSearchResult>> SearchInSpsAsync(string spFilter, string textToFind)
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<string> GetSpCodeAsync(string spName)
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<List<DbSearchResult>> SearchTextInTablesAsync(string textToFind, bool exactMatch, int topPerTable)
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<List<DbSearchResult>> SearchNumberInTablesAsync(decimal valueToFind, int topPerTable)
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }

    public Task<bool> TestConnectionAsync()
    {
        throw new NotImplementedException("Oracle support is not yet fully implemented.");
    }
}
