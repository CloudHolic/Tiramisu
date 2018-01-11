using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tiramisu.Util
{
    public static class DirectoryUtil
    {
        public static void CopyFolder(string sourceFolder, string destFolder, string[] exception = null)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            var files = Directory.GetFiles(sourceFolder);
            var folders = Directory.GetDirectories(sourceFolder);
            bool? overwrite = null;

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                Debug.Assert(name != null);
                var dest = Path.Combine(destFolder, name);

                if (exception != null && exception.Contains(Path.GetExtension(dest)))
                    continue;

                overwrite = FileCopy(file, dest, overwrite);
            }

            foreach (var folder in folders)
            {
                var name = Path.GetFileName(folder);
                Debug.Assert(name != null);
                var dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest, exception);
            }
        }

        private static bool? FileCopy(string sourceFile, string destFile, bool? overwrite = null)
        {
            if (File.Exists(destFile))
            {
                bool result;
                if (overwrite == null)
                    // Always overwrite.
                    result = true;
                else
                    result = (bool)overwrite;

                if (result)
                {
                    File.Delete(destFile);
                    File.Copy(sourceFile, destFile);
                }

                return result;
            }

            File.Copy(sourceFile, destFile);
            return overwrite;
        }
    }
}
