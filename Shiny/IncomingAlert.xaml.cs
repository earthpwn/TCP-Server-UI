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
using System.Windows.Shapes;

namespace Shiny
{
    /// <summary>
    /// Interaction logic for IncomingAlert.xaml
    /// </summary>
    public partial class IncomingAlert : Window
    {
        private int result = 0;
        public IncomingAlert()
        {
            InitializeComponent();
        }

        public int ReturnResult(string ID)
        {
            Incoming.Title = "Alarm #" + ID;
            IncomingAlertContent.Content = "Alarm #" + ID;
            this.ShowDialog();
            return result;
        }

        private void shutDownButton_Click(object sender, RoutedEventArgs e)
        {
            result = 1;
        }

        private void ignoreButton_Click(object sender, RoutedEventArgs e)
        {
            result = 0;
        }
    }
}
