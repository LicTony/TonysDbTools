using System.Windows;
using System.Windows.Controls;
using TonysDbTools.ViewModels;

namespace TonysDbTools.Views;

public partial class DetalleSpView : Window
{
    public DetalleSpView(DetalleSpViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestScrollToText += (index, length) =>
        {
            SqlEditor.Focus();
            SqlEditor.Select(index, length);
            
            // Hacer scroll hasta la línea
            int lineIndex = SqlEditor.GetLineIndexFromCharacterIndex(index);
            SqlEditor.ScrollToLine(lineIndex);
        };

        // Manejar el cambio de visibilidad del panel de búsqueda para dar foco al TextBox
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DetalleSpViewModel.IsSearchOpen) && viewModel.IsSearchOpen)
            {
                // Un pequeño delay para esperar a que el panel sea visible
                Dispatcher.BeginInvoke(new System.Action(() => {
                    TxtSearch.Focus();
                    TxtSearch.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        };
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
