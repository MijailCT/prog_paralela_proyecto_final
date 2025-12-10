using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ProyectoFinalProgramacionParalela
{
    
    public sealed class DatosSingleton
    {
        private static DatosSingleton instance = null;
        private static readonly object _lock = new object();

        private readonly string dbPath = "prog_paralela_proyecto.db";
        private readonly string connectionString;

        // Cache en memoria para busquedas rapidas
        private readonly ConcurrentDictionary<string, string> cacheContenido = new();

        DatosSingleton()
        {
            connectionString = $"Data Source={dbPath}";
            InicializarBaseDeDatos();
        }

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

        public void InicializarBaseDeDatos()
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            var comandos = new[]
            {
                @"CREATE TABLE IF NOT EXISTS Documentos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Ruta TEXT NOT NULL UNIQUE,
                    Contenido TEXT NOT NULL,
                    Palabras INTEGER NOT NULL,
                    TamanoKB REAL NOT NULL,
                    Puntaje INT NOT NULL,
                    FechaIndexacion DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

                @"CREATE TABLE IF NOT EXISTS Metricas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Operacion TEXT NOT NULL,
                    TiempoMs REAL NOT NULL,
                    HilosUsados INTEGER,
                    DocumentosProcesados INTEGER,
                    Fecha DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

                @"CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nivel TEXT NOT NULL,
                    Mensaje TEXT NOT NULL,
                    Fecha DATETIME DEFAULT CURRENT_TIMESTAMP
                );"
            };

            foreach (var sql in comandos)
            {
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            
        }

        // Guarda o actualiza documento con puntaje
        public async Task GuardarDocumentoAsync(string ruta, string contenido, int puntaje = 0)
        {
            cacheContenido[ruta] = contenido;

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();

                const string sql = @"
                    INSERT INTO Documentos (Ruta, Contenido, Palabras, TamanoKB, Puntaje)
                    VALUES (@ruta, @contenido, @palabras, @tamano, @puntaje)
                    ON CONFLICT(Ruta) DO UPDATE SET
                        Contenido = excluded.Contenido,
                        Palabras = excluded.Palabras,
                        TamanoKB = excluded.TamanoKB,
                        Puntaje = excluded.Puntaje,
                        FechaIndexacion = CURRENT_TIMESTAMP;";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.Parameters.AddWithValue("@contenido", contenido);
                cmd.Parameters.AddWithValue("@palabras", contenido.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length);
                cmd.Parameters.AddWithValue("@tamano", new FileInfo(ruta).Length / 1024.0);
                cmd.Parameters.AddWithValue("@puntaje", puntaje);
                cmd.ExecuteNonQuery();
            });
        }

        public List<string> ObtenerTodasLasRutas()
        {
            return new List<string>(cacheContenido.Keys);
        }

        public async Task IncrementarPuntajeAsync(string ruta, int incremento = 1)
        {
            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = new SqliteCommand("UPDATE Documentos SET Puntaje = Puntaje + @inc WHERE Ruta = @ruta", conn);
                cmd.Parameters.AddWithValue("@inc", incremento);
                cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.ExecuteNonQuery();
            });
        }

        public async Task IndexarDirectorioAsync(string directorio)
        {
            if (!Directory.Exists(directorio)) return;

            var archivos = EnumerarArchivo(directorio, "*.txt");
            var opciones = ConfiguracionSingleton.Configuracion.GetOpcionesParalelas();

            await Parallel.ForEachAsync(archivos, opciones, async (archivo, token) =>
            {
                try
                {
                    string contenido = await File.ReadAllTextAsync(archivo, token);
                    await GuardarDocumentoAsync(archivo, contenido);
                }
                catch { }
            });
        }


        public bool ExisteDocumento(string ruta)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            const string sql = "SELECT COUNT(*) FROM Documentos WHERE Ruta = @ruta LIMIT 1";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ruta", ruta);
            return (long?)cmd.ExecuteScalar() > 0;
        }

        // busqueda y orden de puntaje 
        public List<string> BuscarDocumentosQueContengan(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new();

            var resultados = new ConcurrentBag<string>();

            Parallel.ForEach(cacheContenido, kv =>
            {
                if (kv.Value.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    resultados.Add(kv.Key);
            });

            return resultados
                .OrderByDescending(ruta => ObtenerPuntaje(ruta))
                .ToList();
        }

        public int ObtenerPuntaje(string ruta)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = new SqliteCommand("SELECT Puntaje FROM Documentos WHERE Ruta = @ruta", conn);
            cmd.Parameters.AddWithValue("@ruta", ruta);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // metricas y logs
        public async Task RegistrarMetricaAsync(string operacion, double tiempoMs, int? hilos = null, int? docs = null)
        {
            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = new SqliteCommand(
                    "INSERT INTO Metricas (Operacion, TiempoMs, HilosUsados, DocumentosProcesados) VALUES (@op, @t, @h, @d)", conn);
                cmd.Parameters.AddWithValue("@op", operacion);
                cmd.Parameters.AddWithValue("@t", tiempoMs);
                cmd.Parameters.AddWithValue("@h", hilos ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@d", docs ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            });
        }

        public async Task LogAsync(string nivel, string mensaje)
        {
            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = new SqliteCommand("INSERT INTO Logs (Nivel, Mensaje) VALUES (@n, @m)", conn);
                cmd.Parameters.AddWithValue("@n", nivel.ToUpper());
                cmd.Parameters.AddWithValue("@m", mensaje);
                cmd.ExecuteNonQuery();
            });
        }

        // limpiar
        public void LimpiarTodo()
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
            cacheContenido.Clear();
            InicializarBaseDeDatos();
        }
    }
}