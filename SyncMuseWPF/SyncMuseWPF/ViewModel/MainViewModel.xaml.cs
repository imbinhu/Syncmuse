using SyncMuseWPF.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SyncMuseWPF.ViewModel
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainViewModel : Window
    {

        public int CustomHeight { get; set; }
        public MainViewModel()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Height = 100;
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            YoutubeWindowView youtubeWindow = new YoutubeWindowView();
            MainGrid.SetCurrentValue(Grid.RowProperty, 1);
            MainGrid.SetCurrentValue(Grid.ColumnProperty, 0);

        }
    }
}
