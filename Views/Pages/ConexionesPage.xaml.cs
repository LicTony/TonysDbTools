using System.Windows;
using System.Windows.Controls;
using TonysDbTools.ViewModels.Pages;

namespace TonysDbTools.Views.Pages;

public partial class ConexionesPage : UserControl
{
    public ConexionesPage()
    {
        InitializeComponent();
        var viewModel = new ConexionesViewModel();
        DataContext = viewModel;

        // Suscribirse a cambios en el ViewModel para actualizar el PasswordBox cuando se edita
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConexionesViewModel.Password))
            {
                if (PassBox.Password != viewModel.Password)
                {
                    PassBox.Password = viewModel.Password;
                }
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConexionesViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }
}