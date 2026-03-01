using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarNumeroViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Buscar número";

    [ObservableProperty]
    private string _descripcion = "Busque valores numéricos en los datos de sus tablas.";
}
