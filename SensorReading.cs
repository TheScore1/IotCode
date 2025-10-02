namespace IotTgBot
{
    public class SensorReading
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public double TemperatureC { get; set; }
        public double HumidityPct { get; set; }
        public double PressureHpa { get; set; }
        public string? Source { get; set; } // например "esp32_1"
    }

}
