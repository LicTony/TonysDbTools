using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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

    /// <summary>
    /// Al cargar el AutoSuggestBox, busca el Popup interno y ajusta el MinWidth
    /// de su contenido para que las sugerencias no se recorten.
    /// </summary>
    private void AutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is UI.AutoSuggestBox asb)
        {
            asb.ApplyTemplate();

            var popup = FindVisualChild<Popup>(asb);
            if (popup != null)
            {
                popup.MinWidth = UiConstants.AutoSuggestBoxPopupMinWidth;
                if (popup.Child is FrameworkElement popupChild)
                {
                    popupChild.MinWidth = UiConstants.AutoSuggestBoxPopupMinWidth;
                }
            }

            var listView = FindVisualChild<ListView>(asb);
            if (listView != null)
            {
                listView.MinWidth = UiConstants.AutoSuggestBoxPopupMinWidth;
            }

            var listBox = FindVisualChild<ListBox>(asb);
            if (listBox != null)
            {
                listBox.MinWidth = UiConstants.AutoSuggestBoxPopupMinWidth;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}

