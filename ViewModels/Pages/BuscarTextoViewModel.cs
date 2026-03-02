using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TonysDbTools.Models;
using TonysDbTools.Services;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarTextoViewModel : ViewModelBase
{
    [ObservableProperty] private string _titulo = "Buscar texto";
    [ObservableProperty] private string _descripcion = "Busque cadenas de texto en los datos de sus tablas.";

    public ObservableCollection<Conexion> Conexiones => SessionService.Instance.Conexiones;
    public Conexion? ConexionSeleccionada
    {
        get => SessionService.Instance.SelectedConexion;
        set => SessionService.Instance.SelectedConexion = value;
    }

    [ObservableProperty] private string _textoBuscar = string.Empty;
    [ObservableProperty] private bool _isBusquedaExacta = false;
    [ObservableProperty] private int _topValoresPorTabla = 2;

    [ObservableProperty] private ObservableCollection<DbSearchResult> _resultados = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _resultCountMessage = string.Empty;
    [ObservableProperty] private bool _isSearching;

    public BuscarTextoViewModel()
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
        StatusMessage = "Buscando texto en todas las tablas...";
        Resultados.Clear();
        ResultCountMessage = string.Empty;

        try
        {
            var provider = MetadataProviderFactory.Create(ConexionSeleccionada);
            var results = await provider.SearchTextInTablesAsync(TextoBuscar, IsBusquedaExacta, TopValoresPorTabla);

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
        TextoBuscar = string.Empty;
        Resultados.Clear();
        StatusMessage = string.Empty;
        ResultCountMessage = string.Empty;
    }

    partial void OnTextoBuscarChanged(string value) => BuscarCommand.NotifyCanExecuteChanged();
    partial void OnIsSearchingChanged(bool value) => BuscarCommand.NotifyCanExecuteChanged();
}
