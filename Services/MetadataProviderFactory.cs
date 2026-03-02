using System;
using TonysDbTools.Models;

namespace TonysDbTools.Services;

public static class MetadataProviderFactory
{
    public static IMetadataProvider Create(Conexion conexion)
    {
        return conexion.Provider switch
        {
            DbProvider.Mssql => new MssqlMetadataProvider(conexion.GetConnectionString()),
            DbProvider.Oracle => new OracleMetadataProvider(conexion.GetConnectionString()),
            _ => throw new NotSupportedException($"Provider {conexion.Provider} is not supported.")
        };
    }
}
