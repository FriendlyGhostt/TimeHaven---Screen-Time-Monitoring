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
using TimeHaven.ViewModels;
using TimeHaven;


namespace TimeHaven
{
    public partial class Page1 : UserControl
    {
        private ChartViewModel ViewModel { get; set; }

        public Page1()
        {
            InitializeComponent();
            ViewModel = new ChartViewModel();
            DataContext = ViewModel;
        }

        private void CartesianChart_Loaded(object sender, RoutedEventArgs e)
        {
        }
        public void Page1Action(bool check_event)
        {
            ViewModel.ChartAction(check_event);
        }
    }

}
