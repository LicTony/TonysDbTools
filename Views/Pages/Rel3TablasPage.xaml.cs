using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;
using UI = iNKORE.UI.WPF.Modern.Controls;

namespace TonysDbTools.Views.Pages;

public partial class Rel3TablasPage : UserControl
{
    public Rel3TablasPage()
    {
        InitializeComponent();
        DataContext = new Rel3TablasViewModel();
    }

    private void AutoSuggestBox1_TextChanged(UI.AutoSuggestBox sender, UI.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == UI.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (DataContext is Rel3TablasViewModel vm)
            {
                vm.ActualizarSugerencias1Command.Execute(sender.Text);
            }
        }
    }

    private void AutoSuggestBox2_TextChanged(UI.AutoSuggestBox sender, UI.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == UI.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (DataContext is Rel3TablasViewModel vm)
            {
                vm.ActualizarSugerencias2Command.Execute(sender.Text);
            }
        }
    }

    private void AutoSuggestBox3_TextChanged(UI.AutoSuggestBox sender, UI.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == UI.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (DataContext is Rel3TablasViewModel vm)
            {
                vm.ActualizarSugerencias3Command.Execute(sender.Text);
            }
        }
    }
}
