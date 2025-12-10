using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ProyectoFinalProgramacionParalela
{
    public class MetricasSingleton
    {
        private static MetricasSingleton instance = null;
        private static readonly object _lock = new object();
        private readonly object _lockLatencias = new object();

        private MetricasSingleton()
        {
            ArchivosProcesados = 0;
            LineasLeidas = 0;
            Coincidencias = 0;
            TiempoTotal = new Stopwatch();
            Throughput = 0;
            LatenciasModulos = new Dictionary<string, long>();
            StopwatchesModulos = new Dictionary<string, Stopwatch>();
        }
        
        public static MetricasSingleton Instancia
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

        public int ArchivosProcesados;
        public int LineasLeidas;
        public int Coincidencias;
        public int Throughput; 
        public Stopwatch TiempoTotal;
        
        public Dictionary<string, long> LatenciasModulos;
        private Dictionary<string, Stopwatch> StopwatchesModulos;


        public void EstablecerThroughput(int archivos) => Throughput = archivos;

        public void Medir(string nombre)
        {
            lock (_lockLatencias)
            {
                if (!StopwatchesModulos.ContainsKey(nombre))
                {
                    StopwatchesModulos[nombre] = new Stopwatch();
                }
                StopwatchesModulos[nombre].Restart();
            }
        }

        public void PararMedicion(string nombre)
        {
            lock (_lockLatencias)
            {
                if (StopwatchesModulos.ContainsKey(nombre))
                {
                    StopwatchesModulos[nombre].Stop();
                    LatenciasModulos[nombre] = StopwatchesModulos[nombre].ElapsedMilliseconds;
                }
            }
        }

        
        public double CalcularSpeedup(double tiempoSecuencial, double tiempoParalelo)
        {
            if (tiempoParalelo <= 0 || tiempoSecuencial <= 0)
                return 0;
            return tiempoSecuencial / tiempoParalelo;
        }

        public double CalcularEficiencia(double speedup, double numProcesadores)
        {
            if (numProcesadores <= 0)
                return 0;
            return speedup / numProcesadores;
        }

        public void Reiniciar()
        {
            ArchivosProcesados = 0;
            LineasLeidas = 0;
            Coincidencias = 0;
            Throughput = 0;
            TiempoTotal.Reset();
            
            lock (_lockLatencias)
            {
                LatenciasModulos.Clear();
                StopwatchesModulos.Clear();
            }
        }
    }
}