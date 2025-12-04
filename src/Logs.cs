namespace ProyectoFinalProgramacionParalela
{
    public enum LogsNivel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    public class Logs
    {
        private string logFilePath;
        private LogsNivel nivel_minimo;

        public Logs(LogsNivel nivel = LogsNivel.INFO)
        {
            logFilePath = $"log-{DateTime.Now.ToString("O")}.txt";
            nivel_minimo = nivel;
        }

        public Logs(string path, bool useDate = true, LogsNivel nivel = LogsNivel.INFO)
        {
            if (useDate) logFilePath = $"{path}-{DateTime.Now.ToString("O")}.txt";
            else logFilePath = path;
            nivel_minimo = nivel;
        }

        public void Write(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}");
        }

        public async Task WriteAsync(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) await File.AppendAllTextAsync(logFilePath, $"[{nvl.ToString()}] {txt}");
        }

        public void WriteLine(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) File.AppendAllText(logFilePath, $"[{nvl.ToString()}] {txt}\n");
        }

        public async Task WriteLineAsync(string txt, LogsNivel? nivel)
        {
            var nvl = nivel ?? nivel_minimo;
            if (nvl >= nivel_minimo) await File.AppendAllTextAsync(logFilePath, $"[{nvl.ToString()}] {txt}\n");
        }
    }
}