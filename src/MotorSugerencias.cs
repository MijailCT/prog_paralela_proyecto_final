using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProyectoFinalProgramacionParalela
{
    public class MotorSugerenciasSingleton
    {
        private static MotorSugerenciasSingleton? instance = null;
        private static readonly object _lock = new object();

        private static Logs logsSugerencias = new Logs(LogsNivel.ERROR);

        private readonly HashSet<string> diccionario;
        private readonly Dictionary<string, List<string>> bigramas;

        private CancellationTokenSource cts;
        private readonly object lockObj = new object();

        
        private string autocompletadoReal = "";
        private string sugerenciaVisible = "";

        private MotorSugerenciasSingleton()
        {
            diccionario = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bigramas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            cts = new CancellationTokenSource();

            CargarDiccionarioYBigramas();
        }

        public static MotorSugerenciasSingleton MotorSugerencias
        {
            get
            {
                lock (_lock)
                {
                    instance ??= new MotorSugerenciasSingleton();
                    return instance;
                }
            }
        }

        //TODO: hacer una funcion EnumerarArchivos en general para no repetir codigo
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
                logsSugerencias.WriteLine($"No se pudo acceder al directorio {directorioActual}!", LogsNivel.ERROR);
            }
            catch (Exception ex)
            {
                logsSugerencias.WriteLine($"No se pudo acceder al directorio {directorioActual} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
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
                    logsSugerencias.WriteLine($"No se pudo leer los subdirectorios de {directorioActual}!", LogsNivel.ERROR);
                }
                catch (Exception ex)
                {
                    logsSugerencias.WriteLine($"No se pudo leer los subdirectorios de {directorioActual} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
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

        private void CargarDiccionarioYBigramas()
        {
            string dir = ConfiguracionSingleton.Configuracion.GetDirectorio();
            if (!Directory.Exists(dir)) return;

            var archivos = EnumerarArchivos(dir, "*.txt", SearchOption.AllDirectories);

            foreach (var archivo in archivos)
            {
                try
                {
                    var texto = File.ReadAllText(archivo);
                    var palabras = texto.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < palabras.Length; i++)
                    {
                        var actual = Normalizar(palabras[i]);
                        if (!string.IsNullOrWhiteSpace(actual))
                            diccionario.Add(actual);

                        if (i < palabras.Length - 1)
                        {
                            var siguiente = Normalizar(palabras[i + 1]);
                            if (string.IsNullOrWhiteSpace(siguiente)) continue;

                            if (!bigramas.ContainsKey(actual))
                                bigramas[actual] = new List<string>();

                            if (!bigramas[actual].Contains(siguiente))
                                bigramas[actual].Add(siguiente);
                        }
                    }
                }
                catch { }
            }
        }

        private string Normalizar(string p) => p.Trim().ToLower();

        
        public void ActualizarSugerencia(string input)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            lock (lockObj)
            {
                autocompletadoReal = "";
                sugerenciaVisible = "";
            }

            _ = Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(50); 
                    if (token.IsCancellationRequested) return;

                    if (string.IsNullOrEmpty(input)) return;

                    
                    bool terminaConEspacio = input.EndsWith(" ");
                    var partes = input.TrimEnd().Split(' ');
                    string ultima = partes.LastOrDefault() ?? "";
                    ultima = Normalizar(ultima);

                    
                    if (terminaConEspacio)
                    {
                        // buscar bigrama
                        if (bigramas.TryGetValue(ultima, out var siguientes) && siguientes.Count > 0)
                        {
                            var siguiente = siguientes.FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(siguiente))
                            {
                                lock (lockObj)
                                {
                                    autocompletadoReal = siguiente;
                                    sugerenciaVisible = siguiente; 
                                }
                                return;
                            }
                        }

                        // Si no hay bigrama, no sugerimos nada
                        return;
                    }

                    // Si no hay espacio final: intentar autocompletar la palabra actual por prefijo
                    var match = diccionario
                        .Where(p => p.StartsWith(ultima, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.Length)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(match) && match != ultima)
                    {
                        lock (lockObj)
                        {
                            autocompletadoReal = match;
                            sugerenciaVisible = match.Substring(ultima.Length);
                        }
                        return;
                    }

                    
                    if (bigramas.TryGetValue(ultima, out var lista) && lista.Count > 0)
                    {
                        var next = lista.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(next))
                        {
                            lock (lockObj)
                            {
                                autocompletadoReal = next;
                                sugerenciaVisible = " " + next; 
                            }
                        }
                    }
                }
                catch { }
            }, token);
        }

        // Devuelve lo que se debe mostrar como ghost-text (puede empezar por espacio)
        public string ObtenerGhostText()
        {
            lock (lockObj) return sugerenciaVisible;
        }

        public bool HaySugerencia()
        {
            lock (lockObj) return !string.IsNullOrEmpty(autocompletadoReal);
        }

        // Completa con TAB y limpia estado interno para evitar duplicados
        public string CompletarConTab(string input)
        {
            lock (lockObj)
            {
                if (string.IsNullOrWhiteSpace(autocompletadoReal))
                    return input;

                string result = input;

                
                if (input.EndsWith(" "))
                {
                    result = input + autocompletadoReal + " ";
                }
                else
                {
                    
                    var partes = input.Split(' ').ToList();
                    string ultima = partes.LastOrDefault() ?? "";
                    if (!string.IsNullOrEmpty(ultima) && autocompletadoReal.StartsWith(ultima, StringComparison.OrdinalIgnoreCase))
                    {
                        partes[partes.Count - 1] = autocompletadoReal;
                        result = string.Join(" ", partes) + " ";
                    }
                    else
                    {
                        
                        result = input + " " + autocompletadoReal + " ";
                    }
                }

                
                autocompletadoReal = "";
                sugerenciaVisible = "";

                return result;
            }
        }

        
        public void RechazarSugerencia()
        {
            lock (lockObj)
            {
                autocompletadoReal = "";
                sugerenciaVisible = "";
            }
        }
    }
}
