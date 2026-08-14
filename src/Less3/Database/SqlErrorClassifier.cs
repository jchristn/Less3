namespace Less3.Database
{
    using System;

    /// <summary>
    /// Classifies database provider exceptions into dialect-agnostic categories so callers can react
    /// to a condition such as a unique-constraint violation without taking a compile-time dependency
    /// on a specific provider's exception type. Matching is performed against the exception chain
    /// (including any wrapping <see cref="AggregateException"/> raised by synchronous waits).
    /// Thread-safe; the class is stateless.
    /// </summary>
    public static class SqlErrorClassifier
    {
        /// <summary>
        /// Determine whether an exception, or any exception it wraps, represents a unique-constraint
        /// or duplicate-key violation. Recognizes the wording used by SQLite, PostgreSQL, MySQL, and
        /// SQL Server. The exception chain is walked to a bounded depth to guard against cycles.
        /// </summary>
        /// <param name="exception">Exception to inspect. May be an <see cref="AggregateException"/> or otherwise wrap inner exceptions.</param>
        /// <returns>True if the exception chain indicates a unique-constraint violation; otherwise false.</returns>
        public static bool IsUniqueConstraintViolation(Exception exception)
        {
            Exception current = exception;
            int depth = 0;

            while (current != null && depth < 16)
            {
                if (current is AggregateException aggregate)
                {
                    foreach (Exception inner in aggregate.InnerExceptions)
                    {
                        if (IsUniqueConstraintViolation(inner)) return true;
                    }
                }

                string message = current.Message;

                if (!String.IsNullOrEmpty(message))
                {
                    // SQLite:     "SQLite Error 19: 'UNIQUE constraint failed: ...'"
                    // PostgreSQL: "duplicate key value violates unique constraint ..."
                    // MySQL:      "Duplicate entry '...' for key '...'"
                    // SQL Server: "Cannot insert duplicate key row ..." / "Violation of UNIQUE KEY constraint ..."
                    if (message.IndexOf("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) >= 0
                        || message.IndexOf("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) >= 0
                        || message.IndexOf("Duplicate entry", StringComparison.OrdinalIgnoreCase) >= 0
                        || message.IndexOf("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase) >= 0
                        || message.IndexOf("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                current = current.InnerException;
                depth++;
            }

            return false;
        }
    }
}
