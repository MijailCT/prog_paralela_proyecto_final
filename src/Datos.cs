
using Microsoft.Data.Sqlite;
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
    }
}