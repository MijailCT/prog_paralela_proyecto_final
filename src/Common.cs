namespace ProyectoFinalProgramacionParalela
{
    public static class Common
    {
        public static IEnumerable<string> EnumerarArchivos(string directorioActual, string patron, SearchOption opcionesBusqueda = SearchOption.TopDirectoryOnly, Logs? logObjeto = null)
        {
            //Enumeramos el directorio actual, pero agarramos errores para que no explote todo
            IEnumerable<string>? archivos = null;
            try
            {
                archivos = Directory.EnumerateFiles(directorioActual, patron, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                logObjeto?.WriteLine($"[EnumerarArchivos] No se pudo acceder al directorio {directorioActual}!", LogsNivel.ERROR);
            }
            catch (Exception ex)
            {
                logObjeto?.WriteLine($"[EnumerarArchivos] No se pudo acceder al directorio {directorioActual} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
            }

            //Si fallamos con enumerar los archivos, nos retiramos y el iterador termina aqui
            if (archivos == null) yield break;

            foreach (var archivo in archivos)
            {
                yield return archivo;
            }

            //Si realmente queremos rebuscar en todo entonces a enumerar los demas directorios
            if (opcionesBusqueda == SearchOption.AllDirectories)
            {
                IEnumerable<string>? subdirectorios = null;
                //Por si acaso, uno nunca sabe...
                try
                {
                    subdirectorios = Directory.EnumerateDirectories(directorioActual);
                }
                catch (UnauthorizedAccessException)
                {
                    logObjeto?.WriteLine($"[EnumerarArchivos] No se pudo leer los subdirectorios de {directorioActual}!", LogsNivel.ERROR);
                }
                catch (Exception ex)
                {
                    logObjeto?.WriteLine($"[EnumerarArchivos] No se pudo leer los subdirectorios de {directorioActual} por el siguiente error: {ex.Message}", LogsNivel.ERROR);
                }

                if (subdirectorios == null) yield break;

                foreach (var subdirectorio in subdirectorios)
                {
                    foreach (var archivo in EnumerarArchivos(subdirectorio, patron, opcionesBusqueda, logObjeto))
                    {
                        yield return archivo;
                    }
                }
            }
        }
    }
}