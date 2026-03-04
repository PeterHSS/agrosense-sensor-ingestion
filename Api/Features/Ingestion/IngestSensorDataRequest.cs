namespace Api.Features.Ingestion;

public record IngestSensorDataRequest(Guid PlotId, double SoilMoisture, double Temperature, double Precipitation);
