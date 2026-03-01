using System.Linq;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using TonysDbTools.ViewModels;
using TonysDbTools.Views.Pages;

namespace TonysDbTools.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
        // Seleccionar el primer ítem por defecto al cargar
        Loaded += (_, _) => NavigateToTag("Conexiones");
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            NavigateToTag(item.Tag?.ToString());
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (DataContext is MainViewModel vm && args.SelectedItem is string name)
        {
            var tag = vm.GetTagFromName(name);
            NavigateToTag(tag);
            
            // Buscar el item en la NavigationView y seleccionarlo visualmente
            var menuItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == tag)
                        ?? NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == tag);
            
            if (menuItem != null)
                NavView.SelectedItem = menuItem;
        }
    }

    private void NavigateToTag(string? tag)
    {
        object? page = tag switch
        {
            "Conexiones"   => new ConexionesPage(),
            "Rel2Tablas"   => new Rel2TablasPage(),
            "Rel3Tablas"   => new Rel3TablasPage(),
            "BuscarSPs"    => new BuscarEnSPsPage(),
            "BuscarTexto"  => new BuscarTextoPage(),
            "BuscarNumero" => new BuscarNumeroPage(),
            "AcercaDe"     => new AcercaDePage(),
            _              => null
        };

        if (page is not null)
            ContentFrame.Navigate(page);
    }
}