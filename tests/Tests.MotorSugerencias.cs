using Xunit;
using ProyectoFinalProgramacionParalela;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class Tests_MotorSugerencias
{
    private string CrearCarpetaConTxt(params string[] contenidos)
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);

        int i = 0;
        foreach (var texto in contenidos)
        {
            File.WriteAllText(Path.Combine(dir, $"archivo{i++}.txt"), texto);
        }

        return dir;
    }

    [Fact]
    public void CargarDiccionario_()
    {
        // prueba para ver si CargarDiccionario lee las palabras
        var motor = MotorSugerenciasSingleton.MotorSugerencias;
        string dir = CrearCarpetaConTxt("hola mundo hola", "programacion paralela");

        motor.CargarDiccionarioDesdeTXT(dir);

        var sugerencia = motor.BuscarCoincidencia("pro");
        Assert.Equal("programacion", sugerencia);
    }

    [Fact]
    public void ObtenerUltimaPalabra_1()
    {
        //prueba para ver si ObtenerUltimaPalabra devuelve la ultima palabra
        var motor = MotorSugerenciasSingleton.MotorSugerencias;

        string ultima = motor.ObtenerUltimaPalabra("hola mundo paralelo");

        Assert.Equal("paralelo", ultima);
    }

    [Fact]
    public void ObtenerResto_1()
    {
        // prueba para ver si ObtenerResto funciona correctamente
        var motor = MotorSugerenciasSingleton.MotorSugerencias;

        string resto = motor.ObtenerResto("hola mundo paralelo");

        Assert.Equal("hola mundo ", resto);
    }

    [Fact]
    public void BuscarCoincidencia_1()
    {
        // prueba para ver si BuscarCoincidencia encuentra la palabra correcta
        var motor = MotorSugerenciasSingleton.MotorSugerencias;
        string dir = CrearCarpetaConTxt("carro roca razcar correr");

        motor.CargarDiccionarioDesdeTXT(dir);

        string? match = motor.BuscarCoincidencia("car");

        Assert.Equal("carro", match);
    }

    [Fact]
    public async Task BuscarCoincidenciaAsync_1()
    {
        // prueba para ver si BuscarCoincidenciaAsync funciona
        var motor = MotorSugerenciasSingleton.MotorSugerencias;
        string dir = CrearCarpetaConTxt("perro pera perico");

        motor.CargarDiccionarioDesdeTXT(dir);

        string? match = await motor.BuscarCoincidenciaAsync("pe");

        Assert.StartsWith("pe", match);
    }
}
