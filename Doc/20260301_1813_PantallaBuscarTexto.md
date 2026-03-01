
implementar la pantalla Buscar texto
Utilizando el siguiente query


DECLARE @SearchValue NVARCHAR(100) = 'SolarNexo Civico Nro. 1'; -- El valor que deseas buscar
DECLARE @BusquedaExacta INT = 2; -- 1 = Busqueda por el valor exacto , 2 = Busqueda por like
DECLARE @TopValesaMostrarPorTabla INT = 5; -- cantidad de registros a mostrar por cada tabla


set nocount on

if @BusquedaExacta =1
begin
	print 'Busqueda por texto exacto'
	print ''
end
else
begin
	print 'Busqueda por texto con like'
	print ''
end

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
WHERE 
    (@BusquedaExacta = 1 AND ty.name IN ('char', 'nchar', 'varchar', 'nvarchar'))
    OR
    (@BusquedaExacta = 2 AND ty.name IN ('char', 'nchar', 'varchar', 'nvarchar', 'text', 'ntext'))

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;

WHILE @@FETCH_STATUS = 0
BEGIN

    if @BusquedaExacta = 1	
		SET @SQL = 'IF EXISTS (SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + '] WHERE [' + @ColumnName + '] = ''' + @SearchValue + ''') ' +
               'BEGIN ' +
			   'PRINT '' ''; ' +
               'PRINT ''--Tabla: ' + @SchemaName + '.' + @TableName + ', Columna: ' + @ColumnName + '''; ' +
			   'PRINT ''SELECT ' + @ColumnName + ',* from ' + @SchemaName + '.' + @TableName + ' WHERE ' + @ColumnName + ' = '''''+ @SearchValue +'''''''; ' +
			   
               'EXEC(''SELECT TOP ' + ltrim(@TopValesaMostrarPorTabla)+ ' ''''Tabla: ' + @SchemaName + '.' + @TableName + ''''' AS Tabla, ''''Columna: ' + @ColumnName + ''''' AS Columna, [' + @ColumnName + '] AS Valor ' +
               'FROM [' + @SchemaName + '].[' + @TableName + '] ' +
               'WHERE [' + @ColumnName + '] = ''''' + @SearchValue + ''''' ' +
               'ORDER BY [' + @ColumnName + ']''); ' +
               'END;';
	else
		SET @SQL = 'IF EXISTS (SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + '] WHERE [' + @ColumnName + '] LIKE ''%' + @SearchValue + '%'') ' +
               'BEGIN ' +
			   'PRINT '' ''; ' +
               'PRINT ''--Tabla: ' + @SchemaName + '.' + @TableName + ', Columna: ' + @ColumnName + '''; ' +
			   'PRINT ''SELECT ' + @ColumnName + ',* from ' + @SchemaName + '.' + @TableName + ' WHERE ' + @ColumnName + ' like ''''%'+ @SearchValue +'%''''''; ' +
               'EXEC(''SELECT TOP ' + ltrim(@TopValesaMostrarPorTabla)+ ' ''''Tabla: ' + @SchemaName + '.' + @TableName + ''''' AS Tabla, ''''Columna: ' + @ColumnName + ''''' AS Columna, [' + @ColumnName + '] AS Valor ' +
               'FROM [' + @SchemaName + '].[' + @TableName + '] ' +
               'WHERE [' + @ColumnName + '] LIKE ''''%' + @SearchValue + '%'''' ' +
               'ORDER BY [' + @ColumnName + ']''); ' +
               'END;';
	
    
    -- Ejecutar el SQL dinámico
    EXEC sp_executesql @SQL;

    FETCH NEXT FROM table_cursor INTO @SchemaName, @TableName, @ColumnName;
END;

CLOSE table_cursor;
DEALLOCATE table_cursor;