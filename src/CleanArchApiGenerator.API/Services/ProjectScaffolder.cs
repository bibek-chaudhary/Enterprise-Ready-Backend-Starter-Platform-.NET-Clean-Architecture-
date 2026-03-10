namespace CleanArchApiGenerator.API.Services
{
    /// <summary>
    /// Handles all dotnet CLI scaffolding: creating projects, adding references, installing packages.
    ///
    /// Parallelism rules:
    ///   - Commands targeting DIFFERENT projects/files → Task.WhenAll (safe)
    ///   - Commands targeting the SAME .csproj or .sln file → sequential (parallel corrupts the file)
    /// </summary>
    public class ProjectScaffolder
    {
        private readonly DotnetCliService _cli;

        public ProjectScaffolder(DotnetCliService cli)
        {
            _cli = cli;
        }

        public async Task CreateSolutionAndProjectsAsync(
            string projectRoot,
            string projectName,
            string targetFramework,
            IProgress<string> progress)
        {
            progress.Report("Creating solution...");
            await _cli.RunAsync(projectRoot, $"new sln -n {projectName}");

            // Each project gets its own folder/csproj, so parallel is safe here
            progress.Report("Creating projects...");
            await Task.WhenAll(
                _cli.RunAsync(projectRoot, $"new webapi -n {projectName}.API -f {targetFramework}"),
                _cli.RunAsync(projectRoot, $"new classlib -n {projectName}.Application -f {targetFramework}"),
                _cli.RunAsync(projectRoot, $"new classlib -n {projectName}.Domain -f {targetFramework}"),
                _cli.RunAsync(projectRoot, $"new classlib -n {projectName}.Infrastructure -f {targetFramework}")
            );

            DeleteDefaultClass1Files(projectRoot, projectName);
        }

        public async Task AddProjectsToSolutionAsync(string projectRoot, string projectName, IProgress<string> progress)
        {
            // Sequential: all sln add commands write to the SAME .sln file — parallel corrupts it
            progress.Report("Adding projects to solution...");
            await _cli.RunAsync(projectRoot, $"sln add {projectName}.API/{projectName}.API.csproj");
            await _cli.RunAsync(projectRoot, $"sln add {projectName}.Application/{projectName}.Application.csproj");
            await _cli.RunAsync(projectRoot, $"sln add {projectName}.Domain/{projectName}.Domain.csproj");
            await _cli.RunAsync(projectRoot, $"sln add {projectName}.Infrastructure/{projectName}.Infrastructure.csproj");
        }

        public async Task AddProjectReferencesAsync(string projectRoot, string projectName, IProgress<string> progress)
        {
            progress.Report("Wiring project references...");

            var apiPath = Path.Combine(projectRoot, $"{projectName}.API");
            var appPath = Path.Combine(projectRoot, $"{projectName}.Application");
            var infraPath = Path.Combine(projectRoot, $"{projectName}.Infrastructure");

            // Each group targets a different .csproj, so groups run in parallel.
            // Within each group, commands are sequential (same .csproj file).
            await Task.WhenAll(
                RunSequentialAsync(apiPath, new[]
                {
                $"add reference ../{projectName}.Application/{projectName}.Application.csproj",
                $"add reference ../{projectName}.Infrastructure/{projectName}.Infrastructure.csproj"
                }),
                RunSequentialAsync(infraPath, new[]
                {
                $"add reference ../{projectName}.Application/{projectName}.Application.csproj",
                $"add reference ../{projectName}.Domain/{projectName}.Domain.csproj"
                }),
                _cli.RunAsync(appPath,
                    $"add reference ../{projectName}.Domain/{projectName}.Domain.csproj")
            );
        }

        public async Task InstallNugetPackagesAsync(
            string projectRoot,
            string projectName,
            string efCoreVersion,
            string jwtBearerVersion,
            bool includeIdentity,
            bool includeJwt,
            bool includeSqlServer,
            IProgress<string> progress)
        {
            progress.Report("Installing NuGet packages...");

            var infraPath = Path.Combine(projectRoot, $"{projectName}.Infrastructure");
            var apiPath = Path.Combine(projectRoot, $"{projectName}.API");
            var appPath = Path.Combine(projectRoot, $"{projectName}.Application");
            var domainPath = Path.Combine(projectRoot, $"{projectName}.Domain");

            var infraPackages = new List<string>();
            var apiPackages = new List<string>();
            var appPackages = new List<string> { "add package FluentValidation" };
            var domainPackages = new List<string>();

            if (includeSqlServer)
            {
                infraPackages.AddRange(new[]
                {
                $"add package Microsoft.EntityFrameworkCore --version {efCoreVersion}",
                $"add package Microsoft.EntityFrameworkCore.SqlServer --version {efCoreVersion}",
                $"add package Microsoft.EntityFrameworkCore.Tools --version {efCoreVersion}",
            });
                apiPackages.Add($"add package Microsoft.EntityFrameworkCore.Design --version {efCoreVersion}");
            }

            if (includeIdentity)
            {
                infraPackages.Add($"add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version {efCoreVersion}");
                infraPackages.Add("add package Microsoft.AspNetCore.Identity");
                domainPackages.Add($"add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version {efCoreVersion}");
            }

            if (includeJwt)
                apiPackages.Add($"add package Microsoft.AspNetCore.Authentication.JwtBearer --version {jwtBearerVersion}");

            apiPackages.Add("add package FluentValidation.AspNetCore");

            // Each project's packages are sequential (same .csproj).
            // Different projects run in parallel (different .csproj files).
            await Task.WhenAll(
                RunSequentialAsync(infraPath, infraPackages),
                RunSequentialAsync(apiPath, apiPackages),
                RunSequentialAsync(appPath, appPackages),
                RunSequentialAsync(domainPath, domainPackages)
            );
        }

        public async Task RestoreAndBuildAsync(string projectRoot, IProgress<string> progress)
        {
            progress.Report("Restoring packages...");
            await _cli.RunAsync(projectRoot, "restore");

            progress.Report("Building solution...");
            await _cli.RunAsync(projectRoot, "build");
        }

        public async Task RunMigrationsAsync(string projectRoot, string projectName, IProgress<string> progress)
        {
            progress.Report("Running EF migrations...");
            await _cli.RunAsync(projectRoot,
                $"ef migrations add InitialCreate --project {projectName}.Infrastructure --startup-project {projectName}.API --output-dir Persistence/Migrations");

            progress.Report("Updating database...");
            await _cli.RunAsync(projectRoot,
                $"ef database update --project {projectName}.Infrastructure --startup-project {projectName}.API");
        }

        // -------------------------------------------------------------------------

        /// <summary>
        /// Runs CLI commands against the same working directory one at a time.
        /// Use whenever commands target the same .csproj or .sln file.
        /// </summary>
        private async Task RunSequentialAsync(string workingDirectory, IEnumerable<string> arguments)
        {
            foreach (var arg in arguments)
                await _cli.RunAsync(workingDirectory, arg);
        }

        private static void DeleteDefaultClass1Files(string projectRoot, string projectName)
        {
            foreach (var lib in new[] { "Domain", "Application", "Infrastructure" })
            {
                var path = Path.Combine(projectRoot, $"{projectName}.{lib}", "Class1.cs");
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

}
