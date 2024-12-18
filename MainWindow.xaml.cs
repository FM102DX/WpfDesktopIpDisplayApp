using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WpfDesktopIpDisplayApp
{
    public class IpInfoElementList
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    public partial class MainWindow : Window
    {
        private int _autoRefreshInterval = 2000; // Интервал в миллисекундах (2 секунды)
        private readonly DispatcherTimer _timer;
        private readonly string _workingDirectory;
        private readonly List<IpInfoElementList> _networkInfoList=new();
        private double _rectangleWidthBase = 250;
        private double _rectangleHeightBase = 120;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public double RectangleWidth => _rectangleWidthBase;
        public double RectangleHeight => _rectangleHeightBase + _networkInfoList.Count * 20;
        private void SetRectangle()
        {
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, RectangleWidth, RectangleHeight), // Указываем прямоугольник
                RadiusX = 30, // Радиус скругления по X
                RadiusY = 30  // Радиус скругления по Y
            };
            Height = RectangleHeight;
            Width = RectangleWidth;
            OnPropertyChanged();
        }

        public MainWindow()
        {
            InitializeComponent();
            SetRectangle();
            // Initialize the working directory
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _workingDirectory = Path.Combine(appDirectory, "Data");

            if (!Directory.Exists(_workingDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_workingDirectory);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create working directory: {_workingDirectory}", ex);
                }
            }

            PositionWindowAtCustomPoint();
            LoadNetworkInfo();
            AddToStartup();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(_autoRefreshInterval);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowContextMenu(e);
        }

        private void ShowContextMenu(MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();

            var closeItem = new MenuItem
            {
                Header = "Закрыть"
            };
            closeItem.Click += (s, args) => this.Close();

            var removeStartupItem = new MenuItem
            {
                Header = "Убрать из автозагрузки"
            };
            removeStartupItem.Click += (s, args) => RemoveFromStartup();

            menu.Items.Add(closeItem);
            menu.Items.Add(removeStartupItem);

            menu.IsOpen = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadNetworkInfo();
            SetRectangle();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworkInfo();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadNetworkInfo()
        {
            _networkInfoList.Clear();
            try
            {
                var ip = GetActiveEthernetName();
                var dns = GetDnsAddress();

                _networkInfoList.Add(new IpInfoElementList { Name = "IP", Text = ip });
                _networkInfoList.Add(new IpInfoElementList { Name = "DNS", Text = dns });

                NetworkInfoListBox.ItemsSource = null;
                NetworkInfoListBox.ItemsSource = _networkInfoList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetActiveEthernetName()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    ni.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = ni.GetIPProperties();
                    var ipv4Address = ipProperties.UnicastAddresses
                        .FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                    if (ipv4Address != null)
                    {
                        return ipv4Address.Address.ToString();
                    }
                }
            }

            return "undefined";
        }

        public string GetDnsAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    ni.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = ni.GetIPProperties();
                    var dnsAddress = ipProperties.DnsAddresses
                        .FirstOrDefault(dns => dns.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                    if (dnsAddress != null)
                    {
                        return dnsAddress.ToString();
                    }
                }
            }

            return "undefined";
        }

        private void AddToStartup()
        {
            try
            {
                string appName = "WpfDesktopIpDisplayApp";
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key.GetValue(appName) == null)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка автозагрузки: {ex.Message}");
            }
        }

        private void RemoveFromStartup()
        {
            try
            {
                string appName = "WpfDesktopIpDisplayApp";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName);
                    MessageBox.Show("Программа удалена из автозагрузки.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Программа не найдена в автозагрузке.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления из автозагрузки: {ex.Message}");
            }
        }
        private void PositionWindowAtCustomPoint()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = screenWidth - 270;
            this.Top = 30;
        }

    }
}