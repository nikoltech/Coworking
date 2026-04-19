using Coworking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Desks;

public class DeskConfiguration : IEntityTypeConfiguration<Desk>
{
    public void Configure(EntityTypeBuilder<Desk> builder)
    {
        builder.ToTable("Desks");

        builder.HasKey(x => x.Id);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();
        //.IsConcurrencyToken();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasMany(x => x.Bookings)
            .WithOne(c => c.Desk)
            .HasForeignKey(x => x.DeskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}