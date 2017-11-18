using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;

namespace ComListener
{
    public class ComListenerApp : ApplicationContext
    {
        private NotifyIcon trayIcon = new NotifyIcon();
        private Task listener;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private List<string> currentPorts = new List<string>();

        public ComListenerApp()
        {
            // tray icon init
            trayIcon.Icon = ComListener.Properties.Resources.AppIcon;
            ContextMenu ctx = new ContextMenu();
            ctx.MenuItems.Add(new MenuItem("Exit", Exit));
            trayIcon.ContextMenu = ctx;
            trayIcon.Visible = true;
            trayIcon.MouseDoubleClick += trayIcon_MouseDoubleClick;
            trayIcon.BalloonTipTitle = "COM port changed:";
            trayIcon.Text = "ComListener v1.0";

            currentPorts.AddRange(SerialPort.GetPortNames());

            listener = new Task(new Action(CheckPorts), cts.Token);
            listener.Start();
        }

        void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(trayIcon.BalloonTipText))
            {
                trayIcon.ShowBalloonTip(1000); // show last event
            }
        }

        private void CheckPorts()
        {
            List<string> temp = new List<string>();

            while (!cts.IsCancellationRequested)
            {
                temp.Clear();
                temp.AddRange(SerialPort.GetPortNames());

                if (currentPorts.Count < temp.Count) // port inserted
                {
                    trayIcon.BalloonTipText = String.Empty;
                    var ports = temp.Except(currentPorts);

                    foreach (string port in ports)
                    {
                        trayIcon.BalloonTipText += port + " inserted\n";
                    }

                    trayIcon.ShowBalloonTip(1000);
                }
                else if (currentPorts.Count > temp.Count) // port removed
                {
                    trayIcon.BalloonTipText = String.Empty;
                    var ports = currentPorts.Except(temp);

                    foreach (string port in ports)
                    {
                        trayIcon.BalloonTipText += port + " removed\n";
                    }

                    trayIcon.ShowBalloonTip(1000);
                }

                // update ports to current state
                currentPorts.Clear();
                currentPorts.AddRange(SerialPort.GetPortNames());

                Thread.Sleep(500);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user hover mouse over it
            trayIcon.Visible = false;
            cts.Cancel(); // stop task
            listener.Wait(); // wait till task is really stopped
            Application.Exit(); // exit program
        }
    }
}
