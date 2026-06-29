using System.ComponentModel.DataAnnotations;

namespace CommandTrack.Shared.Units;

public sealed record CreateUnitRequest(
    [Required]
    [StringLength(50, MinimumLength = 2)]
    string CallSign,

    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Type);