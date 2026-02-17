using System;
using System.Windows.Forms;

namespace MISDAudioCard.Example.AI_AO
{
    /// <summary>
    /// MISDAudioCard AI-AO Example Program
    /// Demonstrates audio input (AI) and output (AO) using sound card
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
