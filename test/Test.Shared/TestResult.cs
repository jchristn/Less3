namespace Test.Shared
{
    using System;

    /// <summary>
    /// Represents the outcome of a single test execution.
    /// </summary>
    public class TestResult
    {
        #region Private-Members

        private string _Name = string.Empty;
        private bool _Passed = false;
        private string? _Message = null;
        private Exception? _Exception = null;
        private long _ElapsedMs = 0;

        #endregion

        #region Public-Members

        /// <summary>
        /// The name of the test.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = value;
        }

        /// <summary>
        /// Whether the test passed.
        /// </summary>
        public bool Passed
        {
            get => _Passed;
            set => _Passed = value;
        }

        /// <summary>
        /// An optional message describing the result, typically set on failure.
        /// </summary>
        public string? Message
        {
            get => _Message;
            set => _Message = value;
        }

        /// <summary>
        /// The exception that caused the test to fail, if any.
        /// </summary>
        public Exception? Exception
        {
            get => _Exception;
            set => _Exception = value;
        }

        /// <summary>
        /// The elapsed time in milliseconds for the test execution.
        /// </summary>
        public long ElapsedMs
        {
            get => _ElapsedMs;
            set => _ElapsedMs = value;
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResult"/> class with the specified test name.
        /// </summary>
        /// <param name="name">The name of the test.</param>
        public TestResult(string name)
        {
            _Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Marks this test result as passed with the given elapsed time.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
        public void MarkPassed(long elapsedMs)
        {
            _Passed = true;
            _ElapsedMs = elapsedMs;
            _Message = null;
            _Exception = null;
        }

        /// <summary>
        /// Marks this test result as failed with the given elapsed time, message, and optional exception.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
        /// <param name="message">A message describing the failure.</param>
        /// <param name="exception">The exception that caused the failure, if any.</param>
        public void MarkFailed(long elapsedMs, string message, Exception? exception = null)
        {
            _Passed = false;
            _ElapsedMs = elapsedMs;
            _Message = message;
            _Exception = exception;
        }

        #endregion
    }
}
