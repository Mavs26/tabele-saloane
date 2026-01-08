using System;
using System.Windows.Forms;

namespace lab9
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            using var auth = new AuthForm();
            var result = auth.ShowDialog();

            if (result != DialogResult.OK)
                return; // user a inchis / cancel => inchide app

            Application.Run(new Form1(auth.LoggedInUser));
        }
    }
}
