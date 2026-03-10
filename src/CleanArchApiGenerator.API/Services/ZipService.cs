using System.IO.Compression;

namespace CleanArchApiGenerator.API.Services
{
    public class ZipService
    {
        public string CreateZip(string sourcePath, string projectName)
        {
            var basePath = Path.GetDirectoryName(sourcePath)!;
            var zipPath = Path.Combine(basePath, $"{projectName}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(sourcePath, zipPath);

            return zipPath;
        }
    }
}
