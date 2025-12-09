using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace ProyectoFinalProgramacionParalela
{
    public class MotorSugerenciasSingleton
    {
        private static MotorSugerenciasSingleton? instance = null;
        private static readonly object _lock = new object();

        private static Logs logsSugerencias = new Logs(LogsNivel.ERROR);

        private List<string> diccionario;
    

        MotorSugerenciasSingleton()
        {
            diccionario = new();
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
        public void CargarDiccionarioDesdeTXT(string carpeta)
        {
            diccionario.Clear();

            if (!Directory.Exists(carpeta))
            {
                Console.WriteLine($"No existe la carpeta '{carpeta}'");
                return;
            }

            var archivos = Directory.GetFiles(carpeta, "*.txt");

            foreach (var archivo in archivos)
            {
                string texto = File.ReadAllText(archivo).ToLower();

                var palabras = Regex.Matches(texto, @"\b[\wáéíóúñü]+\b")
                                    .Select(m => m.Value)
                                    .Distinct();

                diccionario.AddRange(palabras);
            }

            diccionario = diccionario.Distinct().OrderBy(p => p).ToList();
        }


        public string ObtenerUltimaPalabra(string input)
        {
            var partes = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 0 ? "" : partes[^1];
        }


        public string ObtenerResto(string input)
        {
            var index = input.LastIndexOf(' ');
            if (index == -1) return "";
            return input[..(index + 1)];
        }

        public string? BuscarCoincidencia(string palabra)
        {
            if (string.IsNullOrWhiteSpace(palabra))
                return null;

            return diccionario
                .FirstOrDefault(p => p.StartsWith(palabra, StringComparison.OrdinalIgnoreCase));
        }

        public void PintarInputConGhost(string input)
        {
            Console.Write(input);

            string ultima = ObtenerUltimaPalabra(input);
            string resto = ObtenerResto(input);

            string? sugerencia = BuscarCoincidencia(ultima);

            if (sugerencia != null && sugerencia.Length > ultima.Length)
            {
                string fantasma = sugerencia[ultima.Length..];
                AnsiConsole.Markup($"[grey]{fantasma}[/]");
            }
        }
    }
}
