namespace Test.MultiNode
{
    /// <summary>
    /// The outcome of a single test.
    /// </summary>
    public sealed class TestResult
    {
        /// <summary>Test name.</summary>
        public string Name { get; set; }

        /// <summary>Section the test belonged to.</summary>
        public string Section { get; set; }

        /// <summary>Whether the test passed.</summary>
        public bool Passed { get; set; }

        /// <summary>Elapsed time in milliseconds.</summary>
        public double ElapsedMs { get; set; }

        /// <summary>Failure detail (why it failed), when applicable.</summary>
        public string Detail { get; set; }
    }
}
