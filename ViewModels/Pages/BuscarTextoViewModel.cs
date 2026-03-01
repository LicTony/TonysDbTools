using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarTextoViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Buscar texto";

    [ObservableProperty]
    private string _descripcion = "Busque cadenas de texto en los datos de sus tablas.";
}
