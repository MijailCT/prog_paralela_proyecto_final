using Xunit;
using ProyectoFinalProgramacionParalela;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class Tests_MotorBusqueda
{
    private string CrearCarpetaConArchivos(Dictionary<string, string> archivos)
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);

        foreach (var file in archivos)
        {
            File.WriteAllText(Path.Combine(dir, file.Key), file.Value);
        }

        return dir;
    }

    [Fact]
    public void EnumerarArchivos_1()
    {
        // prueba para ver si EnumerarArchivos encuentra los archivos correctamente
        string dir = CrearCarpetaConArchivos(new()
        {
            ["a.txt"] = "hola mundo",
            ["b.txt"] = "otro archivo"
        });

        var resultado = MotorBusquedaSingleton.EnumerarArchivos(
            dir, "*.txt", SearchOption.TopDirectoryOnly
        ).ToList();

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public void Buscar_1()
    {
        // prueba para ver si Buscar encuentra archivos que contienen texto
        string dir = CrearCarpetaConArchivos(new()
        {
            ["uno.txt"] = "hola mundo",
            ["dos.txt"] = "nada aqui"
        });

        ConfiguracionSingleton.Configuracion.SetDirectorio(dir);

        var motor = MotorBusquedaSingleton.MotorBusqueda;

        List<string> res = motor.Buscar("hola");

        Assert.Single(res);
        Assert.Contains("uno.txt", res[0]);
    }

    [Fact]
    public void Buscar_2()
    {
        //prueba para ver si Buscar no devuelve archivo sin coincidencia 
        string dir = CrearCarpetaConArchivos(new()
        {
            ["uno.txt"] = "hola mundo",
            ["dos.txt"] = "nada aquí"
        });

        ConfiguracionSingleton.Configuracion.SetDirectorio(dir);

        var motor = MotorBusquedaSingleton.MotorBusqueda;

        List<string> res = motor.Buscar("xyz");

        Assert.Empty(res);
    }
}
