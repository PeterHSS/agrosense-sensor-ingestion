using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Ingestion;

[ApiController]
[Route("api/sensor-ingestion")]
public class IngestionController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.UserOnly)]
    public async Task<IResult> Handle([FromServices] IUseCase<IngestSensorDataRequest> useCase, [FromBody] IngestSensorDataRequest request)
    {
        var response = await useCase.Handle(request);

        if (response.IsFailure)
            return Results.BadRequest(response.Error);

        return Results.Created();
    }
}
