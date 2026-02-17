using System;
using System.Windows.Forms;

namespace MISDAudioCard.Example
{
    /// <summary>
    /// MISDAudioCard Example Program
    /// Demonstrates audio input and output using sound card
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
