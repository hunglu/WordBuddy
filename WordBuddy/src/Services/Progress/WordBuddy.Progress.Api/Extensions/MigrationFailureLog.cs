using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace WordBuddy.Progress.Api.Extensions;

/// <summary>
/// Logs a failed <c>migrate</c> run with enough detail to diagnose it, without leaking secrets.
/// </summary>
/// <remarks>
/// The exception object is never passed to the logger, because its <c>Message</c> can echo part of the
/// connection string (for example an <see cref="ArgumentException"/> from a malformed connection string).
/// What is logged instead: the exception types, the stack traces, and, only for provider
/// <see cref="DbException"/>s, the provider message and error code. Npgsql and SqlClient server messages
/// hold the user name, database or host but never the password. Any other exception type keeps its message withheld.
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
        string providerMessage = dbException?.Message ?? "(withheld: not a database provider error)";

        logger.LogError(
            "{Service} migration failed: {ExceptionType}, provider error code {ProviderErrorCode}, provider message {ProviderMessage}{NewLine}{StackTrace}",
            service,
            exception.GetType().Name,
            errorCode,
            providerMessage,
            Environment.NewLine,
            StackTraces(exception));
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
