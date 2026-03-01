using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using TonysDbTools.Models;
using TonysDbTools.Services;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarNumeroViewModel : ViewModelBase
{
    [ObservableProperty] private string _titulo = "Buscar número";
    [ObservableProperty] private string _descripcion = "Busque valores numéricos en los datos de sus tablas.";

    public ObservableCollection<Conexion> Conexiones => SessionService.Instance.Conexiones;
    public Conexion? ConexionSeleccionada
    {
        get => SessionService.Instance.SelectedConexion;
        set => SessionService.Instance.SelectedConexion = value;
    }

    [ObservableProperty] private decimal? _valorBuscar;
    [ObservableProperty] private int _topValoresPorTabla = 5;

    [ObservableProperty] private ObservableCollection<DbSearchResult> _resultados = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _resultCountMessage = string.Empty;
    [ObservableProperty] private bool _isSearching;

    public BuscarNumeroViewModel()
    {
        SessionService.Instance.PropertyChanged += OnSessionServicePropertyChanged;
    }

    private void OnSessionServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionService.SelectedConexion))
        {
            OnPropertyChanged(nameof(ConexionSeleccionada));
            BuscarCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(SessionService.Conexiones))
        {
            OnPropertyChanged(nameof(Conexiones));
        }
    }

    [RelayCommand(CanExecute = nameof(CanBuscar))]
    private async Task Buscar()
    {
        if (ConexionSeleccionada == null || ValorBuscar == null) return;

        IsSearching = true;
        StatusMessage = "Buscando valor numérico en todas las tablas...";
        Resultados.Clear();
        ResultCountMessage = string.Empty;

        try
        {
            var connStr = ConexionSeleccionada.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string query = @"
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
DEALLOCATE table_cursor;";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@param_find", ValorBuscar.Value);
            cmd.Parameters.AddWithValue("@param_top", TopValoresPorTabla);

            cmd.CommandTimeout = 300; 

            using var reader = await cmd.ExecuteReaderAsync();
            do
            {
                while (await reader.ReadAsync())
                {
                    Resultados.Add(new DbSearchResult
                    {
                        Tabla = reader["Tabla"].ToString() ?? "",
                        Columna = reader["Columna"].ToString() ?? "",
                        Valor = reader["Valor"].ToString() ?? ""
                    });
                }
            } while (await reader.NextResultAsync());

            if (Resultados.Count == 0)
            {
                StatusMessage = "Sin resultados encontrados.";
            }
            else
            {
                StatusMessage = "Búsqueda finalizada.";
                ResultCountMessage = $"Se encontraron {Resultados.Count} resultados.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private bool CanBuscar() => !IsSearching && ConexionSeleccionada != null && ValorBuscar != null;

    [RelayCommand]
    private void Limpiar()
    {
        ValorBuscar = null;
        Resultados.Clear();
        StatusMessage = string.Empty;
        ResultCountMessage = string.Empty;
    }

    partial void OnValorBuscarChanged(decimal? value) => BuscarCommand.NotifyCanExecuteChanged();
    partial void OnIsSearchingChanged(bool value) => BuscarCommand.NotifyCanExecuteChanged();
}
