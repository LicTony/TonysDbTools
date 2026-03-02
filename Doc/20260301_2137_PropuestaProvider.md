# Propuesta de Soporte Multiprovider (MSSQL, ORACLE)

Este documento detalla los cambios necesarios para que `TonysDbTools` pueda soportar múltiples motores de base de datos, abstrayendo la lógica específica de cada proveedor.

## 1. Cambios en el Modelo de Datos

### 1.1. Enum de Proveedores
Definir los motores soportados:
```csharp
public enum DbProvider
{
    Mssql,
    Oracle
}
```

### 1.2. Actualización de `Conexion`
Agregar la propiedad `Provider` a la clase base `Conexion` para identificar qué motor utiliza cada conexión guardada.

## 2. Abstracción de Metadatos (`IMetadataProvider`)

Crear una interfaz que centralice todas las consultas de catálogo (esquema) y búsquedas, eliminando el SQL embebido en los ViewModels y Servicios.

```csharp
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
}
```

## 3. Implementaciones Específicas

### 3.1. `MssqlMetadataProvider`
Mover toda la lógica actual de `Microsoft.Data.SqlClient` y las queries a `sys.tables`, `sys.foreign_keys`, `syscomments`, etc., a esta clase.

### 3.2. `OracleMetadataProvider`
Implementar la interfaz utilizando `Oracle.ManagedDataAccess.Client`. Las queries apuntarán a:
- `ALL_TABLES` / `USER_TABLES`
- `ALL_CONSTRAINTS` / `ALL_CONS_COLUMNS` (para FKs)
- `ALL_SOURCE` (para buscar en SPs)
- Búsqueda de texto dinámica adaptada a la sintaxis de Oracle (PL/SQL).

## 4. Refactorización de Servicios y ViewModels

### 4.1. `MetadataProviderFactory`
Crear una factoría para instanciar el proveedor correcto:
```csharp
public static class MetadataProviderFactory
{
    public static IMetadataProvider Create(Conexion conexion)
    {
        return conexion.Provider switch
        {
            DbProvider.Mssql => new MssqlMetadataProvider(conexion.GetConnectionString()),
            DbProvider.Oracle => new OracleMetadataProvider(conexion.GetConnectionString()),
            _ => throw new NotSupportedException()
        };
    }
}
```

### 4.2. `JoinFinderService`
Modificar el constructor para recibir un `IMetadataProvider` en lugar de una cadena de conexión. Esto lo hace totalmente agnóstico al motor de DB.

### 4.3. ViewModels (`BuscarEnSPsViewModel`, `BuscarTextoViewModel`)
- Eliminar referencias a `Microsoft.Data.SqlClient`.
- Eliminar bloques de código T-SQL.
- Utilizar el `IMetadataProvider` obtenido a través de la factoría basada en la `ConexionSeleccionada`.

## 5. Ventajas
- **Extensibilidad:** Agregar un nuevo motor (ej. PostgreSQL, MySQL) solo requiere una nueva implementación de `IMetadataProvider`.
- **Mantenibilidad:** El SQL no está disperso por la UI (ViewModels).
- **Testeo:** Permite mockear el `IMetadataProvider` para pruebas unitarias de la lógica de JOINs sin requerir una base de datos real.
