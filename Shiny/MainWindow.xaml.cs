using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace Shiny
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool serverRunning = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonStartServer_Click(object sender, RoutedEventArgs e)
        {
            Console.Items.Add("Server is started");
            serverRunning = true;
            Task.Factory.StartNew(() => StartServer());
        }

        private void ButtonStopServer_Click(object sender, RoutedEventArgs e)
        {
            Console.Items.Add("Server is stopped");
            serverRunning = false;
        }

        private void Console_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ConsoleAddItem(String item)
        {
            Console.Dispatcher.BeginInvoke(new Action(delegate ()
            {
                Console.Items.Add(item);
            }));
        }

        // bekletme(ms)
        public async Task<string> WaitAsynchronouslyAsync(int i)
        {
            await Task.Delay(i);
            return "Finished";
        }

        private async void StartServer()
        {
            const string IP_Adresi = "127.0.0.1";
            const int Port_No = 5000;
            List<Thread> threadler = new List<Thread>();

            TcpListener sunucu = new TcpListener(IPAddress.Parse(IP_Adresi), Port_No);

            sunucu.Start();
            ConsoleAddItem(String.Format(String.Format("Server has started on {0}:{1}", IP_Adresi, Port_No)));

            while (serverRunning)
            {
                if (sunucu.Pending())
                {
                    threadler.Add(new Thread(() => IstemciThreadi(sunucu)));
                    threadler[threadler.Count - 1].Start();
                    /*ConsoleAddItem(String.Format("Current Threads:");
                    for (int i = 0; i < threadler.Count; i++)
                    {
                        ConsoleAddItem(String.Format("Thread ID: {0} - Thread Status: {1}", threadler[i].ManagedThreadId, threadler[i].ThreadState);
                    }*/
                }
                await WaitAsynchronouslyAsync(100);
            }
            sunucu.Stop();
            Thread.CurrentThread.Abort();
        }


        private void IstemciThreadi(TcpListener sunucu)
        {
            TcpClient istemci = sunucu.AcceptTcpClient();

            ConsoleAddItem(String.Format("A client connected. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));

            NetworkStream yayin = istemci.GetStream();

            //enter to an infinite cycle to be able to handle every change in stream
            while (serverRunning)
            {
                try
                {
                    Byte[] gelen_ham_baytlar = new Byte[istemci.Available];
                    yayin.Read(gelen_ham_baytlar, 0, gelen_ham_baytlar.Length);

                    //translate bytes of request to string
                    String gelen_veri = Encoding.UTF8.GetString(gelen_ham_baytlar);
                    if (gelen_veri.Length > 0)
                    {
                        ConsoleAddItem(String.Format("{0}: {1}", Thread.CurrentThread.ManagedThreadId, gelen_veri));
                        if (InsertAlert(gelen_veri))
                        {
                            ConsoleAddItem(String.Format("Alert info has been inserted to DB successfully!"));
                        }
                        else
                        {
                            ConsoleAddItem(String.Format("Alert info could NOT be inserted!!!"));
                            //handle rest
                        }
                    }
                }
                catch (System.IO.IOException)
                {
                    ConsoleAddItem(String.Format("Connection lost. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
                    break;
                }
            }
            Thread.CurrentThread.Abort();
        }


        private bool InsertAlert(String location)
        {
            bool result = false;
            string connStr = "server=localhost;user=root;database=anan;port=3306;password=anan";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                String alertTime = DateTime.Now.ToString();
                conn.Open();
                ConsoleAddItem("DB Connection established!");
                MySqlCommand cmd = new MySqlCommand(String.Format("INSERT INTO test (Time, Location) VALUES ('{0}', '{1}')", alertTime, location), conn);
                cmd.ExecuteNonQuery();
                ConsoleAddItem("Query was run succesfully!");
                result = true;
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
            if (conn.State == System.Data.ConnectionState.Open)
            {
                conn.Close();
                ConsoleAddItem("DB Connection Closed.");
            }
            return result;
        }


    }
}
