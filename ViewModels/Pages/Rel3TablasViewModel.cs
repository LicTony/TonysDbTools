using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TonysDbTools.Models;
using TonysDbTools.Models.Join;
using TonysDbTools.Services;

namespace TonysDbTools.ViewModels.Pages;

public partial class Rel3TablasViewModel : ViewModelBase
{
    private readonly ConexionService _conexionService = new();
    private IJoinFinderService? _joinFinderService;

    [ObservableProperty] private string _titulo = "Rel. 3 tablas";
    [ObservableProperty] private string _descripcion = "Analyse relaciones entre tres tablas de base de datos.";

    [ObservableProperty] private ObservableCollection<Conexion> _conexiones = new();
    [ObservableProperty] private Conexion? _conexionSeleccionada;

    [ObservableProperty] private string _tabla1 = string.Empty;
    [ObservableProperty] private string _tabla2 = string.Empty;
    [ObservableProperty] private string _tabla3 = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _tablasDisponibles = new();
    [ObservableProperty] private ObservableCollection<string> _sugerenciasTabla1 = new();
    [ObservableProperty] private ObservableCollection<string> _sugerenciasTabla2 = new();
    [ObservableProperty] private ObservableCollection<string> _sugerenciasTabla3 = new();

    [ObservableProperty] private JoinResult? _resultado;
    [ObservableProperty] private int _maxDepth = 3; // Menor profundidad por defecto para 3 tablas por performance
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public Rel3TablasViewModel()
    {
        CargarConexionesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task CargarConexiones()
    {
        var list = await _conexionService.GetAllAsync();
        Conexiones = new ObservableCollection<Conexion>(list);
        if (Conexiones.Any())
            ConexionSeleccionada = Conexiones.First();
    }

    partial void OnConexionSeleccionadaChanged(Conexion? value)
    {
        _joinFinderService?.Dispose();
        _joinFinderService = null;
        TablasDisponibles.Clear();
        SugerenciasTabla1.Clear();
        SugerenciasTabla2.Clear();
        SugerenciasTabla3.Clear();
        Resultado = null;

        if (value != null)
        {
            _joinFinderService = new JoinFinderService(value.GetConnectionString());
            _ = CargarTablasAsync();
        }
        
        BuscarRelacionesCommand.NotifyCanExecuteChanged();
    }

    private async Task CargarTablasAsync()
    {
        if (_joinFinderService == null) return;

        try
        {
            StatusMessage = "Cargando esquema de tablas...";
            var tablas = await _joinFinderService.GetAllTablesWithRelationsAsync();
            TablasDisponibles = new ObservableCollection<string>(tablas);
            StatusMessage = $"Se cargaron {tablas.Count} tablas.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar tablas: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ActualizarSugerencias1(string query)
    {
        SugerenciasTabla1.Clear();
        if (string.IsNullOrWhiteSpace(query)) return;

        var filtered = TablasDisponibles
            .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(15);

        foreach (var t in filtered)
            SugerenciasTabla1.Add(t);
    }

    [RelayCommand]
    private void ActualizarSugerencias2(string query)
    {
        SugerenciasTabla2.Clear();
        if (string.IsNullOrWhiteSpace(query)) return;

        var filtered = TablasDisponibles
            .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(15);

        foreach (var t in filtered)
            SugerenciasTabla2.Add(t);
    }

    [RelayCommand]
    private void ActualizarSugerencias3(string query)
    {
        SugerenciasTabla3.Clear();
        if (string.IsNullOrWhiteSpace(query)) return;

        var filtered = TablasDisponibles
            .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(15);

        foreach (var t in filtered)
            SugerenciasTabla3.Add(t);
    }

    [RelayCommand(CanExecute = nameof(CanBuscar))]
    private async Task BuscarRelaciones()
    {
        if (_joinFinderService == null || string.IsNullOrWhiteSpace(Tabla1) || string.IsNullOrWhiteSpace(Tabla2) || string.IsNullOrWhiteSpace(Tabla3))
            return;

        IsSearching = true;
        StatusMessage = "Buscando caminos de JOIN entre las 3 tablas...";
        Resultado = null;

        try
        {
            var tableNames = new[] { Tabla1, Tabla2, Tabla3 };
            var result = await _joinFinderService.FindJoinPathsAsync(tableNames, MaxDepth);
            
            // Buscar JOINS implícitos (por nombre de columna)
            var implicitJoins = await _joinFinderService.FindImplicitJoinsAsync(tableNames);
            
            foreach (var rel in implicitJoins)
            {
                var path = new JoinPath
                {
                    Steps = new List<JoinStep>
                    {
                        new JoinStep
                        {
                            ConstraintName = rel.ConstraintName,
                            FromTable = rel.FromFullName,
                            FromColumn = rel.FromColumn,
                            ToTable = rel.ToFullName,
                            ToColumn = rel.ToColumn
                        }
                    }
                };
                
                if (!result.Paths.Any(p => p.Steps.Count == 1 && 
                    p.Steps[0].FromTable == path.Steps[0].FromTable && 
                    p.Steps[0].ToTable == path.Steps[0].ToTable &&
                    p.Steps[0].FromColumn == path.Steps[0].FromColumn &&
                    p.Steps[0].ToColumn == path.Steps[0].ToColumn))
                {
                    result.Paths.Add(path);
                }
            }

            Resultado = result;
            
            if (Resultado.CanJoin)
                StatusMessage = $"Se encontraron {Resultado.TotalPaths} combinaciones que conectan las tablas.";
            else if (Resultado.Errors.Any())
                StatusMessage = $"Error: {Resultado.Errors.First()}";
            else
                StatusMessage = "No se encontraron relaciones que conecten las 3 tablas simultáneamente.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private bool CanBuscar() => !IsSearching && ConexionSeleccionada != null && 
                                !string.IsNullOrWhiteSpace(Tabla1) && !string.IsNullOrWhiteSpace(Tabla2) && !string.IsNullOrWhiteSpace(Tabla3);

    partial void OnTabla1Changed(string value) => BuscarRelacionesCommand.NotifyCanExecuteChanged();
    partial void OnTabla2Changed(string value) => BuscarRelacionesCommand.NotifyCanExecuteChanged();
    partial void OnTabla3Changed(string value) => BuscarRelacionesCommand.NotifyCanExecuteChanged();
    partial void OnIsSearchingChanged(bool value) => BuscarRelacionesCommand.NotifyCanExecuteChanged();
}
