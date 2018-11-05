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
    /// Interaction logic for AlertShutDownDialog.xaml
    /// </summary>
    public partial class AlertShutDownDialog : Window
    {
        private int result = 0;
        public AlertShutDownDialog()
        {
            InitializeComponent();

        }

        public int ReturnResult(string ID)
        {
            ShutDownDialog.Title = "Alarm #" + ID;
            this.ShowDialog();
            return result;
        }

        private void yesButton_Click(object sender, RoutedEventArgs e)
        {
            result = 1;
            this.Close();
        }

        private void noButton_Click(object sender, RoutedEventArgs e)
        {
            result = 0;
            this.Close();
        }
    }
}
