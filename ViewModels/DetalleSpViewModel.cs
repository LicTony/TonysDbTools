using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using TonysDbTools.Models;

namespace TonysDbTools.ViewModels;

public partial class DetalleSpViewModel : ViewModelBase
{
    [ObservableProperty] private string _titulo = "Detalle del Store";
    [ObservableProperty] private string _spName = string.Empty;
    [ObservableProperty] private string _contenidoSql = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    // Búsqueda
    [ObservableProperty] private string _textoBusquedaLocal = string.Empty;
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private string _searchStatus = string.Empty;

    private readonly string _connectionString;
    private readonly string _commandText;
    private int _lastSearchIndex = -1;

    public DetalleSpViewModel(string spName, string commandText, string connectionString, string initialSearchText)
    {
        SpName = spName;
        _commandText = commandText;
        _connectionString = connectionString;
        TextoBusquedaLocal = initialSearchText;
        Titulo = $"Código: {spName}";
        
        CargarContenidoCommand.Execute(null);
    }

    [RelayCommand]
    private async Task CargarContenido()
    {
        IsLoading = true;
        StatusMessage = "Cargando código...";
        var sb = new StringBuilder();

        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(_commandText, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sb.Append(reader[0].ToString());
            }

            ContenidoSql = sb.ToString();
            StatusMessage = "Código cargado correctamente.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar: {ex.Message}";
            ContenidoSql = "No se pudo obtener el código del store.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopiarTodo()
    {
        if (!string.IsNullOrEmpty(ContenidoSql))
        {
            System.Windows.Clipboard.SetText(ContenidoSql);
            StatusMessage = "Código copiado al portapapeles.";
        }
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        if (!IsSearchOpen)
        {
            SearchStatus = string.Empty;
            _lastSearchIndex = -1;
        }
    }

    public event Action<int, int>? RequestScrollToText;

    [RelayCommand]
    private void BuscarSiguiente()
    {
        if (string.IsNullOrEmpty(TextoBusquedaLocal) || string.IsNullOrEmpty(ContenidoSql)) return;

        int index = ContenidoSql.IndexOf(TextoBusquedaLocal, _lastSearchIndex + 1, StringComparison.OrdinalIgnoreCase);

        if (index == -1) // Si no encuentra más, volver a empezar desde el principio
        {
            index = ContenidoSql.IndexOf(TextoBusquedaLocal, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (index != -1)
        {
            _lastSearchIndex = index;
            SearchStatus = "Encontrado";
            RequestScrollToText?.Invoke(index, TextoBusquedaLocal.Length);
        }
        else
        {
            SearchStatus = "No encontrado";
        }
    }
}
