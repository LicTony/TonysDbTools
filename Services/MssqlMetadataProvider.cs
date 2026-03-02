using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TonysDbTools.Models;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public class MssqlMetadataProvider : IMetadataProvider
{
    private readonly string _connectionString;

    public MssqlMetadataProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<string>> GetAllTablesAsync()
    {
        var tables = new List<string>();
        const string query = "SELECT s.name, t.name FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        return tables;
    }

    public async Task<List<ForeignKeyRelation>> GetForeignKeyRelationsAsync()
    {
        var relations = new List<ForeignKeyRelation>();
        const string query = """
            SELECT 
                fk.name                           AS ConstraintName,
                sch1.name                         AS FromSchema,
                tab1.name                         AS FromTable,
                col1.name                         AS FromColumn,
                sch2.name                         AS ToSchema,
                tab2.name                         AS ToTable,
                col2.name                         AS ToColumn
            FROM sys.foreign_key_columns fkc
            INNER JOIN sys.foreign_keys fk 
                ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.tables tab1 
                ON fkc.parent_object_id = tab1.object_id
            INNER JOIN sys.schemas sch1 
                ON tab1.schema_id = sch1.schema_id
            INNER JOIN sys.columns col1 
                ON fkc.parent_object_id = col1.object_id 
                AND fkc.parent_column_id = col1.column_id
            INNER JOIN sys.tables tab2 
                ON fkc.referenced_object_id = tab2.object_id
            INNER JOIN sys.schemas sch2 
                ON tab2.schema_id = sch2.schema_id
            INNER JOIN sys.columns col2 
                ON fkc.referenced_object_id = col2.object_id 
                AND fkc.referenced_column_id = col2.column_id
            ORDER BY fk.name, fkc.constraint_column_id
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            relations.Add(new ForeignKeyRelation
            {
                ConstraintName = reader.GetString(0),
                FromSchema = reader.GetString(1),
                FromTable = reader.GetString(2),
                FromColumn = reader.GetString(3),
                ToSchema = reader.GetString(4),
                ToTable = reader.GetString(5),
                ToColumn = reader.GetString(6),
            });
        }

        return relations;
    }

    public async Task<List<ForeignKeyRelation>> FindImplicitJoinsAsync(string[] tableNames)
    {
        var results = new List<ForeignKeyRelation>();

        const string query = """
            SELECT 
                c1.TABLE_SCHEMA AS Schema1,
                c1.TABLE_NAME   AS Table1,
                c1.COLUMN_NAME  AS Column1,
                c2.TABLE_SCHEMA AS Schema2,
                c2.TABLE_NAME   AS Table2,
                c2.COLUMN_NAME  AS Column2,
                c1.DATA_TYPE    AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS c1
            INNER JOIN INFORMATION_SCHEMA.COLUMNS c2
                ON c1.COLUMN_NAME = c2.COLUMN_NAME
                AND c1.DATA_TYPE = c2.DATA_TYPE
                AND (c1.TABLE_SCHEMA + '.' + c1.TABLE_NAME) < (c2.TABLE_SCHEMA + '.' + c2.TABLE_NAME)
            WHERE c1.TABLE_NAME IN ({0})
              AND c2.TABLE_NAME IN ({0})
            ORDER BY c1.COLUMN_NAME
            """;

        var parameters = tableNames
            .Select((name, i) => name.Contains('.') ? name.Split('.')[1] : name)
            .ToArray();

        var placeholders = string.Join(",", parameters.Select((_, i) => $"@t{i}"));
        var finalQuery = string.Format(query, placeholders);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(finalQuery, connection);
        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@t{i}", parameters[i]);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ForeignKeyRelation
            {
                ConstraintName = $"[IMPLICIT: same column name '{reader.GetString(2)}' ({reader.GetString(6)})]",
                FromSchema = reader.GetString(0),
                FromTable = reader.GetString(1),
                FromColumn = reader.GetString(2),
                ToSchema = reader.GetString(3),
                ToTable = reader.GetString(4),
                ToColumn = reader.GetString(5),
            });
        }

        return results;
    }

    public async Task<List<SpSearchResult>> SearchInSpsAsync(string spFilter, string textToFind)
    {
        var results = new List<SpSearchResult>();

        const string query = @"
            declare @part_name_sp varchar(50) = @param_sp
            declare @part_name_find varchar(50) = @param_find

            select  'sp_helptext ' + '''' + s.name + '.' + o.name + '''' as Store,
                    COUNT(*) as CantOcurrencias
            from        sysobjects o
            inner join  syscomments c on o.id = c.id
            inner join  sys.schemas s on s.schema_id = o.uid
            where
                o.name like '%' + ltrim(rtrim(@part_name_sp)) + '%'
                and (type = 'p' or type = 'v')
                and text like '%' + ltrim(rtrim(@part_name_find)) + '%'
            group by o.name, s.name
            order by 2 desc";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@param_sp", spFilter ?? "");
        command.Parameters.AddWithValue("@param_find", textToFind);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SpSearchResult
            {
                Store = reader["Store"].ToString() ?? "",
                CantOcurrencias = Convert.ToInt32(reader["CantOcurrencias"])
            });
        }

        return results;
    }

    public async Task<string> GetSpCodeAsync(string spName)
    {
        var sb = new StringBuilder();
        // spName already comes in format: sp_helptext 'schema.name' from SearchInSpsAsync results
        // or we need to handle it. 
        // In SearchInSpsAsync we return: 'sp_helptext ' + '''' + s.name + '.' + o.name + ''''
        
        string commandText = spName.StartsWith("sp_helptext") ? spName : $"sp_helptext '{spName}'";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(commandText, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sb.Append(reader[0].ToString());
        }

        return sb.ToString();
    }

    public async Task<List<DbSearchResult>> SearchTextInTablesAsync(string textToFind, bool exactMatch, int topPerTable)
    {
        var results = new List<DbSearchResult>();

        string filtroTipos = exactMatch
            ? "ty.name IN ('char', 'nchar', 'varchar', 'nvarchar')"
            : "ty.name IN ('char', 'nchar', 'varchar', 'nvarchar','text', 'ntext')";

        string body = exactMatch
            ? """
                SET @SQL = 'IF EXISTS (SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + '] WHERE [' + @ColumnName + '] = @val) ' + 
                           'BEGIN ' + 
                           'SELECT TOP ' + ltrim(@TopValesaMostrarPorTabla)+ ' ''Tabla: ' + @SchemaName + '.' + @TableName + ''' AS Tabla, ''Columna: ' + @ColumnName + ''' AS Columna, CAST([' + @ColumnName + '] AS NVARCHAR(MAX)) AS Valor ' + 
                           'FROM [' + @SchemaName + '].[' + @TableName + '] ' + 
                           'WHERE [' + @ColumnName + '] = @val ' + 
                           'ORDER BY [' + @ColumnName + ']; ' + 
                           'END;';
                """
            : """
                SET @SQL = 'IF EXISTS (SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + '] WHERE [' + @ColumnName + '] LIKE ''%'' + @val + ''%'') ' + 
                           'BEGIN ' + 
                           'SELECT TOP ' + ltrim(@TopValesaMostrarPorTabla)+ ' ''Tabla: ' + @SchemaName + '.' + @TableName + ''' AS Tabla, ''Columna: ' + @ColumnName + ''' AS Columna, CAST([' + @ColumnName + '] AS NVARCHAR(MAX)) AS Valor ' + 
                           'FROM [' + @SchemaName + '].[' + @TableName + '] ' + 
                           'WHERE [' + @ColumnName + '] LIKE ''%'' + @val + ''%'' '  + 
                           'END;';
                """;

        string query = $"""
            DECLARE @SearchValue NVARCHAR(100) = @param_find;
            DECLARE @BusquedaExacta INT = @param_exacta;
            DECLARE @TopValesaMostrarPorTabla INT = @param_top;

            DECLARE @SQL NVARCHAR(MAX);
            DECLARE @TableName NVARCHAR(256);
            DECLARE @ColumnName NVARCHAR(256);
            DECLARE @SchemaName NVARCHAR(256);

            DECLARE table_cursor CURSOR FOR
            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
            FROM sys.schemas s
            JOIN sys.tables t ON s.schema_id = t.schema_id
            JOIN sys.columns c ON t.object_id = c.object_id
            JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            WHERE {filtroTipos}

            OPEN table_cursor;
            FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                {body}    
                EXEC sp_executesql @SQL, N'@val NVARCHAR(MAX)', @val = @SearchValue;
                FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;
            END;

            CLOSE table_cursor;
            DEALLOCATE table_cursor;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@param_find", textToFind);
        command.Parameters.AddWithValue("@param_exacta", exactMatch ? 1 : 2);
        command.Parameters.AddWithValue("@param_top", topPerTable);
        command.CommandTimeout = 300;

        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                results.Add(new DbSearchResult
                {
                    Tabla = reader["Tabla"].ToString() ?? "",
                    Columna = reader["Columna"].ToString() ?? "",
                    Valor = reader["Valor"].ToString() ?? ""
                });
            }
        } while (await reader.NextResultAsync());

        return results;
    }

    public async Task<List<DbSearchResult>> SearchNumberInTablesAsync(decimal valueToFind, int topPerTable)
    {
        var results = new List<DbSearchResult>();

        string query = """
            DECLARE @SearchValue DECIMAL(38, 18) = @param_find;
            DECLARE @TopValesaMostrarPorTabla INT = @param_top;

            DECLARE @SQL NVARCHAR(MAX);
            DECLARE @TableName NVARCHAR(256);
            DECLARE @ColumnName NVARCHAR(256);
            DECLARE @SchemaName NVARCHAR(256);

            DECLARE table_cursor CURSOR FOR
            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
            FROM sys.schemas s
            JOIN sys.tables t ON s.schema_id = t.schema_id
            JOIN sys.columns c ON t.object_id = c.object_id
            JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            WHERE ty.name IN ('int', 'bigint', 'smallint', 'tinyint', 'decimal', 'numeric', 'float', 'real', 'money', 'smallmoney');

            OPEN table_cursor;
            FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @SQL = 'IF EXISTS (SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + '] WHERE [' + @ColumnName + '] = @val) ' + 
                           'BEGIN ' + 
                           'SELECT TOP ' + ltrim(@TopValesaMostrarPorTabla)+ ' ''Tabla: ' + @SchemaName + '.' + @TableName + ''' AS Tabla, ''Columna: ' + @ColumnName + ''' AS Columna, CAST([' + @ColumnName + '] AS NVARCHAR(MAX)) AS Valor ' + 
                           'FROM [' + @SchemaName + '].[' + @TableName + '] ' + 
                           'WHERE [' + @ColumnName + '] = @val ' + 
                           'ORDER BY [' + @ColumnName + ']; ' + 
                           'END;';

                EXEC sp_executesql @SQL, N'@val DECIMAL(38, 18)', @val = @SearchValue;

                FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;
            END;

            CLOSE table_cursor;
            DEALLOCATE table_cursor;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@param_find", valueToFind);
        command.Parameters.AddWithValue("@param_top", topPerTable);
        command.CommandTimeout = 300;

        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                results.Add(new DbSearchResult
                {
                    Tabla = reader["Tabla"].ToString() ?? "",
                    Columna = reader["Columna"].ToString() ?? "",
                    Valor = reader["Valor"].ToString() ?? ""
                });
            }
        } while (await reader.NextResultAsync());

        return results;
    }

    public async Task<bool> TestConnectionAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return true;
    }
}
