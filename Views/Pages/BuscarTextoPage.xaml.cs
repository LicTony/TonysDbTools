using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class BuscarTextoPage : UserControl
{
    public BuscarTextoPage()
    {
        InitializeComponent();
        DataContext = new BuscarTextoViewModel();
    }
}