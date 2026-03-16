using Coworking.Application.Common.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Coworking.Infrastructure.Persistence.Transactions;

internal static class IsolationLevelMapper
{
    public static IsolationLevel ToSqlType(this TransactionIsolationLevel level) => level switch
    {
        TransactionIsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
        TransactionIsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
        TransactionIsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
        TransactionIsolationLevel.Snapshot => IsolationLevel.Snapshot,
        TransactionIsolationLevel.Serializable => IsolationLevel.Serializable,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}
