using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using TonysDbTools.Models;
using TonysDbTools.Models.Join;

namespace TonysDbTools.Services;

public class OracleMetadataProvider : IMetadataProvider
{
    private readonly string _connectionString;
    //private readonly int _commandTimeout = 1200;

    public OracleMetadataProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetAllTablesAsync()
    {
        try
        {
            var tables = new List<string>();
            const string query = "SELECT OWNER, TABLE_NAME FROM ALL_TABLES WHERE OWNER = 'ELECCIONES_DBA_DESA' ORDER BY OWNER, TABLE_NAME";
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new OracleCommand(query, connection);
            //command.CommandTimeout = _commandTimeout;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
            }
            return tables;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching tables: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<ForeignKeyRelation>> GetForeignKeyRelationsAsync()
    {
        var relations = new List<ForeignKeyRelation>();
        const string query = @"
            SELECT
                a.constraint_name AS ConstraintName,
                a.owner AS FromSchema,
                a.table_name AS FromTable,
                a.column_name AS FromColumn,
                c.owner AS ToSchema,
                c.table_name AS ToTable,
                d.column_name AS ToColumn
            FROM all_cons_columns a
            JOIN all_constraints b ON a.constraint_name = b.constraint_name AND a.owner = b.owner
            JOIN all_constraints c ON b.r_constraint_name = c.constraint_name AND b.r_owner = c.owner
            JOIN all_cons_columns d ON c.constraint_name = d.constraint_name AND c.owner = d.owner AND a.position = d.position
            WHERE b.constraint_type = 'R'
            ORDER BY a.owner, a.table_name, a.constraint_name, a.position";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(query, connection);
        //command.CommandTimeout = _commandTimeout;
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

        const string queryTemplate = @"
            SELECT 
                c1.OWNER AS Schema1,
                c1.TABLE_NAME AS Table1,
                c1.COLUMN_NAME AS Column1,
                c2.OWNER AS Schema2,
                c2.TABLE_NAME AS Table2,
                c2.COLUMN_NAME AS Column2,
                c1.DATA_TYPE AS DataType
            FROM ALL_TAB_COLUMNS c1
            JOIN ALL_TAB_COLUMNS c2 
                ON c1.COLUMN_NAME = c2.COLUMN_NAME 
                AND c1.DATA_TYPE = c2.DATA_TYPE
                AND (c1.OWNER || '.' || c1.TABLE_NAME) < (c2.OWNER || '.' || c2.TABLE_NAME)
            WHERE c1.TABLE_NAME IN ({0})
              AND c2.TABLE_NAME IN ({0})
            ORDER BY c1.COLUMN_NAME";

        var tableNamesOnly = tableNames
            .Select(name => name.Contains('.') ? name.Split('.')[1] : name)
            .ToArray();

        var placeholders = string.Join(",", tableNamesOnly.Select((_, i) => $":t{i}"));
        var finalQuery = string.Format(queryTemplate, placeholders);

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(finalQuery, connection);
        //command.CommandTimeout = _commandTimeout;
        for (int i = 0; i < tableNamesOnly.Length; i++)
        {
            command.Parameters.Add($":t{i}", tableNamesOnly[i]);
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
            SELECT 
                OWNER || '.' || NAME AS Store,
                COUNT(*) AS CantOcurrencias
            FROM ALL_SOURCE
            WHERE (TYPE IN ('PROCEDURE', 'FUNCTION', 'PACKAGE', 'PACKAGE BODY'))
              AND NAME LIKE '%' || UPPER(:spFilter) || '%'
              AND UPPER(TEXT) LIKE '%' || UPPER(:textToFind) || '%'
            GROUP BY OWNER, NAME
            ORDER BY CantOcurrencias DESC";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(query, connection);
        //command.CommandTimeout = _commandTimeout;
        command.Parameters.Add(":spFilter", spFilter?.ToUpper() ?? "");
        command.Parameters.Add(":textToFind", textToFind?.ToUpper() ?? "");

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SpSearchResult
            {
                Store = reader.GetString(0),
                CantOcurrencias = Convert.ToInt32(reader.GetValue(1))
            });
        }

        return results;
    }

    public async Task<string> GetSpCodeAsync(string spName)
    {
        var sb = new StringBuilder();
        string owner = "";
        string name = spName;

        if (spName.Contains('.'))
        {
            var parts = spName.Split('.');
            owner = parts[0];
            name = parts[1];
        }

        const string query = @"
            SELECT TEXT
            FROM ALL_SOURCE
            WHERE OWNER = :owner AND NAME = :name
            ORDER BY LINE";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(query, connection);
        //command.CommandTimeout = _commandTimeout;
        command.Parameters.Add(":owner", owner);
        command.Parameters.Add(":name", name);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sb.Append(reader.GetString(0));
        }

        return sb.ToString();
    }

    public async Task<List<DbSearchResult>> SearchTextInTablesAsync(string textToFind, bool exactMatch, int topPerTable)
    {
        var results = new List<DbSearchResult>();

        string operatorSql = exactMatch ? "=" : "LIKE";
        string valueSql = exactMatch ? textToFind : $"%{textToFind}%";

        string query = $@"
            DECLARE
              l_cursor SYS_REFCURSOR;
              l_sql VARCHAR2(32767);
            BEGIN
              FOR r IN (
                SELECT OWNER, TABLE_NAME, COLUMN_NAME 
                FROM ALL_TAB_COLUMNS 
                WHERE DATA_TYPE IN ('CHAR', 'VARCHAR2', 'NCHAR', 'NVARCHAR2', 'CLOB')
                  AND OWNER NOT IN ('SYS', 'SYSTEM', 'OUTLN', 'APPQOSSYS', 'DBSNMP', 'CTXSYS', 'XDB', 'WMSYS')
              ) LOOP
                l_sql := 'SELECT ' || 
                         '  ''Tabla: '' || :owner || ''.'' || :table AS Tabla, ' ||
                         '  ''Columna: '' || :column AS Columna, ' ||
                         '  CAST( ""' || r.COLUMN_NAME || '"" AS VARCHAR2(4000)) AS Valor ' ||
                         'FROM ""' || r.OWNER || '"".""' || r.TABLE_NAME || '"" ' ||
                         'WHERE ""' || r.COLUMN_NAME || '"" {operatorSql} :val ' ||
                         'AND ROWNUM <= :top';
                
                -- Check if any records exist before returning a result set to keep it clean
                -- But to avoid extra round-trips, we can just try to open and return.
                -- However, returning thousands of empty cursors might be slow.
                -- Let's check existence first.
                
                EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ""' || r.OWNER || '"".""' || r.TABLE_NAME || '"" WHERE ""' || r.COLUMN_NAME || '"" {operatorSql} :val AND ROWNUM = 1' 
                INTO l_sql 
                USING valueSql;
                
                IF TO_NUMBER(l_sql) > 0 THEN
                  OPEN l_cursor FOR 'SELECT ' || 
                                   '  ''Tabla: '' || :owner || ''.'' || :table AS Tabla, ' ||
                                   '  ''Columna: '' || :column AS Columna, ' ||
                                   '  CAST( ""' || r.COLUMN_NAME || '"" AS VARCHAR2(4000)) AS Valor ' ||
                                   'FROM ""' || r.OWNER || '"".""' || r.TABLE_NAME || '"" ' ||
                                   'WHERE ""' || r.COLUMN_NAME || '"" {operatorSql} :val ' ||
                                   'AND ROWNUM <= :top'
                  USING r.OWNER, r.TABLE_NAME, r.COLUMN_NAME, valueSql, topPerTable;
                  
                  DBMS_SQL.RETURN_RESULT(l_cursor);
                END IF;
              END LOOP;
            END;";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(query, connection);
        //command.CommandTimeout = _commandTimeout;
        // We use string interpolation for the operator but bind parameters for the value
        command.Parameters.Add(":val", valueSql);
        command.Parameters.Add(":top", topPerTable);
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

        string query = $@"
            DECLARE
              l_cursor SYS_REFCURSOR;
              l_sql VARCHAR2(32767);
            BEGIN
              FOR r IN (
                SELECT OWNER, TABLE_NAME, COLUMN_NAME 
                FROM ALL_TAB_COLUMNS 
                WHERE DATA_TYPE IN ('NUMBER', 'FLOAT')
                  AND OWNER NOT IN ('SYS', 'SYSTEM', 'OUTLN', 'APPQOSSYS', 'DBSNMP', 'CTXSYS', 'XDB', 'WMSYS')
              ) LOOP
                
                EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ""' || r.OWNER || '"".""' || r.TABLE_NAME || '"" WHERE ""' || r.COLUMN_NAME || '"" = :val AND ROWNUM = 1' 
                INTO l_sql 
                USING valueToFind;
                
                IF TO_NUMBER(l_sql) > 0 THEN
                  OPEN l_cursor FOR 'SELECT ' || 
                                   '  ''Tabla: '' || :owner || ''.'' || :table AS Tabla, ' ||
                                   '  ''Columna: '' || :column AS Columna, ' ||
                                   '  CAST( ""' || r.COLUMN_NAME || '"" AS VARCHAR2(4000)) AS Valor ' ||
                                   'FROM ""' || r.OWNER || '"".""' || r.TABLE_NAME || '"" ' ||
                                   'WHERE ""' || r.COLUMN_NAME || '"" = :val ' ||
                                   'AND ROWNUM <= :top'
                  USING r.OWNER, r.TABLE_NAME, r.COLUMN_NAME, valueToFind, topPerTable;
                  
                  DBMS_SQL.RETURN_RESULT(l_cursor);
                END IF;
              END LOOP;
            END;";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new OracleCommand(query, connection);
        //command.CommandTimeout = _commandTimeout;
        command.Parameters.Add(":val", valueToFind);
        command.Parameters.Add(":top", topPerTable);
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
}
