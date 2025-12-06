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
        private static MotorSugerenciasSingleton instance = null;
        private static readonly object _lock = new object();

        private MotorSugerenciasSingleton()
        {
            palabrasComunes = new List<string>
            {
                "algoritmo", "paralelo", "hilo", "tarea", "buscar", "archivo", "texto",
                "especulativo", "predicción", "sugerencia", "compleción", "código",
                "programación", "multihilo", "procesador", "núcleo", "rendimiento"
            };

            bigramas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            ConstruirBigramas();
        }

        public static MotorSugerenciasSingleton MotorSugerencias
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                        instance = new MotorSugerenciasSingleton();
                    return instance;
                }
            }
        }
        
        private readonly List<string> palabrasComunes;
        private readonly Dictionary<string, List<string>> bigramas;

        private void ConstruirBigramas() 
        {
            var frases = new[]
            {
                "algoritmo paralelo", "programación multihilo", "buscar archivo",
                "tarea especulativa", "sugerencia código", "rendimiento núcleo"
            };

            foreach (var frase in frases)
            {
                var palabras = frase.Split(' ');
                for (int i = 0; i < palabras.Length - 1; i++)
                {
                    var actual = palabras[i];
                    var siguiente = palabras[i + 1];

                    if (!bigramas.ContainsKey(actual))
                        bigramas[actual] = new List<string>();

                    if (!bigramas[actual].Contains(siguiente))
                        bigramas[actual].Add(siguiente);
                }
            }
        }
        private CancellationTokenSource cts;
        private Task tareaEspeculativa;
        private string sugerenciaActual = "";
        private readonly object lockObj = new object();

        public void ActualizarSugerencia(string textoActual)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();

            lock (lockObj) { sugerenciaActual = ""; }

            var token = cts.Token;

            tareaEspeculativa = Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(150);
                    if (token.IsCancellationRequested) return;

                    var ultimaPalabra = ObtenerUltimaPalabra(textoActual);
                    if (string.IsNullOrWhiteSpace(ultimaPalabra)) return;

                    var prediccion = PredecirSiguientePalabra(ultimaPalabra);
                    if (string.IsNullOrEmpty(prediccion)) return;

                    lock (lockObj)
                    {
                        if (!token.IsCancellationRequested)
                            sugerenciaActual = prediccion;
                    }
                }
                catch { }
            }, token);
        }
        
    }
}