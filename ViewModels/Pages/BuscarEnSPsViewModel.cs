using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using TonysDbTools.Models;
using TonysDbTools.Views;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarEnSPsViewModel : ViewModelBase
{
    private readonly ConexionService _conexionService = new();

    [ObservableProperty] private string _titulo = "Buscar en SPs";
    [ObservableProperty] private string _descripcion = "Busca texto dentro del código de Stored Procedures y Vistas.";
    
    [ObservableProperty] private ObservableCollection<Conexion> _conexiones = new();
    [ObservableProperty] private Conexion? _conexionSeleccionada;
    
    [ObservableProperty] private string _filtroSp = string.Empty;
    [ObservableProperty] private string _textoBuscar = string.Empty;
    
    [ObservableProperty] private ObservableCollection<SpSearchResult> _resultados = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _resultCountMessage = string.Empty;
    [ObservableProperty] private bool _isSearching;

    public BuscarEnSPsViewModel()
    {
        CargarConexionesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task CargarConexiones()
    {
        var list = await _conexionService.GetAllAsync();
        Conexiones = new ObservableCollection<Conexion>(list);
        if (Conexiones.Any())
            ConexionSeleccionada = Conexiones.First();
    }

    [RelayCommand(CanExecute = nameof(CanBuscar))]
    private async Task Buscar()
    {
        if (ConexionSeleccionada == null || string.IsNullOrWhiteSpace(TextoBuscar)) return;

        IsSearching = true;
        StatusMessage = "Buscando en la base de datos...";
        Resultados.Clear();
        ResultCountMessage = string.Empty;

        try
        {
            var connStr = ConexionSeleccionada.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string query = @"
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

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@param_sp", FiltroSp ?? "");
            cmd.Parameters.AddWithValue("@param_find", TextoBuscar);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Resultados.Add(new SpSearchResult
                {
                    Store = reader["Store"].ToString() ?? "",
                    CantOcurrencias = Convert.ToInt32(reader["CantOcurrencias"])
                });
            }

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

    private bool CanBuscar() => !IsSearching && ConexionSeleccionada != null && !string.IsNullOrWhiteSpace(TextoBuscar);

    [RelayCommand]
    private void Limpiar()
    {
        FiltroSp = string.Empty;
        TextoBuscar = string.Empty;
        Resultados.Clear();
        StatusMessage = string.Empty;
        ResultCountMessage = string.Empty;
    }

    [RelayCommand]
    private void VerDetalleSp(SpSearchResult? item)
    {
        if (item == null || ConexionSeleccionada == null) return;

        // Extraer el nombre del SP del comando sp_helptext 'schema.name'
        // El formato es: sp_helptext 'schema.name'
        string spName = item.Store.Replace("sp_helptext '", "").Replace("'", "");

        var viewModel = new DetalleSpViewModel(spName, item.Store, ConexionSeleccionada.GetConnectionString(), TextoBuscar);
        var view = new DetalleSpView(viewModel);
        
        // Intentar poner la ventana principal como dueña
        if (Application.Current.MainWindow != null)
        {
            view.Owner = Application.Current.MainWindow;
        }

        view.Show();
        StatusMessage = $"Abierto detalle de: {spName}";
    }

    partial void OnTextoBuscarChanged(string value) => BuscarCommand.NotifyCanExecuteChanged();
    partial void OnConexionSeleccionadaChanged(Conexion? value) => BuscarCommand.NotifyCanExecuteChanged();
    partial void OnIsSearchingChanged(bool value) => BuscarCommand.NotifyCanExecuteChanged();
}
