using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace ProyectoFinalProgramacionParalela
{
    //OJO: Capa de datos
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

        private void InicializarBaseDeDatos()
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
    }
}

