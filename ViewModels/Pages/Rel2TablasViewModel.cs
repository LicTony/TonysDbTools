using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class Rel2TablasViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Rel. 2 tablas";

    [ObservableProperty]
    private string _descripcion = "Analyse relaciones entre dos tablas de base de datos.";
}
