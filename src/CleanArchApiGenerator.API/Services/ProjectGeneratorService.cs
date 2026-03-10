using CleanArchApiGenerator.API.Config;
using CleanArchApiGenerator.API.Models;

namespace CleanArchApiGenerator.API.Services
{
    /// <summary>
    /// Thin orchestrator: delegates all work to focused services.
    /// Accepts IProgress&lt;string&gt; for real-time step reporting.
    /// </summary>
    public class ProjectGeneratorService
    {
        private readonly ProjectScaffolder _scaffolder;
        private readonly ProjectFileWriter _fileWriter;
        private readonly ZipService _zipService;
        private readonly GeneratorOptions _opts;

        public ProjectGeneratorService(
            ProjectScaffolder scaffolder,
            ProjectFileWriter fileWriter,
            ZipService zipService,
            GeneratorOptions opts)
        {
            _scaffolder = scaffolder;
            _fileWriter = fileWriter;
            _zipService = zipService;
            _opts = opts;
        }

        public async Task<string> GenerateAsync(
            GeneratorConfiguration config,
            IProgress<string>? progress = null)
        {
            progress ??= new Progress<string>(_ => { }); // no-op if caller doesn't care

            var basePath = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                _opts.BaseOutputPath);

            Directory.CreateDirectory(basePath);

            var projectRoot = Path.Combine(basePath, config.ProjectName);

            if (Directory.Exists(projectRoot))
                Directory.Delete(projectRoot, true);

            Directory.CreateDirectory(projectRoot);

            await _scaffolder.CreateSolutionAndProjectsAsync(projectRoot, config.ProjectName, _opts.TargetFramework, progress);
            await _scaffolder.AddProjectsToSolutionAsync(projectRoot, config.ProjectName, progress);
            await _scaffolder.AddProjectReferencesAsync(projectRoot, config.ProjectName, progress);
            await _scaffolder.InstallNugetPackagesAsync(
                projectRoot, config.ProjectName,
                _opts.EfCoreVersion, _opts.JwtBearerVersion,
                config.IncludeIdentity, config.IncludeJwt, config.IncludeSqlServer,
                progress);

            progress.Report("Writing source files...");
            _fileWriter.WriteAllFiles(projectRoot, config);

            await _scaffolder.RestoreAndBuildAsync(projectRoot, progress);
            await _scaffolder.RunMigrationsAsync(projectRoot, config.ProjectName, progress);

            progress.Report("Creating zip...");
            return _zipService.CreateZip(projectRoot, config.ProjectName);
        }
    }
}
