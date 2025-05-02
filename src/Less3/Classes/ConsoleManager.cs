namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SyslogLogging;

    /// <summary>
    /// Console for less3.
    /// </summary>
    internal class ConsoleManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private bool _Enabled { get; set; }
        private Settings _Settings { get; set; } 
        private LoggingModule _Logging { get; set; }  

        #endregion

        #region Constructors-and-Factories

        internal ConsoleManager(
            Settings settings,
            LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging)); 

            _Enabled = true;

            _Settings = settings;
            _Logging = logging; 
        }

        #endregion

        #region Public-Methods

        internal void Worker()
        {
            string userInput = "";

            while (_Enabled)
            {
                Console.Write("Command (? for help) > ");
                userInput = Console.ReadLine();

                if (userInput == null) continue;
                switch (userInput.ToLower().Trim())
                {
                    case "?":
                        Menu();
                        break;

                    case "c":
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;

                    case "q":
                    case "quit":
                        _Enabled = false;
                        break; 
                }
            }
        }

        #endregion

        #region Private-Methods

        private void Menu()
        {
            Console.WriteLine(Common.Line(79, "-"));
            Console.WriteLine("  ?                         help / this menu");
            Console.WriteLine("  cls / c                   clear the console");
            Console.WriteLine("  quit / q                  exit the application"); 
            Console.WriteLine("");
            return;
        }
         
        #endregion 
    }
}
