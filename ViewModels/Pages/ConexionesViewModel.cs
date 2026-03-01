using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class ConexionesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Conexiones";

    [ObservableProperty]
    private string _descripcion = "Administre las conexiones a sus bases de datos SQL Server.";
}
