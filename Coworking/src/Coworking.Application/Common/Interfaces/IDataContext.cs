using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Interfaces.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Common.Interfaces;

public interface IDataContext
{
    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<ITransaction> BeginTransactionAsync(TransactionIsolationLevel isolationLevel, CancellationToken ct = default);
}
