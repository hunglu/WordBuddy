using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace WordBuddy.Identity.Api.Extensions;

/// <summary>
/// Logs a failed <c>migrate</c> run with enough detail to diagnose it, without leaking secrets.
/// </summary>
/// <remarks>
/// The exception object is never passed to the logger, because its <c>Message</c> can echo part of the
/// connection string (for example an <see cref="ArgumentException"/> from a malformed connection string).
/// The provider message is withheld too: server text such as <c>Message</c>, <c>MessageText</c>, <c>Detail</c>,
/// <c>Hint</c> or <c>Where</c> can hold row values (for example the duplicate key value, an email address).
/// What is logged instead: the exception types, the stack traces, and, only for provider
/// <see cref="DbException"/>s, an allowlist of data-free fields (error code, severity, schema names).
/// Diagnose with the documented error code (for example 18456, 4060 or 28P01).
/// </remarks>
public static class MigrationFailureLog
{
    /// <summary>Writes one Error entry for a failed migration.</summary>
    /// <param name="logger">Logger (message template only).</param>
    /// <param name="service">Service name for the message.</param>
    /// <param name="exception">The failure.</param>
    public static void Write(ILogger logger, string service, Exception exception)
    {
        DbException? dbException = Find(exception);
        string errorCode = dbException switch
        {
            PostgresException postgres => postgres.SqlState,
            SqlException sql => sql.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "n/a",
        };
        string providerDetails = Details(dbException);

        logger.LogError(
            "{Service} migration failed: {ExceptionType}, provider error code {ProviderErrorCode}, provider details {ProviderDetails}{NewLine}{StackTrace}",
            service,
            exception.GetType().Name,
            errorCode,
            providerDetails,
            Environment.NewLine,
            StackTraces(exception));
    }

    private static string Details(DbException? dbException)
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
        return dbException switch
        {
            PostgresException postgres => string.Create(
                culture,
                $"severity={postgres.Severity}, schema={postgres.SchemaName}, table={postgres.TableName}, column={postgres.ColumnName}, constraint={postgres.ConstraintName}"),
            SqlException sql => string.Create(
                culture,
                $"class={sql.Class}, state={sql.State}, procedure={sql.Procedure}, line={sql.LineNumber}"),
            DbException other => string.Create(culture, $"error code={other.ErrorCode}"),
            _ => "n/a",
        };
    }

    private static DbException? Find(Exception? exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException dbException)
            {
                return dbException;
            }
        }

        return null;
    }

    private static string StackTraces(Exception exception)
    {
        StringBuilder builder = new();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            builder.Append(current.GetType().FullName).AppendLine();
            builder.AppendLine(current.StackTrace);
        }

        return builder.ToString();
    }
}
