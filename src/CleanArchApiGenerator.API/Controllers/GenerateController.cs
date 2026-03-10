using CleanArchApiGenerator.API.Models;
using CleanArchApiGenerator.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchApiGenerator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateController : ControllerBase
    {
        private readonly ProjectGeneratorService _generatorService;
        private readonly ILogger<GenerateController> _logger;

        public GenerateController(
            ProjectGeneratorService generatorService,
            ILogger<GenerateController> logger)
        {
            _generatorService = generatorService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] GeneratorConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.ProjectName))
                return BadRequest("Project name is required.");

            var steps = new List<string>();
            var progress = new Progress<string>(step =>
            {
                _logger.LogInformation("[Generator] {Step}", step);
                steps.Add(step);
            });

            try
            {
                var zipPath = await _generatorService.GenerateAsync(config, progress);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
                return File(fileBytes, "application/zip", $"{config.ProjectName}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project generation failed for '{ProjectName}'.", config.ProjectName);
                return StatusCode(500, $"An error occurred while generating the project: {ex.Message}");
            }
        }
    }
}
