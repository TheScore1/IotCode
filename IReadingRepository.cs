namespace IotTgBot
{
    public interface IReadingRepository
    {
        Task AddAsync(SensorReading r, CancellationToken ct = default);
        Task<List<SensorReading>> GetRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
        Task<SensorReading?> GetLatestAsync(CancellationToken ct = default);
    }

}
