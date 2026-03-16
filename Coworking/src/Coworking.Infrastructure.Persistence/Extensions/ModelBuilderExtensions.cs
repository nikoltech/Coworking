using Coworking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyGlobalConfiguration(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var type = entityType.ClrType;

            if (typeof(ITrackEntity).IsAssignableFrom(type))
            {
                modelBuilder.ConfigureTrackEntity(type);
            }
        }
    }

    private static void ConfigureTrackEntity(this ModelBuilder modelBuilder, Type entityType)
    {
        modelBuilder.Entity(entityType)
            .Property(nameof(ITrackEntity.CreatedAt))
            .IsRequired();

        modelBuilder.Entity(entityType).HasIndex(nameof(ITrackEntity.CreatedAt));
        //modelBuilder.Entity(entityType).HasIndex(nameof(ITrackEntity.UpdatedAt));
    }
}