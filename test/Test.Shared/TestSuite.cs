namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstract base class for test suites. Provides test execution, timing, and assertion helpers.
    /// </summary>
    public abstract class TestSuite
    {
        #region Private-Members

        private List<TestResult> _Results = new List<TestResult>();

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public abstract string Name { get; }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all tests in this suite and returns the collected results.
        /// </summary>
        /// <returns>A list of <see cref="TestResult"/> instances for each test executed.</returns>
        public async Task<List<TestResult>> RunAsync()
        {
            _Results = new List<TestResult>();
            await RunTestsAsync().ConfigureAwait(false);
            return _Results;
        }

        /// <summary>
        /// Override this method to define and run the individual tests in this suite.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task RunTestsAsync();

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Executes a single asynchronous test with timing and error handling.
        /// </summary>
        /// <param name="name">The name of the test.</param>
        /// <param name="action">The asynchronous test action to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task RunTest(string name, Func<Task> action)
        {
            TestResult result = new TestResult(name);
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                await action().ConfigureAwait(false);
                stopwatch.Stop();
                result.MarkPassed(stopwatch.ElapsedMilliseconds);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  PASS ");
                Console.ResetColor();
                Console.WriteLine($"{name} ({stopwatch.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.MarkFailed(stopwatch.ElapsedMilliseconds, ex.Message, ex);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  FAIL ");
                Console.ResetColor();
                Console.WriteLine($"{name} ({stopwatch.ElapsedMilliseconds}ms)");
                Console.WriteLine($"       {ex.Message}");
            }

            _Results.Add(result);
        }

        /// <summary>
        /// Executes a single synchronous test with timing and error handling.
        /// </summary>
        /// <param name="name">The name of the test.</param>
        /// <param name="action">The synchronous test action to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task RunTest(string name, Action action)
        {
            await RunTest(name, () =>
            {
                action();
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        #endregion

        #region Assert-Methods

        /// <summary>
        /// Asserts that the specified condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">The failure message if the condition is false.</param>
        protected void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Asserts that two values are equal using the default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the values being compared.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertEqual<T>(T expected, T actual, string? message = null)
        {
            bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
            if (!equal)
            {
                string failMessage = message
                    ?? $"Expected [{expected}] but got [{actual}]";
                throw new Exception(failMessage);
            }
        }

        /// <summary>
        /// Asserts that two values are not equal using the default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the values being compared.</typeparam>
        /// <param name="expected">The value that should not match.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertNotEqual<T>(T expected, T actual, string? message = null)
        {
            bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
            if (equal)
            {
                string failMessage = message
                    ?? $"Expected values to differ but both were [{actual}]";
                throw new Exception(failMessage);
            }
        }

        /// <summary>
        /// Asserts that the specified value is not null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertNotNull(object? value, string? message = null)
        {
            if (value == null)
            {
                throw new Exception(message ?? "Expected non-null value but got null");
            }
        }

        /// <summary>
        /// Asserts that the specified value is null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertNull(object? value, string? message = null)
        {
            if (value != null)
            {
                throw new Exception(message ?? $"Expected null but got [{value}]");
            }
        }

        /// <summary>
        /// Asserts that the specified condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertTrue(bool condition, string? message = null)
        {
            if (!condition)
            {
                throw new Exception(message ?? "Expected true but got false");
            }
        }

        /// <summary>
        /// Asserts that the specified condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertFalse(bool condition, string? message = null)
        {
            if (condition)
            {
                throw new Exception(message ?? "Expected false but got true");
            }
        }

        /// <summary>
        /// Asserts that a string contains the specified substring.
        /// </summary>
        /// <param name="haystack">The string to search within.</param>
        /// <param name="needle">The substring to search for.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertContains(string haystack, string needle, string? message = null)
        {
            if (haystack == null || !haystack.Contains(needle, StringComparison.Ordinal))
            {
                string failMessage = message
                    ?? $"Expected string to contain [{needle}] but it did not";
                throw new Exception(failMessage);
            }
        }

        /// <summary>
        /// Asserts that a string starts with the specified prefix.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="prefix">The expected prefix.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertStartsWith(string value, string prefix, string? message = null)
        {
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal))
            {
                string failMessage = message
                    ?? $"Expected string to start with [{prefix}] but got [{value}]";
                throw new Exception(failMessage);
            }
        }

        /// <summary>
        /// Asserts that the specified synchronous action throws an exception of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="action">The action expected to throw.</param>
        /// <param name="message">An optional failure message.</param>
        /// <returns>The caught exception instance.</returns>
        protected T AssertThrows<T>(Action action, string? message = null) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    message ?? $"Expected [{typeof(T).Name}] but got [{ex.GetType().Name}]: {ex.Message}");
            }

            throw new Exception(
                message ?? $"Expected [{typeof(T).Name}] but no exception was thrown");
        }

        /// <summary>
        /// Asserts that the specified asynchronous action throws an exception of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="action">The asynchronous action expected to throw.</param>
        /// <param name="message">An optional failure message.</param>
        /// <returns>The caught exception instance.</returns>
        protected async Task<T> AssertThrowsAsync<T>(Func<Task> action, string? message = null) where T : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    message ?? $"Expected [{typeof(T).Name}] but got [{ex.GetType().Name}]: {ex.Message}");
            }

            throw new Exception(
                message ?? $"Expected [{typeof(T).Name}] but no exception was thrown");
        }

        /// <summary>
        /// Asserts that a numeric value is greater than the specified threshold.
        /// </summary>
        /// <param name="actual">The actual value.</param>
        /// <param name="threshold">The threshold value.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertGreaterThan(long actual, long threshold, string? message = null)
        {
            if (actual <= threshold)
            {
                throw new Exception(message ?? $"Expected value greater than [{threshold}] but got [{actual}]");
            }
        }

        /// <summary>
        /// Asserts that a collection is not null and not empty.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to check.</param>
        /// <param name="message">An optional failure message.</param>
        protected void AssertNotEmpty<T>(ICollection<T>? collection, string? message = null)
        {
            if (collection == null || collection.Count == 0)
            {
                throw new Exception(message ?? "Expected non-empty collection but got null or empty");
            }
        }

        #endregion
    }
}
