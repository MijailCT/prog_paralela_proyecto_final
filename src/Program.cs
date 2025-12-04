using System.Text.Json;

namespace ProyectoFinalProgramacionParalela
{
    public class Program
    {
        public static async Task Main()
        {
            Logs debugLogs = new Logs(LogsNivel.DEBUG);
            ConfiguracionSingleton conf = ConfiguracionSingleton.Configuracion;
            //NOTA: esto es una base primitiva adonde empezar a trabajar, 
            // a medida que avancemos esto se hara haciendo mejor.

            Console.WriteLine("Buscador de texto en archivos V0.1");

            if (!conf.GetLanzadoPrimeraVez())
            {
                Console.WriteLine("[Configuracion inicial]");
                Console.WriteLine("A continuacion escribira datos necesarios para el funcionamiento del programa.");
                Console.Write("Directorio de trabajo: ");
                string dir = Console.ReadLine() ?? "./";
                while (!Directory.Exists(dir))
                {
                    Console.WriteLine("ERROR: El directorio de trabajo proporcionado no existe.");
                    Console.Write("Directorio de trabajo: ");
                    dir = Console.ReadLine() ?? "./";
                }
                conf.SetDirectorio(dir);
                conf.Guardar();
            }

            //TODO: hacer un menu + loop principal
            MotorBusquedaSingleton motorBusqueda = MotorBusquedaSingleton.MotorBusqueda;
            MotorSugerenciasSingleton motorSugerencias = MotorSugerenciasSingleton.MotorSugerencias;
            MetricasSingleton metricas = MetricasSingleton.Metricas;
            DatosSingleton capaDatos = DatosSingleton.Datos;
            //List<File> files = Directory.GetFiles(dir); (un suponer)
            Console.WriteLine("Escribe tu busqueda, al iniciar se le recomendara " +
            "palabras que podria utilizar, estas pueden aceptarse con la tecla TAB.");
            Console.WriteLine("Para buscar el texto en los archivos tienes que presionar la tecla ENTER.");
            Console.WriteLine("NOTA: Al darle a la tecla enter, se perdera la recomendacion.");
            Console.Write("Busqueda: ");

            string input = "";
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length == 0) continue;
                    input = input.Substring(0, input.Length - 1);
                    var (left, top) = Console.GetCursorPosition();
                    Console.SetCursorPosition(left - 1, top);
                    Console.Write(' ');
                    Console.SetCursorPosition(left - 1, top);
                    await Console.Out.FlushAsync(); //por si acaso
                }
                else
                {
                    input += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
                //motorSugerencias.Predecir(input); (un suponer)
            }
            Console.WriteLine();
            Console.WriteLine($"Buscando el texto {input} en archivos...");
            //motorBusqueda.Buscar(dir, input); (un suponer)
        }
    }
    //TODO: mover cada clase a archivos independientes para mejor organizacion
    public class MotorBusquedaSingleton
    {
        private static MotorBusquedaSingleton instance = null;
        private static readonly object _lock = new object();

        MotorBusquedaSingleton() { }
        
        public static MotorBusquedaSingleton MotorBusqueda
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new MotorBusquedaSingleton();
                    }
                    return instance;
                }
            }
        }
    }

    public class MotorSugerenciasSingleton
    {
        private static MotorSugerenciasSingleton instance = null;
        private static readonly object _lock = new object();

        MotorSugerenciasSingleton() { }
        
        public static MotorSugerenciasSingleton MotorSugerencias
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new MotorSugerenciasSingleton();
                    }
                    return instance;
                }
            }
        }
    }

    public class MetricasSingleton
    {
        private static MetricasSingleton instance = null;
        private static readonly object _lock = new object();

        MetricasSingleton() { }
        
        public static MetricasSingleton Metricas
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new MetricasSingleton();
                    }
                    return instance;
                }
            }
        }
    }

    //OJO: Capa de datos
    public sealed class DatosSingleton
    {
        private static DatosSingleton instance = null;
        private static readonly object _lock = new object();

        DatosSingleton() { }

        public static DatosSingleton Datos
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new DatosSingleton();
                    }
                    return instance;
                }
            }
        }
    }

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

    public enum LogsNivel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }
    
    public class Logs
    {
        private string logFilePath;
        private LogsNivel nivel_minimo;

        public Logs(LogsNivel nivel = LogsNivel.INFO)
        {
            logFilePath = $"log-{DateTime.Now.ToString("O")}.txt";
            nivel_minimo = nivel;
        }

        public Logs(string path, bool useDate = true, LogsNivel nivel = LogsNivel.INFO)
        {
            if (useDate) logFilePath = $"{path}-{DateTime.Now.ToString("O")}.txt";
            else logFilePath = path;
            nivel_minimo = nivel;
        }

        public void Write(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}");
        }

        public async Task WriteAsync(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) await File.AppendAllTextAsync(logFilePath, $"[{nvl.ToString()}] {txt}");
        }

        public void WriteLine(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if ((nivel ?? nivel_minimo) >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}\n");
        }
        
        public async Task WriteLineAsync(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if((nivel ?? nivel_minimo) >= nivel_minimo) await File.AppendAllTextAsync(logFilePath, $"[{nvl.ToString()}] {txt}\n");
        }
    }
};