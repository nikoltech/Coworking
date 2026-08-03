namespace Coworking.Application.Common.Enums;

public enum TransactionIsolationLevel
{
    ReadCommitted,
    ReadUncommitted,
    RepeatableRead,
    Snapshot,


    Serializable
}
