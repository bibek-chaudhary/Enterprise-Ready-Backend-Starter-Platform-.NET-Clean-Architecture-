using CleanArchApiGenerator.API.Config;
using CleanArchApiGenerator.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind options from appsettings.json or use defaults
var generatorOptions = builder.Configuration
    .GetSection("GeneratorOptions")
    .Get<GeneratorOptions>() ?? new GeneratorOptions();

// Register services - flat 3-layer, no interfaces needed for internal services
builder.Services.AddSingleton(generatorOptions);
builder.Services.AddScoped<DotnetCliService>();
builder.Services.AddScoped<ZipService>();
builder.Services.AddScoped<FileContentGenerator>();
builder.Services.AddScoped<ProjectScaffolder>();
builder.Services.AddScoped<ProjectFileWriter>();
builder.Services.AddScoped<ProjectGeneratorService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();