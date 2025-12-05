using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProyectoFinalProgramacionParalela
{
    public class MotorBusquedaSingleton
    {
        private static MotorBusquedaSingleton instance = null;
        private static readonly object _lock = new object();

        private static Logs logsBusqueda = new Logs("busqueda-logs", nivel: LogsNivel.WARN);

        MotorBusquedaSingleton()
        {
        }

        //Implementacion de Directory.EnumerateFiles que atrapa errores
        //OJO: Esto no es una funcion que mete a la memoria todos los archivos, 
        // sino un iterador que trabaja por partes.
        //TODO: Tomar en cuenta los puntajes de cada archivo de alguna manera...
        public static IEnumerable<string> EnumerarArchivos(string directorioActual, string patron, SearchOption opcionesBusqueda)
        {
            //Enumeramos el directorio actual, pero agarramos errores para que no explote todo
            IEnumerable<string>? archivos = null;
            try
            {
                archivos = Directory.EnumerateFiles(directorioActual, patron, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                logsBusqueda.WriteLine($"No se pudo acceder al directorio {directorioActual} durante una busqueda!", LogsNivel.ERROR);
            }
            catch (Exception ex)
            {
                logsBusqueda.WriteLine($"No se pudo acceder al directorio {directorioActual} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
            }

            //Si fallamos con enumerar los archivos, nos retiramos y el iterador termina aqui
            if (archivos == null) yield break;

            foreach (var archivo in archivos)
            {
                yield return archivo;
            }

            //Si realmente queremos rebuscar en todo entonces a enumerar los demas directorios
            if (opcionesBusqueda == SearchOption.AllDirectories)
            {
                IEnumerable<string>? subdirectorios = null;
                //Por si acaso, uno nunca sabe...
                try
                {
                    subdirectorios = Directory.EnumerateDirectories(directorioActual);
                }
                catch (UnauthorizedAccessException)
                {
                    logsBusqueda.WriteLine($"No se pudo leer los subdirectorios de {directorioActual} durante una busqueda!", LogsNivel.ERROR);
                }
                catch (Exception ex)
                {
                    logsBusqueda.WriteLine($"No se pudo leer los subdirectorios de {directorioActual} durante una busqueda por el siguiente error: {ex.Message}", LogsNivel.ERROR);
                }

                if (subdirectorios == null) yield break;

                foreach (var subdirectorio in subdirectorios)
                {
                    foreach (var archivo in EnumerarArchivos(subdirectorio, patron, opcionesBusqueda))
                    {
                        yield return archivo;
                    }
                }
            }
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
        
        public List<string> Buscar(string texto)
        {
            var conf = ConfiguracionSingleton.Configuracion;
            //Enumeramos toooodos los archivos (incluyendo los que estan adentro de los directorios) de nuestro directorio de trabajo
            var archivos = EnumerarArchivos(conf.GetDirectorio(), "*.txt", SearchOption.AllDirectories);
            //Las concurrentbags sirven para recolectar datos de una forma thread-safe (osea, sin condiciones de carrera)
            var resultadosBag = new ConcurrentBag<string>();
            Parallel.ForEach(archivos, conf.GetOpcionesParalelas(),
            (archivoPath) =>
            {
                try
                {
                    string textoArchivo = File.ReadAllText(archivoPath);
                    if (textoArchivo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    {
                        resultadosBag.Add(archivoPath);
                    }
                }
                catch (Exception ex)
                {
                    logsBusqueda.WriteLine($"No se pudo acceder al archivo {archivoPath} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
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
                        instance = new MotorBusquedaSingleton();
                    }
                    return instance;
                }
            }
        }
    }
}