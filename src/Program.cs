namespace ProyectoFinalProgramacionParalela
{
    public class Program
    {
        public static void MostrarTabla(List<string> lista)
        {
            Console.WriteLine("0. Abortar seleccion");
            for (int i = 0; i < lista.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {lista[i]}");
            }
            int archivoIdx = -1;
            while (archivoIdx == -1)
            {
                Console.Write("Seleccione un archivo para abrir [0]: ");
                try
                {
                    archivoIdx = int.Parse(Console.ReadLine() ?? "0");
                }
                catch (FormatException)
                {
                    archivoIdx = -1;
                }
                if(archivoIdx > lista.Count || archivoIdx < 0) archivoIdx = -1;
            }
            Console.WriteLine($"Abriendo el archivo {lista[archivoIdx]}");
            MotorBusquedaSingleton.AbrirArchivo(lista[archivoIdx]);
        }
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
            DatosSingleton capaDatos = DatosSingleton.Datos;
            MotorBusquedaSingleton motorBusqueda = MotorBusquedaSingleton.MotorBusqueda;
            MotorSugerenciasSingleton motorSugerencias = MotorSugerenciasSingleton.MotorSugerencias;
            MetricasSingleton metricas = MetricasSingleton.Metricas;
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
            MostrarTabla(await motorBusqueda.Buscar(input));
        }
    }
};