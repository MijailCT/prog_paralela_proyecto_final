namespace ProyectoFinalProgramacionParalela
{
    public class MetricasSingleton
    {
        private static MetricasSingleton instance = null;
        private static readonly object _lock = new object();

        MetricasSingleton() { }
        
        public static MetricasSingleton Metricas
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
    }
}