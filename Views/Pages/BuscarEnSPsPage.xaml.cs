using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class BuscarEnSPsPage : UserControl
{
    public BuscarEnSPsPage()
    {
        InitializeComponent();
        DataContext = new BuscarEnSPsViewModel();
    }
}