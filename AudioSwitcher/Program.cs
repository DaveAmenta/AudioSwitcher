using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AudioSwitcher
{
    static class Program
    {
        class EndPointEntry
        {
            public string Text { get; set; }
            public string Index { get; set; }

            internal void SwitchTo()
            {
                InvokeEndPointController(Index);
            }
        }

        static string InvokeEndPointController(string args = null)
        {
            var endController = new Process();
            endController.StartInfo.FileName = "EndPointController.exe";
            endController.StartInfo.Arguments = args;
            endController.StartInfo.UseShellExecute = false;
            endController.StartInfo.CreateNoWindow = true;
            if (string.IsNullOrWhiteSpace(args))
            {
                endController.StartInfo.RedirectStandardOutput = true;
            }
            
            endController.Start();
            string output = null;
            if (string.IsNullOrWhiteSpace(args))
            {
                output = endController.StandardOutput.ReadToEnd();
                endController.WaitForExit();
            }
            return output;
        }

        static IList<EndPointEntry> GetDevices()
        {
            var output = InvokeEndPointController();

            var Devices = new SortedList<string, EndPointEntry>();
            foreach (string line in output.Split(new char[] { '\n' }))
            {
                Match mx = Regex.Match(line, @"Audio Device ([0-9]*): (.*?) \((.*?)\)");
                if (mx.Success)
                {
                    var dev = new EndPointEntry
                    {
                        Text = string.Format("{0} ({1})", mx.Groups[2].Value, mx.Groups[3].Value),
                        Index = mx.Groups[1].Value
                    };
                    Devices.Add(dev.Text, dev);
                }
            }

            return Devices.Values;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var t = new Thread(() =>
            {
                var trayIcon = new NotifyIcon();
                trayIcon.Icon = AudioSwitcher.Properties.Resources.NotificationIcon;
                trayIcon.Text = "Audio Switcher";
                trayIcon.Visible = true;
                trayIcon.ContextMenu = new ContextMenu();

                var menuItem = new MenuItem();
                menuItem.Text = "Quit";
                menuItem.Click += (ss, ee) =>
                {
                    trayIcon.Visible = false;
                    Environment.Exit(0);
                };
                trayIcon.ContextMenu.MenuItems.Add(menuItem);

                trayIcon.ContextMenu.MenuItems.Add(new MenuItem("-"));

                foreach(var dev in GetDevices())
                {
                    menuItem = new MenuItem();
                    menuItem.Text = dev.Text;
                    menuItem.Click += (_, __) => dev.SwitchTo();
                    trayIcon.ContextMenu.MenuItems.Add(menuItem);
                }

                Application.Run();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
