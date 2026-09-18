using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Coworking.Infrastructure.Persistence.Configurations.Common;

public class UUIDv7ValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();

    public override bool GeneratesTemporaryValues => false;
}
