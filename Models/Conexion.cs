using System;
using System.Text.Json.Serialization;

namespace TonysDbTools.Models;

public enum TipoConexion
{
    UserPass,
    IntegratedSecurity,
    ConnectionString
}

public enum DbProvider
{
    Mssql,
    Oracle
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConexionUserPass), typeDiscriminator: "UserPass")]
[JsonDerivedType(typeof(ConexionIntegratedSecurity), typeDiscriminator: "IntegratedSecurity")]
[JsonDerivedType(typeof(ConexionConnectionString), typeDiscriminator: "ConnectionString")]
public abstract class Conexion
{
    public int Id { get; set; }
    public TipoConexion Tipo { get; set; }
    public DbProvider Provider { get; set; } = DbProvider.Mssql;
    public string Detalle { get; set; } = string.Empty;

    public abstract string GetConnectionString();
}

public class ConexionUserPass : Conexion
{
    public string Server { get; set; } = string.Empty;
    public string BaseDeDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ConexionUserPass() => Tipo = TipoConexion.UserPass;

    public override string GetConnectionString() => 
        $"Server={Server};Database={BaseDeDatos};User Id={Usuario};Password={Password};TrustServerCertificate=True;";
}

public class ConexionIntegratedSecurity : Conexion
{
    public string Server { get; set; } = string.Empty;
    public string BaseDeDatos { get; set; } = string.Empty;

    public ConexionIntegratedSecurity() => Tipo = TipoConexion.IntegratedSecurity;

    public override string GetConnectionString() => 
        $"Server={Server};Database={BaseDeDatos};Integrated Security=True;TrustServerCertificate=True;";
}

public class ConexionConnectionString : Conexion
{
    public string ConnectionString { get; set; } = string.Empty;

    public ConexionConnectionString() => Tipo = TipoConexion.ConnectionString;

    public override string GetConnectionString() => ConnectionString;
}
