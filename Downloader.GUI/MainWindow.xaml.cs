using System.Timers;
using Downloader;
using System;
using System.Collections.Generic;
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
using Downloader.DownloadSites;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowView _view = new MainWindowView();
        public MainWindow()
        {
            InitializeComponent();
            Grid.DataContext = _view;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Grid.SelectedItems)
            {
                FileDescription file = item as FileDescription;
                if (file != null)
                {
                    file.Start();
                }
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Grid.SelectedItems)
            {
                FileDescription file = item as FileDescription;
                if (file != null)
                {
                    file.Stop();
                }
            }
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Grid.SelectedItems)
            {
                FileDescription file = item as FileDescription;
                if (file != null)
                {
                    _view.Up(file);
                }
            }
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Grid.SelectedItems)
            {
                FileDescription file = item as FileDescription;
                if (file != null)
                {
                    _view.Down(file);
                }
            }
        }

        async private void ExploreUrl_Click(object sender, RoutedEventArgs e)
        {
            foreach (FileDescription download in await ExploreHelper.ExtractDownloadDescriptions(Url.Text))
            {
                _view.Add(download);
            }
        }
    }
}
