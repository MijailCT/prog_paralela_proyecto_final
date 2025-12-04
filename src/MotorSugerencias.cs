namespace ProyectoFinalProgramacionParalela
{
    public class MotorSugerenciasSingleton
    {
        private static MotorSugerenciasSingleton instance = null;
        private static readonly object _lock = new object();

        MotorSugerenciasSingleton() { }
        
        public static MotorSugerenciasSingleton MotorSugerencias
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new MotorSugerenciasSingleton();
                    }
                    return instance;
                }
            }
        }
    }
}