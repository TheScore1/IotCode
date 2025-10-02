using IotTgBot;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingRepository _repo;
    public ReadingsController(IReadingRepository repo) { _repo = repo; }

    public class PostDto
    {
        public double temperature { get; set; }
        public double humidity { get; set; }
        public double pressure { get; set; }
        public string? source { get; set; }
        public string? timestampUtc { get; set; } // optional ISO string
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PostDto dto, CancellationToken ct)
    {
        var ts = string.IsNullOrEmpty(dto.timestampUtc) ? DateTime.UtcNow : DateTime.Parse(dto.timestampUtc).ToUniversalTime();
        var r = new SensorReading
        {
            TimestampUtc = ts,
            TemperatureC = dto.temperature,
            HumidityPct = dto.humidity,
            PressureHpa = dto.pressure,
            Source = dto.source
        };
        await _repo.AddAsync(r, ct);
        return Ok(new { saved = true });
    }
}
