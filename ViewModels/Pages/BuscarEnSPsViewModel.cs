using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TonysDbTools.Models;
using TonysDbTools.Services;
using TonysDbTools.Views;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarEnSPsViewModel : ViewModelBase
{
    [ObservableProperty] private string _titulo = "Buscar en SPs";
    [ObservableProperty] private string _descripcion = "Busca texto dentro del código de Stored Procedures y Vistas.";
    
    public ObservableCollection<Conexion> Conexiones => SessionService.Instance.Conexiones;
    public Conexion? ConexionSeleccionada
    {
        get => SessionService.Instance.SelectedConexion;
        set => SessionService.Instance.SelectedConexion = value;
    }
    
    [ObservableProperty] private string _filtroSp = string.Empty;
    [ObservableProperty] private string _textoBuscar = string.Empty;
    
    [ObservableProperty] private ObservableCollection<SpSearchResult> _resultados = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _resultCountMessage = string.Empty;
    [ObservableProperty] private bool _isSearching;

    public BuscarEnSPsViewModel()
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
        if (ConexionSeleccionada == null || string.IsNullOrWhiteSpace(TextoBuscar)) return;

        IsSearching = true;
        StatusMessage = "Buscando en la base de datos...";
        Resultados.Clear();
        ResultCountMessage = string.Empty;

        try
        {
            var provider = MetadataProviderFactory.Create(ConexionSeleccionada);
            var results = await provider.SearchInSpsAsync(FiltroSp, TextoBuscar);

            foreach (var res in results)
            {
                Resultados.Add(res);
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

        var provider = MetadataProviderFactory.Create(ConexionSeleccionada);
        var viewModel = new DetalleSpViewModel(spName, provider, TextoBuscar);
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
    partial void OnIsSearchingChanged(bool value) => BuscarCommand.NotifyCanExecuteChanged();
}
