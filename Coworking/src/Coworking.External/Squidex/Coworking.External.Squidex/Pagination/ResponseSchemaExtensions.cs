using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Pagination;

internal static class ResponseSchemaExtensions
{
    public static bool MayHaveMore<T>(this ResponseSchema<T> page, int pageSize) =>
        page.Items.Count >= pageSize;
}
