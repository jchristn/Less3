namespace Test.MultiNode
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Runs named tests, timing each and printing a PASS/FAIL line as it completes, then prints a
    /// summary with the overall result and total runtime. A failure prints the reason and any
    /// captured detail so the cause is visible without rerunning.
    /// </summary>
    public sealed class TestReport
    {
        private readonly List<TestResult> _Results = new List<TestResult>();
        private readonly Stopwatch _Overall = Stopwatch.StartNew();
        private string _CurrentSection = "General";

        /// <summary>
        /// Start a new section; subsequent tests are grouped under it.
        /// </summary>
        /// <param name="title">Section title.</param>
        public void Section(string title)
        {
            _CurrentSection = title;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("== " + title + " ==");
            Console.ResetColor();
        }

        /// <summary>
        /// Emit an informational (non-test) line.
        /// </summary>
        /// <param name="message">Message.</param>
        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("   " + message);
            Console.ResetColor();
        }

        /// <summary>
        /// Run a single test, recording and printing its result.
        /// </summary>
        /// <param name="name">Test name.</param>
        /// <param name="test">The test body; throwing indicates failure.</param>
        /// <returns>Task.</returns>
        public async Task RunAsync(string name, Func<Task> test)
        {
            Stopwatch sw = Stopwatch.StartNew();
            TestResult result = new TestResult { Name = name, Section = _CurrentSection };

            try
            {
                await test().ConfigureAwait(false);
                sw.Stop();
                result.Passed = true;
                result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine(name + " (" + FormatMs(result.ElapsedMs) + ")");
            }
            catch (Exception e)
            {
                sw.Stop();
                result.Passed = false;
                result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                result.Detail = Flatten(e);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine(name + " (" + FormatMs(result.ElapsedMs) + ")");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (string line in result.Detail.Split('\n'))
                {
                    Console.WriteLine("       " + line.TrimEnd('\r'));
                }
                Console.ResetColor();
            }

            _Results.Add(result);
        }

        /// <summary>
        /// Print the summary and return the process exit code (0 = all passed, 1 = any failed).
        /// </summary>
        /// <returns>Exit code.</returns>
        public int Summarize()
        {
            _Overall.Stop();
            int passed = 0;
            int failed = 0;
            foreach (TestResult r in _Results)
            {
                if (r.Passed) passed++;
                else failed++;
            }

            Console.WriteLine();
            Console.WriteLine("============================================================");

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Failed tests:");
                foreach (TestResult r in _Results)
                {
                    if (!r.Passed) Console.WriteLine("    - [" + r.Section + "] " + r.Name);
                }
                Console.ResetColor();
            }

            Console.Write("  Total: " + _Results.Count + "    Passed: " + passed + "    Failed: " + failed);
            Console.WriteLine("    Runtime: " + (_Overall.Elapsed.TotalSeconds).ToString("F1") + "s");

            Console.Write("  RESULT:  ");
            if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASS");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
            }
            Console.ResetColor();
            Console.WriteLine("============================================================");

            return failed == 0 ? 0 : 1;
        }

        private static string FormatMs(double ms)
        {
            if (ms >= 1000) return (ms / 1000.0).ToString("F2") + " s";
            return ms.ToString("F0") + " ms";
        }

        private static string Flatten(Exception e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Exception current = e;
            int depth = 0;
            while (current != null && depth < 5)
            {
                sb.Append(current.GetType().Name + ": " + current.Message);
                current = current.InnerException;
                if (current != null) sb.Append('\n');
                depth++;
            }
            return sb.ToString();
        }
    }
}
