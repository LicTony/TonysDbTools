using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class Rel3TablasPage : UserControl
{
    public Rel3TablasPage()
    {
        InitializeComponent();
        DataContext = new Rel3TablasViewModel();
    }
}