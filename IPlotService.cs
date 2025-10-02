namespace IotTgBot
{
    public interface IPlotService
    {
        Task<byte[]> PlotMetricAsync(List<SensorReading> data, string title, string metric, CancellationToken ct = default);
        Task<byte[]> PlotCombinedAsync(List<SensorReading> data, string title, CancellationToken ct = default);
    }
}
