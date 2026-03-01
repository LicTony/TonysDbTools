using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TonysDbTools.Models;

namespace TonysDbTools.Services;

public partial class SessionService : ObservableObject
{
    private static SessionService? _instance;
    public static SessionService Instance => _instance ??= new SessionService();

    private readonly ConexionService _conexionService = new();

    [ObservableProperty]
    private ObservableCollection<Conexion> _conexiones = new();

    [ObservableProperty]
    private Conexion? _selectedConexion;

    private SessionService()
    {
        _ = InitialLoadAsync();
    }

    public async Task InitialLoadAsync()
    {
        var list = await _conexionService.GetAllAsync();
        Conexiones = new ObservableCollection<Conexion>(list);
        
        if (SelectedConexion == null && Conexiones.Any())
        {
            SelectedConexion = Conexiones.First();
        }
        else if (SelectedConexion != null)
        {
            // Update the selected connection reference to the one in the new list if it exists
            var updated = Conexiones.FirstOrDefault(c => c.Id == SelectedConexion.Id);
            if (updated != null)
            {
                SelectedConexion = updated;
            }
            else if (Conexiones.Any())
            {
                SelectedConexion = Conexiones.First();
            }
            else
            {
                SelectedConexion = null;
            }
        }
    }

    public async Task RefreshConexionesAsync()
    {
        var currentSelectedId = SelectedConexion?.Id;
        var list = await _conexionService.GetAllAsync();
        Conexiones = new ObservableCollection<Conexion>(list);
        
        if (currentSelectedId.HasValue)
        {
            SelectedConexion = Conexiones.FirstOrDefault(c => c.Id == currentSelectedId.Value) ?? Conexiones.FirstOrDefault();
        }
        else
        {
            SelectedConexion = Conexiones.FirstOrDefault();
        }
    }
}
