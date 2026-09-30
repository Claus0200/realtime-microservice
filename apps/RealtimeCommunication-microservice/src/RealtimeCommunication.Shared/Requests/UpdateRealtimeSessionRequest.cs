using System.ComponentModel.DataAnnotations;
using RealtimeCommunication.Shared.Models;

namespace RealtimeCommunication.Shared.Requests;

public sealed record UpdateRealtimeSessionRequest : IValidatableObject
{
    public RealtimeSessionStatus Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Status))
        {
            yield return new ValidationResult(
                "Status is not valid.",
                new[] { nameof(Status) });
        }
    }
}
