using FluentValidation;

namespace Api.Features.Ingestion;

public class IngestionValidator : AbstractValidator<IngestSensorDataRequest>
{
    public IngestionValidator()
    {
        RuleFor(x => x.PlotId)
            .NotEmpty().WithMessage("PlotId is required.");

        RuleFor(x => x.SoilMoisture)
            .InclusiveBetween(0, 100).WithMessage("SoilMoisture must be between 0 and 100.");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(-50, 50).WithMessage("Temperature must be between -50 and 50.");

        RuleFor(x => x.Precipitation)
            .InclusiveBetween(0, 500).WithMessage("Precipitation must be between 0 and 500.");
    }
}
