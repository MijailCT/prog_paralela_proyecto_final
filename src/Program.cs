using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProyectoFinalProgramacionParalela
{
    public class Program
    {

        public static int MostrarOpciones(List<string> lista,
        string seleccionTexto = "Seleccione un item",
        string seleccionCero = "Abortar seleccion")
        {
            Console.WriteLine($"0. {seleccionCero}");
            for (int i = 0; i < lista.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {lista[i]}");
            }
            int seleccionIdx = -1;
            while (seleccionIdx == -1)
            {
                Console.Write($"{seleccionTexto} [DEJAR VACIO para seleccionar 0]: ");
                try
                {
                    string numeroStr = Console.ReadLine() ?? "0";
                    if (numeroStr == "") numeroStr = "0";
                    seleccionIdx = int.Parse(numeroStr);
                }
                catch (FormatException)
                {
                    seleccionIdx = -1;
                }
                if (seleccionIdx > lista.Count || seleccionIdx < 0) seleccionIdx = -1;
            }
            return seleccionIdx == 0 ? -1 : seleccionIdx;
        }

        public static void MostrarTablaArchivos(List<string> lista)
        {
            int archivoIdx = MostrarOpciones(lista, "Seleccione un archivo para abrir");
            if (archivoIdx == -1) return;
            Console.WriteLine($"Abriendo el archivo {lista[archivoIdx - 1]}!");
            MotorBusquedaSingleton.AbrirArchivo(lista[archivoIdx - 1]);
        }


        public static async Task Busqueda()
        {
            Console.Clear();
            
            DatosSingleton capaDatos = DatosSingleton.Datos;
            MotorBusquedaSingleton motorBusqueda = MotorBusquedaSingleton.MotorBusqueda;
            MotorSugerenciasSingleton motorSugerencias = MotorSugerenciasSingleton.MotorSugerencias;
            MetricasSingleton metricas = MetricasSingleton.Metricas;

            string input = "";
            string? sugerenciaActual = null;
            string ultimoInputProcesado = "";
            while (true)
            {
                //Si el input cambio, lanzamos una sugerencia pero para la version capturada nadamas
                if (input != ultimoInputProcesado)
                {
                    ultimoInputProcesado = input;
                    string captured = input; //Capturamos el input
                    //Obtenemos la ultima palabra, pero del input
                    string ultimaParaCaptured = motorSugerencias.ObtenerUltimaPalabra(captured);

                    //Para matar la warning lo asignamos a _
                    //Y para no bloquear lo hacemos de forma paralela
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            //Buscamos la coincidencia con el input que capturamos
                            var coincidencia = await motorSugerencias.BuscarCoincidenciaAsync(ultimaParaCaptured);
                            //Si hemos podido procesar antes de que el usuario cambie el input, la sugerimos
                            //Esto es para evitar condiciones de carrera
                            if (captured == ultimoInputProcesado)
                                sugerenciaActual = coincidencia;
                        }
                        //Por si acaso agarramos errores
                        catch (OperationCanceledException) { }
                        catch (Exception) { }
                    });
                }

                Console.Clear();
                Console.WriteLine("[Busqueda de texto en archivos]");
                Console.WriteLine("Escribe tu busqueda, al iniciar se le recomendara " +
                "palabras que podria utilizar, estas pueden aceptarse con la tecla TAB.");
                Console.WriteLine("Para buscar el texto en los archivos tienes que presionar la tecla ENTER.");
                Console.WriteLine("NOTA: Al darle a la tecla enter, se perdera la recomendacion y se hara la busqueda.");

                AnsiConsole.Markup("[green]Busqueda: [/]");
                Console.Write(input);

                //Printeamos la sugerencia como texto fantasma
                string ultima2 = motorSugerencias.ObtenerUltimaPalabra(input);
                if (!string.IsNullOrEmpty(sugerenciaActual) &&
                    sugerenciaActual.StartsWith(ultima2, StringComparison.OrdinalIgnoreCase) &&
                    sugerenciaActual.Length > ultima2.Length)
                {
                    string fantasma = sugerenciaActual[ultima2.Length..];
                    AnsiConsole.Markup($"[grey]{fantasma}[/]");
                }

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Buscando el texto \"{input}\" en archivos...");
                    List<string> resultados = motorBusqueda.Buscar(input);
                    MostrarTablaArchivos(resultados);
                    //Limpiamos las variables de input, sugerenciaActual y ultimoInputProcesado por si acaso
                    input = "";
                    sugerenciaActual = null;
                    ultimoInputProcesado = "";
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    //..^1 significa que excluye el item final
                    if (input.Length > 0) input = input[..^1];
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    if (sugerenciaActual != null)
                    {
                        string resto = motorSugerencias.ObtenerResto(input);
                        input = resto + sugerenciaActual;
                        //Actualizamos el ultimo input procesado y 
                        // limpiamos la sugerencia actual para recomputar
                        sugerenciaActual = null;
                        ultimoInputProcesado = input;
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    //Escribir los caracteres
                    input += key.KeyChar;
                }
            }
        }

        public static async Task Configuracion()
        {
            var conf = ConfiguracionSingleton.Configuracion;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("[Configuracion]");
                Console.WriteLine($"Modo de paralelismo: {conf.GetModo().ToString()}");
                Console.WriteLine($"Hilos: {conf.GetHilos()}");
                Console.WriteLine($"Directorio de trabajo: {conf.GetDirectorio()}");
                int seleccionConf = MostrarOpciones(new List<string>{
                "Ir hacia atras y olvidar los cambios (LOS CAMBIOS NO SE GUARDARAN)",
                "Modo de paralelismo",
                "Hilos (para el modo custom)",
                "Directorio de trabajo"
                }, "Seleccione que opcion quiere configurar", "Ir hacia atras y guardar los cambios");
                switch(seleccionConf - 1)
                {
                    case 0:
                        conf.Cargar();
                        return;
                    case 1:
                        int seleccionModo = MostrarOpciones(new List<string>{
                        "Light: usar la mitad de los hilos",
                        "Heavy: usar TODOS los hilos",
                        "Custom: usar una cantidad de hilos definida por la opcion \"Hilos\"",
                        "Optimized: usar una cantidad de hilos optimizados de forma dinamica por la TPL"
                        }, "Seleccione un modo de paralelismo");
                        if (seleccionModo == -1) break;

                        conf.SetModo((ConfiguracionModo)Enum.ToObject(typeof(ConfiguracionModo), seleccionModo - 1));
                        break;
                    case 2:
                        int hilos = -1;
                        while (hilos == -1)
                        {
                            Console.Write($"Hilos [DEJAR VACIO para seleccionar {Environment.ProcessorCount}]: ");
                            string hilosStr = Console.ReadLine() ?? Environment.ProcessorCount.ToString();
                            if (hilosStr == "") hilosStr = Environment.ProcessorCount.ToString();

                            try
                            {
                                hilos = int.Parse(hilosStr);
                            }
                            catch (FormatException)
                            {
                                hilos = -1;
                            }
                        }
                        conf.SetHilos(hilos);
                        break;
                    case 3:
                        Console.Write("Directorio de trabajo: ");
                        string dir = Console.ReadLine() ?? ".";
                        while (!Directory.Exists(dir))
                        {
                            Console.WriteLine("ERROR: El directorio de trabajo proporcionado no existe.");
                            Console.Write("Directorio de trabajo: ");
                            dir = Console.ReadLine() ?? ".";
                        }
                        conf.SetDirectorio(dir);
                        break;
                    default:
                        conf.Guardar();
                        return;
                }
            }
        }
        
        //Asincrono porque no podemos gastar tiempo de procesamiento haciendo nada 
        public static async Task Error(string texto)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {texto}");
            Console.ResetColor();
            await Task.Delay(2000);
        }

        public static async Task Main()
        {
            Console.Clear();
            Console.CursorVisible = true;

            Logs debugLogs = new Logs(LogsNivel.DEBUG);
            ConfiguracionSingleton conf = ConfiguracionSingleton.Configuracion;
            DatosSingleton capaDatos = DatosSingleton.Datos;
            MetricasSingleton metricas = MetricasSingleton.Metricas;
            //TODO: hacer esto asincrono o optimizar de alguna manera
            MotorSugerenciasSingleton.MotorSugerencias.CargarDiccionarioDesdeTXT(conf.GetDirectorio());
            //archivo de prueba
            //MotorSugerenciasSingleton.MotorSugerencias.CargarDiccionarioDesdeTXT("src/archivos");
            Console.WriteLine("Buscador de texto en archivos V0.5");

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

            while (true)
            {
                Console.WriteLine("[Menu inicial]");
                Console.WriteLine("1. Busqueda de texto en archivos");
                Console.WriteLine("2. Configuracion");
                Console.WriteLine("0. Salir");
                ConsoleKeyInfo key;
                while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
                {
                    if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                    {
                        await Busqueda();
                        break;
                    }
                    if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                    {
                        await Configuracion();
                        break;
                    }
                    else if (key.Key == ConsoleKey.D0 || key.Key == ConsoleKey.NumPad0)
                    {
                        Environment.Exit(0);
                        break;
                    }
                    else
                    {
                        await Error("La opcion seleccionada no existe!");
                        break;
                    }
                }
                Console.Clear();
            }
        }
    }
};
