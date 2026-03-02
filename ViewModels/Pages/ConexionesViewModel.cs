using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TonysDbTools.Models;
using TonysDbTools.Services;

namespace TonysDbTools.ViewModels.Pages;

public partial class ConexionesViewModel : ViewModelBase
{
    private readonly ConexionService _service = new();

    [ObservableProperty]
    private string _titulo = "Conexiones";

    [ObservableProperty]
    private string _descripcion = "Administre las conexiones a sus bases de datos SQL Server.";

    public ObservableCollection<Conexion> Conexiones => SessionService.Instance.Conexiones;
    public Conexion? ConexionSeleccionada
    {
        get => SessionService.Instance.SelectedConexion;
        set => SessionService.Instance.SelectedConexion = value;
    }

    // Campos del formulario
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _detalle = string.Empty;
    [ObservableProperty] private DbProvider _providerSeleccionado = DbProvider.Mssql;
    [ObservableProperty] private TipoConexion _tipoSeleccionado = TipoConexion.UserPass;
    [ObservableProperty] private string _server = string.Empty;
    [ObservableProperty] private string _baseDeDatos = string.Empty;
    [ObservableProperty] private string _usuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _connectionString = string.Empty;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public Array TiposConexion => Enum.GetValues(typeof(TipoConexion));
    public Array Providers => Enum.GetValues(typeof(DbProvider));

    public ConexionesViewModel()
    {
        SessionService.Instance.PropertyChanged += OnSessionServicePropertyChanged;
    }

    private void OnSessionServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionService.SelectedConexion))
        {
            OnPropertyChanged(nameof(ConexionSeleccionada));
        }
        else if (e.PropertyName == nameof(SessionService.Conexiones))
        {
            OnPropertyChanged(nameof(Conexiones));
        }
    }

    [RelayCommand]
    private async Task CargarConexiones()
    {
        await SessionService.Instance.RefreshConexionesAsync();
    }

    [RelayCommand]
    private async Task GuardarConexion()
    {
        if (string.IsNullOrWhiteSpace(Detalle)) return;

        Conexion conexion = CrearConexionDesdeCampos();
        conexion.Id = Id;
        conexion.Detalle = Detalle;

        if (IsEditing)
            await _service.UpdateAsync(conexion);
        else
            await _service.AddAsync(conexion);

        LimpiarFormulario();
        await SessionService.Instance.RefreshConexionesAsync();
        StatusMessage = "Conexión guardada correctamente.";
    }

    [RelayCommand]
    private async Task ProbarConexion()
    {
        StatusMessage = "Probando conexión...";
        try
        {
            var temp = CrearConexionDesdeCampos();
            var provider = MetadataProviderFactory.Create(temp);
            await provider.TestConnectionAsync();
            
            StatusMessage = "¡Conexión exitosa!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private Conexion CrearConexionDesdeCampos()
    {
        Conexion conexion = TipoSeleccionado switch
        {
            TipoConexion.UserPass => new ConexionUserPass { Server = Server, BaseDeDatos = BaseDeDatos, Usuario = Usuario, Password = Password },
            TipoConexion.IntegratedSecurity => new ConexionIntegratedSecurity { Server = Server, BaseDeDatos = BaseDeDatos },
            TipoConexion.ConnectionString => new ConexionConnectionString { ConnectionString = ConnectionString },
            _ => throw new ArgumentOutOfRangeException()
        };
        conexion.Provider = ProviderSeleccionado;
        return conexion;
    }

    [RelayCommand]
    private async Task EliminarConexion(Conexion? conexion)
    {
        if (conexion == null) return;
        await _service.DeleteAsync(conexion.Id);
        await SessionService.Instance.RefreshConexionesAsync();
        StatusMessage = "Conexión eliminada.";
    }

    [RelayCommand]
    private void EditarConexion(Conexion? conexion)
    {
        if (conexion == null) return;

        IsEditing = true;
        Id = conexion.Id;
        Detalle = conexion.Detalle;
        TipoSeleccionado = conexion.Tipo;
        ProviderSeleccionado = conexion.Provider;

        if (conexion is ConexionUserPass up)
        {
            Server = up.Server;
            BaseDeDatos = up.BaseDeDatos;
            Usuario = up.Usuario;
            Password = up.Password;
        }
        else if (conexion is ConexionIntegratedSecurity isec)
        {
            Server = isec.Server;
            BaseDeDatos = isec.BaseDeDatos;
        }
        else if (conexion is ConexionConnectionString cs)
        {
            ConnectionString = cs.ConnectionString;
        }
        StatusMessage = $"Editando: {Detalle}";
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        IsEditing = false;
        Id = 0;
        Detalle = string.Empty;
        Server = string.Empty;
        BaseDeDatos = string.Empty;
        Usuario = string.Empty;
        Password = string.Empty;
        ConnectionString = string.Empty;
        TipoSeleccionado = TipoConexion.UserPass;
        ProviderSeleccionado = DbProvider.Mssql;
        StatusMessage = string.Empty;
    }
}
