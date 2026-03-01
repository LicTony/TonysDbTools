using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class Rel3TablasViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Rel. 3 tablas";

    [ObservableProperty]
    private string _descripcion = "Analyse relaciones entre tres tablas de base de datos.";
}
