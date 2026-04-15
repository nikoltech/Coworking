using Coworking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Common;

public static class SlotSizeEfMapping
{
    public static void Map<T>(OwnedNavigationBuilder<T, SlotSize> slot)
        where T : class
    {
        slot.Property(x => x.Minutes);
    }
}
