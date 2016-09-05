// Program.cs
// 
// Copyright (c) 2016-2016 midolin limegreen All right reserved.
// 
// License:
// See LICENSE.md or README.md in solution root directory.

using System;
using System.Diagnostics;
using System.Windows;

namespace KinokoBreaker.Launcher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Process.Start(new ProcessStartInfo(".\\bin\\KinokoBreaker.exe")
                {
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                });
            }
            catch (Exception)
            {
                MessageBox.Show("起動に失敗しました。");
            }
        }
    }
}
