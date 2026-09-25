using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Extensions;

public static class MassTransitModelExtensions
{
    public static void AddMassTransitModel(this ModelBuilder modelBuilder)
    {
        //modelBuilder.AddTransactionalOutboxEntities();

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
