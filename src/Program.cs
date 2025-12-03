using System.Security.Cryptography.X509Certificates;

namespace ProyectoFinalProgramacionParalela
{
    public class Program
    {
        public static void Main()
        {
            Logs debugLogs = new Logs(LogsNivel.DEBUG);
            //NOTA: esto es una base primitiva adonde empezar a trabajar, 
            // a medida que avancemos esto se hara haciendo mejor.

            Console.WriteLine("Buscador de texto en archivos V0.1");

            //TODO: hacer un loop con todo esto
            //TODO: hacer todo esto de forma asincrona(?)
            Console.Write("Directorio de trabajo: ");
            string dir = Console.ReadLine() ?? "./";
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("ERROR: El directorio de trabajo proporcionado no existe.");
                return;
            }

            MotorBusquedaSingleton motorBusqueda = new MotorBusquedaSingleton();
            MotorSugerenciasSingleton motorSugerencias = new MotorSugerenciasSingleton();
            MetricasSingleton metricas = new MetricasSingleton();
            DatosSingleton capaDatos = new DatosSingleton();
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
                    Console.Out.Flush(); //por si acaso
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
                lock(_lock)
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
                lock(_lock)
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
                lock(_lock)
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
    public class DatosSingleton
    {
        private static DatosSingleton instance = null;
        private static readonly object _lock = new object();

        DatosSingleton() { }
        
        public static DatosSingleton Datos
        {
            get
            {
                lock(_lock)
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

    public sealed class ConfiguracionSingleton
    {
        private static ConfiguracionSingleton instance = null;
        private static readonly object _lock = new object();

        ConfiguracionSingleton(){}

        public static ConfiguracionSingleton Configuracion
        {
            get
            {
                lock(_lock)
                {
                    if (instance == null)
                    {
                        instance = new ConfiguracionSingleton();
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
            if(nvl >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}");
        }

        public void WriteLine(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if((nivel ?? nivel_minimo) >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}\n");
        }
    }
};