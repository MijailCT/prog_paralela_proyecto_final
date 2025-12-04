using System.Text.Json;

namespace ProyectoFinalProgramacionParalela
{
    public enum ConfiguracionModo
    {
        Light, //Mitad de los nucleos
        Heavy, //TODOS los nucleos
        Custom, //Cantidad especificada por el usuario en la configuracion
        Optimized, //MaxDegreeOfParallelism -1
    }

    public class ConfiguracionDatos
    {
        public ConfiguracionModo modo { get; set; } = ConfiguracionModo.Optimized;
        public int hilos { get; set; } = Environment.ProcessorCount;
        public string directorio { get; set; } = "";
        public bool lanzadoPrimeraVez { get; set; } = false;
    }

    public sealed class ConfiguracionSingleton
    {
        private static ConfiguracionSingleton instance = null;
        private static readonly object _lock = new object();

        private static JsonSerializerOptions jsonConf = new JsonSerializerOptions();
        private ConfiguracionDatos datos;
        private readonly object datos_lock = new object();

        ConfiguracionSingleton(ConfiguracionDatos datosConf)
        {
            datos = datosConf;
        }

        public void Guardar()
        {
            lock (datos_lock)
            {
                if (!datos.lanzadoPrimeraVez) datos.lanzadoPrimeraVez = true;
                string datosJson = JsonSerializer.Serialize(datos, jsonConf);
                File.WriteAllText("configuracion.json", datosJson);
            }
        }

        public void Cargar()
        {
            lock (datos_lock)
            {
                if (File.Exists("configuracion.json"))
                {
                    datos = JsonSerializer.Deserialize<ConfiguracionDatos>(File.ReadAllText("configuracion.json"))!;
                }
            }
        }

        public ParallelOptions GetOpcionesParalelas()
        {
            switch (datos.modo)
            {
                case ConfiguracionModo.Light:
                    return new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };
                case ConfiguracionModo.Heavy:
                    return new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                case ConfiguracionModo.Custom:
                    return new ParallelOptions { MaxDegreeOfParallelism = datos.hilos };

                //Fallback hacia el modo optimizado en caso de que el modo no sea valido
                default:
                case ConfiguracionModo.Optimized:
                    return new ParallelOptions { MaxDegreeOfParallelism = -1 };
            }
        }

        public bool GetLanzadoPrimeraVez() => datos.lanzadoPrimeraVez;

        public string GetDirectorio() => datos.directorio;
        public void SetDirectorio(string nuevo_directorio)
        {
            lock (datos_lock)
            {
                if (Directory.Exists(nuevo_directorio))
                    datos.directorio = nuevo_directorio;
            }
        }

        public ConfiguracionModo GetModo() => datos.modo;
        public void SetModo(ConfiguracionModo nuevo_modo)
        {
            lock (datos_lock)
            {
                datos.modo = nuevo_modo;
            }
        }

        public void SetHilos(int nuevo_hilos)
        {
            lock (datos_lock)
            {
                datos.hilos = nuevo_hilos;
            }
        }


        public static ConfiguracionSingleton Configuracion
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        ConfiguracionDatos datosConf = new ConfiguracionDatos();
                        if (File.Exists("configuracion.json"))
                        {
                            datosConf = JsonSerializer.Deserialize<ConfiguracionDatos>(File.ReadAllText("configuracion.json"))!;
                        }
                        instance = new ConfiguracionSingleton(datosConf);
                    }
                    return instance;
                }
            }
        }
    }
}