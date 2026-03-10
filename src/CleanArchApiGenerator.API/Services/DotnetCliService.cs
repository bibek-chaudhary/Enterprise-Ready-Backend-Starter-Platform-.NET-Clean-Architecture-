using System.Diagnostics;
using System.Text;

namespace CleanArchApiGenerator.API.Services
{
    public class DotnetCliService
    {
        public async Task RunAsync(string workingDirectory, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data != null) outputBuilder.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null) errorBuilder.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception(
                    $"dotnet CLI failed.\nArguments: {arguments}\nError:\n{errorBuilder}\nOutput:\n{outputBuilder}");
        }

        public async Task RunManyAsync(string workingDirectory, IEnumerable<string> arguments)
        {
            var tasks = arguments.Select(arg => RunAsync(workingDirectory, arg));
            await Task.WhenAll(tasks);
        }
    }
}
