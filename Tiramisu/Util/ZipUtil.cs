using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Tiramisu.Util
{
    public static class ZipUtil
    {
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwrite = false)
        {
            using (var zipToOpen = new FileStream(sourceArchiveFileName, FileMode.Open))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    archive.InternalExtractToDirectory(destinationDirectoryName, overwrite);
                }
            }
        }

        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, bool overwrite = false)
        {
            if(overwrite)
                if (File.Exists(destinationArchiveFileName))
                    File.Delete(destinationArchiveFileName);

            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
        }

        private static void InternalExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            foreach (var file in archive.Entries)
            {
                var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                var directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory ?? throw new InvalidOperationException());

                if (file.Name != "")
                    file.ExtractToFile(completeFileName, true);
            }
        }
    }
}
