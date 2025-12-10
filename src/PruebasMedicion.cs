using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProyectoFinalProgramacionParalela
{
    public class PruebasMedicion
    {
        private static Logs logsPruebas = new Logs("pruebas-logs", nivel: LogsNivel.INFO);
        private MetricasSingleton metricas;
        private MotorBusquedaSingleton motor;
        private ConfiguracionSingleton configuracion;

        private Dictionary<string, ResultadoPrueba> resultadosPruebas = new Dictionary<string, ResultadoPrueba>();

        public class ResultadoPrueba
        {
            public string Modo { get; set; }
            public long LatenciaMs { get; set; }
            public int ArchivosEncontrados { get; set; }
            public int ArchivosLeidosConcurrentes { get; set; }
            public int HilosUtilizados { get; set; }

            public double Speedup { get; set; }
            public double Eficiencia { get; set; }
        }

        public PruebasMedicion()
        {
            metricas = MetricasSingleton.Instancia;
            motor = MotorBusquedaSingleton.MotorBusqueda;
            configuracion = ConfiguracionSingleton.Configuracion;
        }

        public void EmpezarPruebas(string textoBuscar, int numNucleos)
        {
            logsPruebas.WriteLine("Iniciando Prueba de Medición ", LogsNivel.INFO);
            logsPruebas.WriteLine($"Texto a buscar: '{textoBuscar}'", LogsNivel.INFO);
            logsPruebas.WriteLine($"Directorio: {configuracion.GetDirectorio()}", LogsNivel.INFO);
            logsPruebas.WriteLine($"Procesadores disponibles: {Environment.ProcessorCount}", LogsNivel.INFO);
            logsPruebas.WriteLine($"Núcleos a utilizar: {numNucleos}", LogsNivel.INFO);
            
            configuracion.SetModo(ConfiguracionModo.Custom);
            configuracion.SetHilos(numNucleos);
            
            logsPruebas.WriteLine($"\n Ejecutando prueba con {numNucleos} núcleos ", LogsNivel.INFO);
            EjecutarPruebasMotorBusqueda(textoBuscar, numNucleos);
 
            logsPruebas.WriteLine("\n Prueba Finalizada", LogsNivel.INFO);
        }
        public void MostrarSpeedupYEficiencia(double tiempoSecuencialMs, int numeroProcesadores)
        {
            if (tiempoSecuencialMs <= 0)
            {
                logsPruebas.WriteLine("Tiempo secuencial inválido (<= 0). No se puede calcular speedup/eficiencia.", LogsNivel.ERROR);
                return;
            }

            logsPruebas.WriteLine("\n Resumen de Speedup / Eficiencia ", LogsNivel.INFO);
            logsPruebas.WriteLine($"Base secuencial: {tiempoSecuencialMs} ms", LogsNivel.INFO);
            logsPruebas.WriteLine($"Número de procesadores: {numeroProcesadores}", LogsNivel.INFO);

            foreach (var kv in resultadosPruebas)
            {
                var resultado = kv.Value;
                
                double speedup = metricas.CalcularSpeedup(tiempoSecuencialMs / 1000.0, resultado.LatenciaMs / 1000.0);
                double eficiencia = metricas.CalcularEficiencia(speedup, resultado.HilosUtilizados);

                logsPruebas.WriteLine($"Modo: {resultado.Modo} | Latencia: {resultado.LatenciaMs} ms | Speedup: {speedup:F3} | Eficiencia: {eficiencia:P2}", LogsNivel.INFO);
            }
        }

        private void EjecutarPruebasMotorBusqueda(string textoBuscar, int numNucleos)
        {
            metricas.Reiniciar();
            
            metricas.TiempoTotal.Start();
            metricas.Medir($"busqueda_{numNucleos}_nucleos");

            try
            {
                var resultados = motor.Buscar(textoBuscar);
                
                metricas.PararMedicion($"busqueda_{numNucleos}_nucleos");
                metricas.TiempoTotal.Stop();
                
                long latenciaMs = metricas.LatenciasModulos[$"busqueda_{numNucleos}_nucleos"];
                double latenciaSeg = latenciaMs / 1000.0;
                
                int throughput = numNucleos;
                metricas.EstablecerThroughput(throughput);
                                
                int archivosEncontrados = resultados.Count;

                var resultado = new ResultadoPrueba
                {
                    Modo = $"{numNucleos}_nucleos",
                    LatenciaMs = latenciaMs,
                    ArchivosEncontrados = archivosEncontrados,
                    ArchivosLeidosConcurrentes = throughput,
                    HilosUtilizados = numNucleos,
                    Speedup = 0,
                    Eficiencia = 0
                };
                resultadosPruebas[$"{numNucleos}_nucleos"] = resultado;

                logsPruebas.WriteLine($"\n[Metricas]", LogsNivel.INFO);
                logsPruebas.WriteLine($"Latencia: {latenciaMs}ms ({latenciaSeg:F3}s)", LogsNivel.INFO);
                logsPruebas.WriteLine($"Throughput: {throughput} archivos siendo leídos concurrentemente", LogsNivel.INFO);
                logsPruebas.WriteLine($"Archivos Encontrados: {archivosEncontrados}", LogsNivel.INFO);
                logsPruebas.WriteLine($"Hilos Utilizados: {numNucleos}", LogsNivel.INFO);

                if (archivosEncontrados > 0)
                {
                    logsPruebas.WriteLine($"\nPrimeros 5 archivos encontrados:", LogsNivel.DEBUG);
                    foreach (var archivo in resultados.Take(5))
                    {
                        logsPruebas.WriteLine($"  → {archivo}", LogsNivel.DEBUG);
                    }
                    if (archivosEncontrados > 5)
                    {
                        logsPruebas.WriteLine($"  ... y {archivosEncontrados - 5} más", LogsNivel.DEBUG);
                    }
                }
            }
            catch (Exception ex)
            {
                metricas.TiempoTotal.Stop();
                logsPruebas.WriteLine($" Error durante la prueba: {ex.Message}", LogsNivel.ERROR);
            }
        }
    }
}