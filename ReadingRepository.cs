using IotTgBot;
using Microsoft.EntityFrameworkCore;

public class ReadingRepository : IReadingRepository
{
    private readonly IotDbContext _db;
    public ReadingRepository(IotDbContext db) { _db = db; }

    public async Task AddAsync(SensorReading r, CancellationToken ct = default)
    {
        _db.Readings.Add(r);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<SensorReading>> GetRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _db.Readings
            .AsNoTracking()
            .Where(x => x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(ct);
    }

    public async Task<SensorReading?> GetLatestAsync(CancellationToken ct = default)
    {
        return await _db.Readings.AsNoTracking().OrderByDescending(x => x.TimestampUtc).FirstOrDefaultAsync(ct);
    }
}
