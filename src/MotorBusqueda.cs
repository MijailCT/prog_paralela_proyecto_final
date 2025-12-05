using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProyectoFinalProgramacionParalela
{
    public class MotorBusquedaSingleton
    {
        private static MotorBusquedaSingleton instance = null;
        private static readonly object _lock = new object();

        private string directorioTrabajo;
        private ParallelOptions opcionesParalelas;

        MotorBusquedaSingleton(string directorio, ParallelOptions opt)
        {
            directorioTrabajo = directorio;
            opcionesParalelas = opt;
        }

        public static void AbrirArchivo(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", path);
            }
        }
        
        public async Task<List<string>> Buscar(string texto)
        {
            //Enumeramos toooodos los archivos (incluyendo los que estan adentro de los directorios) de nuestro directorio de trabajo
            //TODO: reemplazarlo con una funcion que enumere los archivos y despues los organize segun los puntajes
            var archivos = Directory.EnumerateFiles(directorioTrabajo, "*.txt", SearchOption.AllDirectories);
            //Las concurrentbags sirven para recolectar datos de una forma thread-safe (osea, sin condiciones de carrera)
            var resultadosBag = new ConcurrentBag<string>();
            Parallel.ForEach(archivos, opcionesParalelas,
            (archivoPath) =>
            {
                string textoArchivo = File.ReadAllText(archivoPath);
                if (textoArchivo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                {
                    resultadosBag.Add(archivoPath);
                    //DatosSingleton.Datos.IncrementarPuntaje(archivoPath); (un suponer)
                }
            });
            
            return resultadosBag.ToList();
        }
        
        public static MotorBusquedaSingleton MotorBusqueda
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        var conf = ConfiguracionSingleton.Configuracion;
                        instance = new MotorBusquedaSingleton(conf.GetDirectorio(), conf.GetOpcionesParalelas());
                    }
                    return instance;
                }
            }
        }
    }
}