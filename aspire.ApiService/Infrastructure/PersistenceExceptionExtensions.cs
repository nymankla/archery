using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace aspire.ApiService.Infrastructure;

public sealed class ConflictException(string message) : Exception(message);

public static class PersistenceExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex, string? constraintName = null)
        => TryMatch(ex, PostgresErrorCodes.UniqueViolation, constraintName);

    public static bool IsCheckConstraintViolation(this DbUpdateException ex, string? constraintName = null)
        => TryMatch(ex, PostgresErrorCodes.CheckViolation, constraintName);

    private static bool TryMatch(DbUpdateException ex, string sqlState, string? constraintName)
    {
        var pg = ex.InnerException as PostgresException ?? ex.GetBaseException() as PostgresException;
        if (pg is null || pg.SqlState != sqlState)
            return false;

        return constraintName is null
            || string.Equals(pg.ConstraintName, constraintName, StringComparison.Ordinal);
    }
}
