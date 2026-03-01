using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class ConexionesPage : UserControl
{
    public ConexionesPage()
    {
        InitializeComponent();
        DataContext = new ConexionesViewModel();
    }
}
