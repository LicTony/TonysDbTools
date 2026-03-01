using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TonysDbTools.Models;

public class ConexionService
{
    private const string FileName = "conexiones.json";
    private readonly string _filePath;

    public ConexionService()
    {
        // Guardar en la carpeta local de la aplicación
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
    }

    public async Task<List<Conexion>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<Conexion>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<Conexion>>(json) ?? new List<Conexion>();
        }
        catch
        {
            return new List<Conexion>();
        }
    }

    public async Task SaveAllAsync(List<Conexion> conexiones)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(conexiones, options);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task AddAsync(Conexion conexion)
    {
        var list = await GetAllAsync();
        conexion.Id = list.Count > 0 ? list.Max(c => c.Id) + 1 : 1;
        list.Add(conexion);
        await SaveAllAsync(list);
    }

    public async Task UpdateAsync(Conexion conexion)
    {
        var list = await GetAllAsync();
        var index = list.FindIndex(c => c.Id == conexion.Id);
        if (index != -1)
        {
            list[index] = conexion;
            await SaveAllAsync(list);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var list = await GetAllAsync();
        var item = list.FirstOrDefault(c => c.Id == id);
        if (item != null)
        {
            list.Remove(item);
            await SaveAllAsync(list);
        }
    }
}
