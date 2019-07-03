using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Less3.Classes
{
    /// <summary>
    /// Console manager.
    /// </summary>
    public class ConsoleManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private bool _Enabled = true;
        private Func<bool> _ExitDelegate;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="exitDelegate">Method to call when exiting the console.</param>
        public ConsoleManager(
            Func<bool> exitDelegate)
        {
            if (exitDelegate == null) throw new ArgumentNullException(nameof(exitDelegate));

            _ExitDelegate = exitDelegate;

            Task.Run(() => ConsoleTask());
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Terminate the console.
        /// </summary>
        public void Stop()
        {
            _Enabled = false;
            return;
        }

        #endregion

        #region Private-Methods

        private void Menu()
        {
            Console.WriteLine("-- Available Commands --");
            Console.WriteLine("   q         Quit");
            Console.WriteLine("   ?         Help, this menu");
            Console.WriteLine("   cls       Clear the screen");
            Console.WriteLine("");
        }

        private void ConsoleTask()
        {
            while (_Enabled)
            {
                string userInput = Common.InputString("Command [? for help]:", null, false);

                switch (userInput.ToLower())
                {
                    case "?":
                        Menu();
                        break;

                    case "c":
                    case "cls":
                        Console.Clear();
                        break;

                    case "q":
                        _Enabled = false;
                        _ExitDelegate();
                        break;
                }
            }
        }

        #endregion
    }
}
