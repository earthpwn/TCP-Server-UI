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
        private string comment = "";
        public AlertShutDownDialog()
        {
            InitializeComponent();
        }

        public Tuple<int, string> ReturnResult(string ID)
        {
            ShutDownDialog.Title = "Alarm #" + ID;
            shutDownTextLabel.Content = shutDownTextLabel.Content.ToString().Replace("#", "#" + ID);
            this.ShowDialog();
            return Tuple.Create(result, comment);
        }

        private void yesButton_Click(object sender, RoutedEventArgs e)
        {
            result = 1;
            if (commentBox.Opacity == 100 && new TextRange(commentBox.Document.ContentStart, commentBox.Document.ContentEnd).Text != "")
            {
                comment = (new TextRange(commentBox.Document.ContentStart, commentBox.Document.ContentEnd).Text);
            }
            this.Close();
        }

        private void noButton_Click(object sender, RoutedEventArgs e)
        {
            result = 0;
            this.Close();
        }
        
        private void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            commentBox.Opacity = 100;
            commentBox.IsReadOnly = false;
            commentBox.Cursor = Cursors.IBeam;
            commentBox.Document.Blocks.Clear();
        }
    }
}
