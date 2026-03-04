namespace Api.Domain.Events;

public record SensorDataIngestedEvent(Guid id, Guid plotId, double soilMoisture, double temperature, double precipitation, DateTime timestamp);
