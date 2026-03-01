using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;
using UI = iNKORE.UI.WPF.Modern.Controls;

namespace TonysDbTools.Views.Pages;

public partial class Rel2TablasPage : UserControl
{
    public Rel2TablasPage()
    {
        InitializeComponent();
        DataContext = new Rel2TablasViewModel();
    }

    private void AutoSuggestBox1_TextChanged(UI.AutoSuggestBox sender, UI.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == UI.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (DataContext is Rel2TablasViewModel vm)
            {
                vm.ActualizarSugerencias1Command.Execute(sender.Text);
            }
        }
    }

    private void AutoSuggestBox2_TextChanged(UI.AutoSuggestBox sender, UI.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == UI.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (DataContext is Rel2TablasViewModel vm)
            {
                vm.ActualizarSugerencias2Command.Execute(sender.Text);
            }
        }
    }
}
