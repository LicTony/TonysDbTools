using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class BuscarNumeroPage : UserControl
{
    public BuscarNumeroPage()
    {
        InitializeComponent();
        DataContext = new BuscarNumeroViewModel();
    }
}