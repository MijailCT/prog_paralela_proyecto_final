using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace ProyectoFinalProgramacionParalela.Tests
{
    public class TestsRendimiento
    {
        private readonly string carpetaTemporal;

        public TestsRendimiento()
        {

            //Configurar el directorio del programa
            ConfiguracionSingleton.Configuracion.SetDirectorio("C:\\Users\\Mijail\\Desktop\\UniProjects\\ProgramacionParalela\\prog_paralela_proyecto_final\\dataset");
        }

        [Fact]
        public void Prueba_Rendimiento_Latencia_Speedup_Eficiencia()
        {
            var metricas = MetricasSingleton.Instancia;
            var motor = MotorBusquedaSingleton.MotorBusqueda;
            var conf = ConfiguracionSingleton.Configuracion;

            //Ejecucion secuencial
            ConfiguracionSingleton.Configuracion.SetDirectorio("C:\\Users\\Mijail\\Desktop\\UniProjects\\ProgramacionParalela\\prog_paralela_proyecto_final\\dataset");
            metricas.Reiniciar();
            conf.SetModo(ConfiguracionModo.Custom);
            conf.SetHilos(1);

            metricas.Medir("secuencial");
            var resultadosSec = motor.Buscar("reader");
            metricas.PararMedicion("secuencial");

            long latenciaSec = metricas.LatenciasModulos["secuencial"];
            Assert.True(latenciaSec > 0); //Por si acaso

            //EJECUCION PARALELA
            ConfiguracionSingleton.Configuracion.SetDirectorio("C:\\Users\\Mijail\\Desktop\\UniProjects\\ProgramacionParalela\\prog_paralela_proyecto_final\\dataset");
            metricas.Reiniciar();
            int hilos = Environment.ProcessorCount;
            conf.SetModo(ConfiguracionModo.Custom);
            conf.SetHilos(hilos);

            metricas.Medir("paralelo");
            var resultadosPar = motor.Buscar("reader");
            metricas.PararMedicion("paralelo");

            long latenciaPar = metricas.LatenciasModulos["paralelo"];
            Assert.True(latenciaPar > 0);

            double tiempoSec = latenciaSec / 1000.0;
            double tiempoPar = latenciaPar / 1000.0;

            //Calculos
            double speedup = metricas.CalcularSpeedup(tiempoSec, tiempoPar);
            //double eficiencia = metricas.CalcularEficiencia(speedup, hilos);
            double eficiencia = speedup / (double)hilos;

            //Validaciones formales

            Assert.True(latenciaPar < latenciaSec);

            Assert.True(speedup > 1);

            Assert.True(eficiencia > 0, "La eficiencia debe ser positiva.");

            Assert.Equal(resultadosSec.Count, resultadosPar.Count);

            string strMetricas = "";

            strMetricas = "----- RESULTADOS METRICAS -----\n";
            strMetricas += $"Latencia Secuencial: {latenciaSec} ms\n";
            strMetricas += $"Latencia Paralela:   {latenciaPar} ms\n";
            strMetricas += $"Speedup:             {speedup.ToString("0.00")}\n";
            strMetricas += $"Eficiencia:          {eficiencia.ToString("0.00")}\n";
            strMetricas += $"Hilos:               {hilos.ToString()}\n";
            strMetricas += "--------------------------------\n";
            var r = new Random();
            File.WriteAllText(
                $"C:\\Users\\Mijail\\Desktop\\UniProjects\\ProgramacionParalela\\prog_paralela_proyecto_final\\metrics\\metricas_prueba_motor_busqueda{r.Next(0,999999)}.txt",
                strMetricas);
        }
    }
}
