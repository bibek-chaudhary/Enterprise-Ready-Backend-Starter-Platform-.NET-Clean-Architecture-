namespace CleanArchApiGenerator.API.Models
{
    public class GeneratorConfiguration
    {
        public string ProjectName { get; set; } = string.Empty;
        public bool IncludeIdentity { get; set; } = true;
        public bool IncludeJwt { get; set; } = true;
        public bool IncludeSqlServer { get; set; } = true;
    }
}
