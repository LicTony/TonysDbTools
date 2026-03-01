using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class Rel2TablasPage : UserControl
{
    public Rel2TablasPage()
    {
        InitializeComponent();
        DataContext = new Rel2TablasViewModel();
    }
}