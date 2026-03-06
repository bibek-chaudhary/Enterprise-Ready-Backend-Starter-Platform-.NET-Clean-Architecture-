namespace Generator.Core;

public class GeneratorContext
{
    public GeneratorConfiguration Configuration { get; }

    public string RootPath { get; }

    public string SolutionPath => Path.Combine(RootPath, Configuration.ProjectName);

    public GeneratorContext(GeneratorConfiguration configuration, string rootPath)
    {
        Configuration = configuration;
        RootPath = rootPath;
    }
}