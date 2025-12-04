namespace ProyectoFinalProgramacionParalela
{
    public class MotorBusquedaSingleton
    {
        private static MotorBusquedaSingleton instance = null;
        private static readonly object _lock = new object();

        MotorBusquedaSingleton() { }
        
        public static MotorBusquedaSingleton MotorBusqueda
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new MotorBusquedaSingleton();
                    }
                    return instance;
                }
            }
        }
    }
}