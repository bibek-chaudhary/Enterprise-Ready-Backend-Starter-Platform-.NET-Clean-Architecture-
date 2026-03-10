namespace CleanArchApiGenerator.API.Config
{
    public class GeneratorOptions
    {
        public string BaseOutputPath { get; set; } = "GeneratedProjects";
        public string EfCoreVersion { get; set; } = "8.0.24";
        public string JwtBearerVersion { get; set; } = "8.0.24";
        public string TargetFramework { get; set; } = "net8.0";
        public string DefaultAdminEmail { get; set; } = "admin@example.com";
        public string DefaultAdminPassword { get; set; } = "Admin@123";
        public string DefaultJwtKey { get; set; } = "THIS_IS_A_SUPER_SECRET_KEY_CHANGE_IT";
        public string DefaultJwtIssuer { get; set; } = "CleanArchApi";
        public string DefaultJwtAudience { get; set; } = "CleanArchApiUsers";
        public int DefaultJwtExpiryMinutes { get; set; } = 60;
    }
}
