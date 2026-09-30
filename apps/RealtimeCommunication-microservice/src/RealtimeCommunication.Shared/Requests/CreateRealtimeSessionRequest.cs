using System.ComponentModel.DataAnnotations;

namespace RealtimeCommunication.Shared.Requests;

public sealed record CreateRealtimeSessionRequest : IValidatableObject
{
    public Guid ChannelId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChannelId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ChannelId must be a non-empty GUID.",
                new[] { nameof(ChannelId) });
        }
    }
}
