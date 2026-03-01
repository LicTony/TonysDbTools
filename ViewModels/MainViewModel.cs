using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

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