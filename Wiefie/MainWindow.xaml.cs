using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace Wiefie
{
    public partial class MainWindow : Window
    {
        int click = 1;
        int click2 = 1;
        Timer myTimer;
        Timer timer2;
        bool connected = false;
        bool end = false;
        bool off = false;
        string hname = null;
        string hpasswd = null;

        public MainWindow()
        {
            InitializeComponent();
            myTimer = new Timer();
            myTimer.Elapsed += new ElapsedEventHandler(CheckConnection);
            myTimer.Interval = 1000;
            myTimer.Enabled = false;

            timer2 = new Timer();
            timer2.Elapsed += new ElapsedEventHandler(RestartHotSpot);
            timer2.Interval = 300000;
            timer2.Enabled = false;

            windowname.Title = windowname.Title + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            click++;
            hname = textbox1.Text;

            if (textbox2.IsVisible == false)
            {
                hpasswd = textbox3.Text;
                buttonshow_Click(sender, e);
            }

            else
            {
                hpasswd = textbox2.Password.ToString();
            }

            if (click % 2 == 0)
            {
                if (string.IsNullOrWhiteSpace(hpasswd) || hpasswd.Length < 8)
                {
                    textbox2.Foreground = new SolidColorBrush(Colors.Red);
                    hotspotwachtwoord.Foreground = new SolidColorBrush(Colors.Red);
                }

                else
                {
                    textbox2.Foreground = new SolidColorBrush(Colors.Black);
                    hotspotwachtwoord.Foreground = new SolidColorBrush(Colors.Black);
                }

                if (string.IsNullOrWhiteSpace(hname))
                {
                    textbox1.Foreground = new SolidColorBrush(Colors.Red);
                    hotspotnaam.Foreground = new SolidColorBrush(Colors.Red);
                }

                else
                {
                    textbox1.Foreground = new SolidColorBrush(Colors.Black);
                    hotspotnaam.Foreground = new SolidColorBrush(Colors.Black);
                }

                if (string.IsNullOrWhiteSpace(hpasswd) || string.IsNullOrWhiteSpace(hname))
                {
                    click--;
                    return;
                }

                buttonshow.IsEnabled = false;
                checkbox1.IsEnabled = false;
                textbox1.IsEnabled = false;
                textbox2.IsEnabled = false;
                textbox3.IsEnabled = false;
                button1.Content = "Turn off";
                off = false;
                label1.Content = "On";
                myTimer.Enabled = true;
            }

            else
            {
                OffAsync();
            }
        }

        private void CheckConnection(object source, ElapsedEventArgs e)
        {
            end = false;
            connected = false;
            myTimer.Enabled = false;
            Ping myPing = new Ping();
            string host = "8.8.8.8";
            byte[] buffer = new byte[32];
            int timeout = 1000;
            PingOptions pingOptions = new PingOptions();
            PingReply pingresult;
            PingReply pingresult2;

            try
            {
                pingresult = myPing.Send(host, timeout, buffer, pingOptions);
                pingresult2 = myPing.Send(host, timeout, buffer, pingOptions);

                if (pingresult.Address == null && pingresult2.Address == null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        label2.Content = "Disconnected";
                        label3.Content = "Off";
                    });
                    ReConnect();
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        label2.Content = "Connected";
                    });
                    connected = true;

                    int i = 0;
                    SelectQuery wmiQuery = new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL");
                    ManagementObjectSearcher searchProcedure = new ManagementObjectSearcher(wmiQuery);
                    foreach (ManagementObject item in searchProcedure.Get())
                    {
                        string lan = ((string)item["NetConnectionId"]);
                        if (lan == "LAN-verbinding* 13")
                        {
                            i++;
                        }
                    }

                    if (i == 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            label3.Content = "Off";
                        });
                        timer2.Enabled = false;
                        HotSpot();
                    }

                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            label3.Content = "On";
                            timer2.Enabled = true;
                        });
                    }
                }
            }

            catch
            {
                this.Dispatcher.Invoke(() =>
                {
                    label2.Content = "Disconnected";
                });
                ReConnect();
            }
            end = true;

            if (click % 2 == 0 && connected == true)
            {
                myTimer.Enabled = true;
            }
        }

        public void ReConnect()
        {
            SelectQuery wmiQuery = new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL");
            ManagementObjectSearcher searchProcedure = new ManagementObjectSearcher(wmiQuery);
            foreach (ManagementObject item in searchProcedure.Get())
            {
                if (((string)item["NetConnectionId"]) == "Wi-Fi")
                {
                    item.InvokeMethod("Disable", null);
                    item.InvokeMethod("Enable", null);
                    WaitForNetwork();
                }
            }
        }

        public async void WaitForNetwork()
        {
            Ping myPing = new Ping();
            string host = "8.8.8.8";
            byte[] buffer = new byte[32];
            int timeout = 1000;
            PingOptions pingOptions = new PingOptions();
            int time = 0;

            while (connected == false)
            {
                try
                {
                    PingReply pingresult = myPing.Send(host, timeout, buffer, pingOptions);
                    PingReply pingresult2 = myPing.Send(host, timeout, buffer, pingOptions);

                    if (pingresult.Address != null && pingresult2.Address != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            label2.Content = "Connected";
                        });
                        await Task.Delay(1000);
                        connected = true;
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            label2.Content = "Connecting...";
                        });
                        time++;
                        await Task.Delay(1000);
                    }
                }
                catch
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        label2.Content = "Connecting...";
                    });
                    time++;
                    await Task.Delay(1000);
                }

                if (off == true)
                {
                    return;
                }

                if (time >= 10)
                {
                    ReConnect();
                }   

            }
            HotSpot();
            myTimer.Enabled = true;
        }

        public void HotSpot()
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            p.StartInfo = info;
            p.Start();

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("netsh wlan set hostednetwork mode = allow ssid = {0} key = {1}", hname, hpasswd);
                    sw.WriteLine("netsh wlan start hostednetwork");
                }
            }
            this.Dispatcher.Invoke(() =>
            {
                label3.Content = "On";
            });
        }

        public async Task OffAsync()
        {
            off = true;
            while (end == false)
            {
                await Task.Delay(1000);
            }

            if (checkbox1.IsChecked == true)
            {
                buttonshow.IsEnabled = false;
                textbox1.IsEnabled = false;
                textbox2.IsEnabled = false;
                textbox3.IsEnabled = false;
            }

            else
            {
                textbox1.IsEnabled = true;
                textbox2.IsEnabled = true;
                textbox3.IsEnabled = true;
                buttonshow.IsEnabled = true;
            }

            checkbox1.IsEnabled = true;
            myTimer.Enabled = false;
            timer2.Enabled = false;
            button1.Content = "Turn on";
            label1.Content = "Off";
            label2.Content = "-";
            label3.Content = "-";
            connected = false;

            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            p.StartInfo = info;
            p.Start();

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("netsh wlan stop hostednetwork");
                }
            }
        }

        public void RestartHotSpot(object source, ElapsedEventArgs e)
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            p.StartInfo = info;
            p.Start();

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("netsh wlan start hostednetwork");
                }
            }
            this.Dispatcher.Invoke(() =>
            {
                label3.Content = "On";
            });
            timer2.Enabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OffAsync();
        }

        private void buttonshow_Click(object sender, RoutedEventArgs e)
        {
            click2++;

            if (click2 % 2 == 0)
            {
                buttonshow.Content = "Hide";
                textbox2.Visibility = Visibility.Hidden;
                textbox3.Visibility = Visibility.Visible;
                textbox3.Text = textbox2.Password;
            }
            else
            {
                buttonshow.Content = "Show";
                textbox2.Visibility = Visibility.Visible;
                textbox3.Visibility = Visibility.Hidden;
                textbox2.Password = textbox3.Text;
            }
        }

        private void checkbox1_Checked(object sender, RoutedEventArgs e)
        {
            hname = "Wiefie";
            hpasswd = "Hannahisschattig";
            textbox1.Text = hname;
            textbox2.Password = hpasswd;
            textbox3.Text = hpasswd;
            textbox1.IsEnabled = false;
            textbox2.IsEnabled = false;
            textbox3.IsEnabled = false;
            buttonshow.IsEnabled = false;

            if (textbox2.IsVisible == false)
            {
                buttonshow_Click(sender, e);
            }
        }

        private void checkbox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (click % 2 != 0)
            {
                hname = "";
                hpasswd = "";
                textbox1.Text = hname;
                textbox2.Password = hpasswd;
                textbox3.Text = hpasswd;
                textbox1.IsEnabled = true;
                textbox2.IsEnabled = true;
                textbox3.IsEnabled = true;
                buttonshow.IsEnabled = true;
            }
        }
    }
}