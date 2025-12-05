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
    }
}