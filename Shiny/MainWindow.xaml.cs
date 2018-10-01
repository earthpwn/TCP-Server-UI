using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using System.Windows.Resources;
using System.Windows.Shapes;

namespace Shiny
{
    // TODO: Create an alert pop-up when a new message is recieved
    // TODO: Add additional column to database, indicating the alarm situation such that on/off
    // TODO: Create another Data Grid & Table to track ongoing alarms which will only retrieve active alarms
    // TODO: Make additional code changes regarding DB change above (inserting & retrieving alarms, data table & data grid etc)
    // TODO: Add another data grid column to shut down alarms (or some other control element)
    // PS: Do not touch current data grid. This will be used for historic purposes

    public partial class MainWindow : Window
    {
        bool serverRunning = false;
        DataTable AlertData = new DataTable();
        static String LogFileName;

        public MainWindow()
        {
            InitializeComponent();
            LogFileName = "log" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            AlertData.Columns.Add("Time", Type.GetType("System.String"));
            AlertData.Columns.Add("Location", Type.GetType("System.String"));
            RetrieveAlerts();
        }

        private void AlertTable_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if(AlertTable.Columns.Count > 0)
                {
                    AlertTable.Columns[0].Width = 160;
                    AlertTable.Columns[1].Width = 120;
                }
            }
            catch(Exception ex)
            {
                ConsoleAddItem(ex.Message);
                LogWorker(ex.Message);
            }
        }

        private void ButtonStartServer_Click(object sender, RoutedEventArgs e)
        {
            ConsoleAddItem("Server is started");
            LogWorker("Server is started");
            serverRunning = true;
            Task.Factory.StartNew(() => StartServer());
        }

        private void ButtonStopServer_Click(object sender, RoutedEventArgs e)
        {
            ConsoleAddItem("Server is stopped");
            LogWorker("Server is stopped");
            serverRunning = false;
        }

        private void StartnStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!serverRunning)
            {
                serverRunning = true;
                Task.Factory.StartNew(() => StartServer());
                StartnStopButton.Content = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Shiny;component/stop.png")), Stretch = Stretch.Uniform, Height = 100, Width = 100 };
            }
            else
            {
                serverRunning = false;
                StartnStopButton.Content = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Shiny;component/start.png")), Stretch = Stretch.Uniform, Height = 100, Width = 100 };
            }
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
            const string IP_Adresi = "192.168.1.203"; // qanqi modul testi için static ip lazım malum XD halledersin sen de. hem 203 güzel bi ip. walla
            const int Port_No = 5000;
            List<Thread> threadler = new List<Thread>();

            TcpListener sunucu = new TcpListener(IPAddress.Parse(IP_Adresi), Port_No);

            sunucu.Start();
            ConsoleAddItem(String.Format(String.Format("Server has started on {0}:{1}", IP_Adresi, Port_No)));
            LogWorker(String.Format(String.Format("Server has started on {0}:{1}", IP_Adresi, Port_No)));

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
            ConsoleAddItem("Server is stopped");
            LogWorker("Server is stopped");
            Thread.CurrentThread.Abort();
        }


        private void IstemciThreadi(TcpListener sunucu)
        {
            TcpClient istemci = sunucu.AcceptTcpClient();

            ConsoleAddItem(String.Format("A client connected. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
            LogWorker(String.Format("A client connected. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));

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
                        LogWorker(String.Format("{0}: {1}", Thread.CurrentThread.ManagedThreadId, gelen_veri));
                        if (InsertAlert(gelen_veri))
                        {
                            ConsoleAddItem(String.Format("Alert info has been inserted to DB successfully!"));
                            LogWorker(String.Format("Alert info has been inserted to DB successfully!"));
                            RetrieveAlerts();
                        }
                        else
                        {
                            ConsoleAddItem(String.Format("Alert info could NOT be inserted!!!"));
                            LogWorker(String.Format("Alert info could NOT be inserted!!!"));
                            //handle rest
                        }
                    }
                }
                catch (System.IO.IOException)
                {
                    ConsoleAddItem(String.Format("Connection lost. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
                    LogWorker(String.Format("Connection lost. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
                    break;
                }
            }
            Thread.CurrentThread.Abort();
        }


        private bool InsertAlert(String location)
        {
            bool result = false;
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                String alertTime = DateTime.Now.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("tr-TR"));
                conn.Open();
                ConsoleAddItem("DB Connection established!");
                LogWorker("DB Connection established!");
                MySqlCommand cmd = new MySqlCommand(String.Format("INSERT INTO anan.test (Time, Location) VALUES ('{0}', '{1}')", alertTime, location), conn);
                cmd.ExecuteNonQuery();
                ConsoleAddItem("Query was run succesfully!");
                LogWorker("Query was run succesfully!");
                result = true;
            }
            catch (Exception ex)
            {
                
            }
            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                    LogWorker("DB Connection Closed.");
                }
                catch { }
            }
            return result;
        }

        private void RetrieveAlerts()
        {
            ConsoleAddItem("Retrieving alert data from DB.");
            LogWorker("Retrieving alert data from DB.");
            this.Dispatcher.Invoke(() => { AlertTable.ItemsSource = null; });
            AlertData.Rows.Clear();
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ConsoleAddItem("DB Connection established!");
                LogWorker("DB Connection established!");
                MySqlCommand cmd = new MySqlCommand(String.Format("SELECT * FROM anan.test"), conn);
                ConsoleAddItem("Query is executing...");
                LogWorker("Query is executing...");
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AlertData.Rows.Add(new object[] { reader["Time"].ToString(), reader["Location"].ToString() });
                    }
                    ConsoleAddItem("Alert records have been retrieved from DB.");
                    LogWorker("Alert records have been retrieved from DB.");
                }
                this.Dispatcher.Invoke(() => { AlertTable.ItemsSource = AlertData.DefaultView; });
                this.Dispatcher.Invoke(() => { AlertTable_Loaded(new object(), new RoutedEventArgs()); });
            }
            catch(Exception ex)
            {
                ConsoleAddItem(ex.Message);
                LogWorker(ex.Message);
            }
            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                    LogWorker("DB Connection Closed.");
                }
                catch { }
            }
        }


        private void LogWorker(String log)
        {
            try
            {
                using (StreamWriter w = File.AppendText(LogFileName))
                {
                    w.WriteLine("{0} : {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), log);
                }
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
        }


    }
}
