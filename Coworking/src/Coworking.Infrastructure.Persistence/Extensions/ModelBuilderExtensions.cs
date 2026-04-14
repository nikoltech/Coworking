using System.Linq.Expressions;
using Coworking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Invocation order is important. This method should be called before all the configurations are applied.
    /// </summary>
    public static void ApplyGlobalConfiguration(this ModelBuilder modelBuilder)
    {
        // Получаем типы один раз, чтобы код читался как простое предложение
        var entityTypes = modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType);

        foreach (var type in entityTypes)
        {
            if (typeof(ITrackEntity).IsAssignableFrom(type))
            {
                modelBuilder.ConfigureTrackEntity(type);
            }

            if (typeof(ICanBeDisabled).IsAssignableFrom(type))
            {
                modelBuilder.ConfigureDisabledQueryFilter(type);
            }
        }
    }

    private static void ConfigureTrackEntity(this ModelBuilder modelBuilder, Type entityType)
    {
        modelBuilder.Entity(entityType)
            .Property(nameof(ITrackEntity.CreatedAt))
            .IsRequired();
    }

    private static void ConfigureDisabledQueryFilter(this ModelBuilder modelBuilder, Type entityType)
    {
        // Safe way to create a query filter using expression trees
        // Creation an expression like: e => e.DisabledAt == null
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ICanBeDisabled.DisabledAt));

        var propertyType = property.Type;
        var nullValue = Expression.Constant(null, propertyType);

        var filterExpression = Expression.Lambda(Expression.Equal(property, nullValue), parameter);

        modelBuilder.Entity(entityType).HasQueryFilter(filterExpression);
    }
}