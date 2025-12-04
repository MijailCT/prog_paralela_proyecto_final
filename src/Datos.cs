namespace ProyectoFinalProgramacionParalela
{
    //OJO: Capa de datos
    public sealed class DatosSingleton
    {
        private static DatosSingleton instance = null;
        private static readonly object _lock = new object();

        DatosSingleton() { }

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
    }
}