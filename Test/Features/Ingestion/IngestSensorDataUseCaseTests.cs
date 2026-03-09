using Api.Domain.Abstractions.Infrastructure.Messaging;
using Api.Domain.Events;
using Api.Features.Ingestion;
using Api.Infrastructure.Persistence.Contexts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Features.Ingestion;

public class IngestSensorDataUseCaseTests
{
    private readonly SensorDbContext _context;
    private readonly Mock<IValidator<IngestSensorDataRequest>> _validatorMock;
    private readonly Mock<ILogger<IngestSensorDataUseCase>> _loggerMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly IngestSensorDataUseCase _useCase;

    public IngestSensorDataUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<SensorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SensorDbContext(options);
        _validatorMock = new Mock<IValidator<IngestSensorDataRequest>>();
        _loggerMock = new Mock<ILogger<IngestSensorDataUseCase>>();
        _publisherMock = new Mock<IMessagePublisher>();

        _useCase = new IngestSensorDataUseCase(_validatorMock.Object, _context, _loggerMock.Object, _publisherMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private void SetupValidRequest() =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<IngestSensorDataRequest>()))
            .Returns(new ValidationResult());

    private void SetupInvalidRequest(params string[] errors) =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<IngestSensorDataRequest>()))
            .Returns(new ValidationResult(errors.Select(e => new ValidationFailure("Field", e))));

    private static IngestSensorDataRequest BuildRequest(Guid? plotId = null) => new(
        PlotId: plotId ?? Guid.NewGuid(),
        SoilMoisture: 45.5,
        Temperature: 28.3,
        Precipitation: 12.0
    );

    // ─── Validation failures ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailureWithErrors()
    {
        // Arrange
        SetupInvalidRequest("SoilMoisture is required", "Temperature out of range");

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(IngestionErrors.Validation(["SoilMoisture is required", "Temperature out of range"]).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotPersistSensorData()
    {
        // Arrange
        SetupInvalidRequest("SoilMoisture is required");

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Empty(_context.Sensors);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotPublishEvent()
    {
        // Arrange
        SetupInvalidRequest("SoilMoisture is required");

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<SensorDataIngestedEvent>(), It.IsAny<string>()), Times.Never);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEverythingValid_ReturnsSuccess()
    {
        // Arrange
        SetupValidRequest();

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_PersistsSensorData()
    {
        // Arrange
        SetupValidRequest();

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Single(_context.Sensors);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_PersistsSensorDataWithCorrectFields()
    {
        // Arrange
        SetupValidRequest();
        var plotId = Guid.NewGuid();
        var request = BuildRequest(plotId);
        var before = DateTime.UtcNow;

        // Act
        await _useCase.Handle(request);

        // Assert
        var after = DateTime.UtcNow;
        var sensor = _context.Sensors.Single();
        Assert.Equal(plotId, sensor.PlotId);
        Assert.Equal(request.SoilMoisture, sensor.SoilMoisture);
        Assert.Equal(request.Temperature, sensor.Temperature);
        Assert.Equal(request.Precipitation, sensor.Precipitation);
        Assert.NotEqual(Guid.Empty, sensor.Id);
        Assert.InRange(sensor.Timestamp, before, after);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_PublishesEventWithCorrectRoutingKey()
    {
        // Arrange
        SetupValidRequest();

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        _publisherMock.Verify(
            p => p.PublishAsync(It.IsAny<SensorDataIngestedEvent>(), "sensor.data.ingested"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_PublishesEventWithCorrectData()
    {
        // Arrange
        SetupValidRequest();
        var plotId = Guid.NewGuid();
        var request = BuildRequest(plotId);

        // Act
        await _useCase.Handle(request);

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            It.Is<SensorDataIngestedEvent>(e =>
                e.PlotId == plotId &&
                e.SoilMoisture == request.SoilMoisture &&
                e.Temperature == request.Temperature &&
                e.Precipitation == request.Precipitation),
            It.IsAny<string>()),
        Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_PublishesAfterPersisting()
    {
        // Arrange
        SetupValidRequest();
        var persistedBeforePublish = false;

        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<SensorDataIngestedEvent>(), It.IsAny<string>()))
            .Callback(() => persistedBeforePublish = _context.Sensors.Any())
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.True(persistedBeforePublish);
    }
}