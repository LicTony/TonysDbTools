using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TonysDbTools.Models;
using TonysDbTools.Services;

namespace TonysDbTools.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    public ObservableCollection<Conexion> Conexiones => SessionService.Instance.Conexiones;
    public Conexion? ConexionSeleccionada
    {
        get => SessionService.Instance.SelectedConexion;
        set => SessionService.Instance.SelectedConexion = value;
    }

    private readonly List<MenuItemInfo> _menuItems = new()
    {
        new("Conexiones", "Conexiones"),
        new("Rel. 2 tablas", "Rel2Tablas"),
        new("Rel. 3 tablas", "Rel3Tablas"),
        new("Buscar en SPs", "BuscarSPs"),
        new("Buscar texto", "BuscarTexto"),
        new("Buscar número", "BuscarNumero"),
        new("Acerca de", "AcercaDe")
    };

    public MainViewModel()
    {
        SessionService.Instance.PropertyChanged += OnSessionServicePropertyChanged;
    }

    private void OnSessionServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionService.SelectedConexion))
        {
            OnPropertyChanged(nameof(ConexionSeleccionada));
        }
        else if (e.PropertyName == nameof(SessionService.Conexiones))
        {
            OnPropertyChanged(nameof(Conexiones));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchSuggestions.Clear();
        if (string.IsNullOrWhiteSpace(value)) return;

        var filtered = _menuItems
            .Where(m => m.Name.Contains(value, System.StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Name);

        foreach (var item in filtered)
            SearchSuggestions.Add(item);
    }

    public string? GetTagFromName(string name)
    {
        return _menuItems.FirstOrDefault(m => m.Name == name)?.Tag;
    }

    private record MenuItemInfo(string Name, string Tag);
}