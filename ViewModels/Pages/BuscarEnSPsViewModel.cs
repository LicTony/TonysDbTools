using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class BuscarEnSPsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Buscar en SPs";

    [ObservableProperty]
    private string _descripcion = "Busque texto dentro de los Stored Procedures de su base de datos.";
}
