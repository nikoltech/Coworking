using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Common;

public static class ConcurrencyTokenMapping
{
    public const string PropertyName = "xmin";

    public static EntityTypeBuilder<TEntity> HasStoreConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<uint>(PropertyName)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        return builder;
    }
}
