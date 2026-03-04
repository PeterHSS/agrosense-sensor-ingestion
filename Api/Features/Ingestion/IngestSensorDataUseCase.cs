using Api.Common;
using Api.Domain.Abstractions.Infrastructure.Messaging;
using Api.Domain.Abstractions.UseCases;
using Api.Domain.Entities;
using Api.Domain.Events;
using Api.Infrastructure.Persistence.Contexts;
using FluentValidation;

namespace Api.Features.Ingestion;

public class IngestSensorDataUseCase(
    IValidator<IngestSensorDataRequest> validator,
    SensorDbContext dbContext,
    ILogger<IngestSensorDataUseCase> logger,
    IMessagePublisher publisher) : IUseCase<IngestSensorDataRequest>
{
    private const string RoutingKey = "sensor.data.ingested";

    public async Task<Result> Handle(IngestSensorDataRequest request)
    {
        var validationResult = validator.Validate(request);

        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for IngestSensorDataRequest: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            return Result.Failure(IngestionErrors.Validation(errors));
        }

        var sensorData = new Sensor
        {
            Id = Guid.NewGuid(),
            PlotId = request.PlotId,
            SoilMoisture = request.SoilMoisture,
            Temperature = request.Temperature,
            Precipitation = request.Precipitation,
            Timestamp = DateTime.UtcNow
        };

        dbContext.Sensors.Add(sensorData);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Sensor data ingested for PlotId: {PlotId}, SensorDataId: {SensorDataId}", sensorData.PlotId, sensorData.Id);

        await publisher.PublishAsync(new SensorDataIngestedEvent(sensorData.Id, sensorData.PlotId, sensorData.SoilMoisture, sensorData.Temperature, sensorData.Precipitation, sensorData.Timestamp), RoutingKey);

        logger.LogInformation("Published SensorDataIngestedEvent for SensorDataId: {SensorDataId}", sensorData.Id);

        return Result.Success() ;
    }
}
