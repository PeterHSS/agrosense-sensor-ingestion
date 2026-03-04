namespace Api.Domain.Entities;

public class Sensor
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public double SoilMoisture { get; set; }
    public double Temperature { get; set; }
    public double Precipitation { get; set; }
    public DateTime Timestamp { get; set; }
}
