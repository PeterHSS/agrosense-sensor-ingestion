using Api.Common;

namespace Api.Features.Ingestion;

public static class IngestionErrors
{
    public static Error Validation(IEnumerable<string> errors) => new("User.Validation", string.Join(';', errors));
}
