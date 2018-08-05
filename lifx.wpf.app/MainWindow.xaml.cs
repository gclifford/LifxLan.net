using lifx.Library;
using lifx.Library.Models;
using lifx.Library.Responses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lifx.wpf.app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static NLog.Logger __logger = NLog.LogManager.GetCurrentClassLogger();

        Client _client = null;

        object _lock = new object();

        public ObservableCollection<Device> Devices
        {
            get 
            {
                return _client.Devices;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _client = new Client();
            _client.Initialize();
            _client.Devices.CollectionChanged += Devices_CollectionChanged;

            BindingOperations.EnableCollectionSynchronization(_client.Devices, _lock);

            DataContext = this;
        }

        private async void Devices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            List<Task<StateVersionResponse>> tasks = new List<Task<StateVersionResponse>>();


            if (e.NewItems != null)
            {
                foreach (var i in e.NewItems)
                {
                    var device = i as Device;
                    __logger.Info($"Device Added: {device.Hostname}");
                    tasks.Add(_client.GetDeviceVersion(device));
                }
            }

            if (e.OldItems != null)
            {
                foreach (var i in e.OldItems)
                {
                    var device = i as Device;
                    __logger.Info($"Device Old: {device.Hostname}");
                }
            }

            await Task.WhenAll(tasks);

            foreach(var task in tasks)
            {
                __logger.Info("Task Done");
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            _client.DiscoverDevices();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}
