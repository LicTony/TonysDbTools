using CommunityToolkit.Mvvm.ComponentModel;

namespace TonysDbTools.ViewModels.Pages;

public partial class AcercaDeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _titulo = "Acerca de";

    [ObservableProperty]
    private string _version = "v1.0.0";

    [ObservableProperty]
    private string _descripcion = "Tony's DB Tools — Herramienta de apoyo para testing de bases de datos.\n.NET 10.0 · WPF · MVVM · iNKORE.UI.WPF.Modern";
}
