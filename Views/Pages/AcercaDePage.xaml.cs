using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class AcercaDePage : UserControl
{
    public AcercaDePage()
    {
        InitializeComponent();
        DataContext = new AcercaDeViewModel();
    }
}