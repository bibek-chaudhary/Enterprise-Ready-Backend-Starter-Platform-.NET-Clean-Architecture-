using CleanArchApiGenerator.API.Models;

namespace CleanArchApiGenerator.API.Services
{
    /// <summary>
    /// Responsible only for writing generated file content to disk.
    /// All path construction and Directory.CreateDirectory calls live here.
    /// </summary>
    public class ProjectFileWriter
    {
        private readonly FileContentGenerator _content;

        public ProjectFileWriter(FileContentGenerator content)
        {
            _content = content;
        }

        public void WriteAllFiles(string projectRoot, GeneratorConfiguration config)
        {
            var n = config.ProjectName;

            WriteApiFiles(projectRoot, n);
            WriteApplicationFiles(projectRoot, n);
            WriteDomainFiles(projectRoot, n, config.IncludeIdentity);
            WriteInfrastructureFiles(projectRoot, n, config.IncludeIdentity, config.IncludeJwt);
        }

        // -------------------------------------------------------------------------

        private void WriteApiFiles(string projectRoot, string n)
        {
            var apiPath = Path.Combine(projectRoot, $"{n}.API");
            var controllerFolder = Path.Combine(apiPath, "Controllers");
            var middlewareFolder = Path.Combine(apiPath, "Middleware");

            Directory.CreateDirectory(controllerFolder);
            Directory.CreateDirectory(middlewareFolder);

            // Remove default weather forecast files
            TryDelete(Path.Combine(apiPath, "WeatherForecast.cs"));
            TryDelete(Path.Combine(controllerFolder, "WeatherForecastController.cs"));

            Write(Path.Combine(apiPath, "Program.cs"), _content.ProgramCs(n));
            Write(Path.Combine(controllerFolder, "BaseApiController.cs"), _content.BaseApiController(n));
            Write(Path.Combine(controllerFolder, "AuthController.cs"), _content.AuthController(n));
            Write(Path.Combine(apiPath, "appsettings.json"), _content.AppSettings(n));
            Write(Path.Combine(middlewareFolder, "ExceptionMiddleware.cs"), _content.ExceptionMiddleware(n));
            Write(Path.Combine(middlewareFolder, "MiddlewareExtensions.cs"), _content.MiddlewareExtensions(n));
        }

        private void WriteApplicationFiles(string projectRoot, string n)
        {
            var resultsFolder = Path.Combine(projectRoot, $"{n}.Application", "Common", "Results");
            var dtosFolder = Path.Combine(projectRoot, $"{n}.Application", "DTOs", "Auth");
            var validatorsFolder = Path.Combine(projectRoot, $"{n}.Application", "Validators");

            Directory.CreateDirectory(resultsFolder);
            Directory.CreateDirectory(dtosFolder);
            Directory.CreateDirectory(validatorsFolder);

            Write(Path.Combine(resultsFolder, "Result.cs"), _content.ResultClass(n));
            Write(Path.Combine(dtosFolder, "LoginRequest.cs"), _content.LoginRequest(n));
            Write(Path.Combine(dtosFolder, "RegisterRequest.cs"), _content.RegisterRequest(n));
            Write(Path.Combine(validatorsFolder, "LoginRequestValidator.cs"), _content.LoginRequestValidator(n));
            Write(Path.Combine(validatorsFolder, "RegisterRequestValidator.cs"), _content.RegisterRequestValidator(n));
        }

        private void WriteDomainFiles(string projectRoot, string n, bool includeIdentity)
        {
            if (!includeIdentity) return;

            var entitiesFolder = Path.Combine(projectRoot, $"{n}.Domain", "Entities");
            Directory.CreateDirectory(entitiesFolder);
            Write(Path.Combine(entitiesFolder, "ApplicationUser.cs"), _content.ApplicationUser(n));
        }

        private void WriteInfrastructureFiles(string projectRoot, string n, bool includeIdentity, bool includeJwt)
        {
            var infraPath = Path.Combine(projectRoot, $"{n}.Infrastructure");
            var persistenceFolder = Path.Combine(infraPath, "Persistence");
            var identityFolder = Path.Combine(infraPath, "Identity");

            Directory.CreateDirectory(persistenceFolder);

            Write(Path.Combine(infraPath, "DependencyInjection.cs"), _content.DependencyInjection(n));
            Write(Path.Combine(persistenceFolder, "ApplicationDbContext.cs"), _content.DbContext(n));

            if (includeIdentity || includeJwt)
            {
                Directory.CreateDirectory(identityFolder);
                Write(Path.Combine(identityFolder, "DbSeeder.cs"), _content.DbSeeder(n));
            }

            if (includeJwt)
            {
                Write(Path.Combine(identityFolder, "JwtTokenService.cs"), _content.JwtTokenService(n));
            }
        }

        // -------------------------------------------------------------------------

        private static void Write(string path, string content) =>
            File.WriteAllText(path, content.TrimStart());

        private static void TryDelete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
