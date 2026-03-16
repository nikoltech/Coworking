using System;
using Coworking.Domain.Common;

namespace Coworking.Domain.Entities;

public class Desk : ITrackEntity, ICanBeDisabled
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DisabledAt { get; set; }
}
