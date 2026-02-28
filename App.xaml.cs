using System;
using TonysDbTools.ViewModels;
using TonysDbTools.Views;

namespace TonysDbTools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
        
    /// <summary>
    /// Application Entry for TonysDbTools
    /// </summary>
    public App()
    {
        var view = new MainView
        {
            DataContext = Activator.CreateInstance<MainViewModel>()
        };
            
        view.Show();
    }
        
}