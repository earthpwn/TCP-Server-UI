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

    //  TODO: Modify TCP Server & Client so that server can send messages to client

    //  TODO: Pop-up a message/info box as a warning when client drops
    
    //  TODO: Research on how to store mysql connection string in a secure way. encrypt or something

    //  TODO: Play an alarm sound when anew alert is recieved
    //  TODO: SIDE QUEST: When new alert is recieved, bring main window to front and make it optional later on
    //  TODO: SIDE QUEST: When new alert is recieved, highlight it in yellow or smt, remove highlight when hovered

    //  Visual
    //  TODO: Work on visuals such that alert shut down dialog, active and all alarm grids. make it look cool, use ur imagination madafaka
    //  TODO: In all alarms grid, highlight client drop cases in red
    //  TODO: Make status column of active alarms more detectable. User should be able to understand the functionality easily




    public partial class MainWindow : Window
    {
        bool serverRunning = false;
        DataTable AlertData = new DataTable();
        DataTable ActiveAlertData = new DataTable();
        static String LogFileName;

        public enum Activity : byte
        {
            ON_Triggered = 0,
            OFF_AdminShutDown = 1,
            OFF_ClientDrop = 2,
        }

        public enum Status : byte
        {
            OFF = 0,
            ON = 1,
        }



        public MainWindow()
        {
            InitializeComponent();
            LogFileName = "log" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            AlertData.Columns.Add("ID", Type.GetType("System.String"));
            AlertData.Columns.Add("Time", Type.GetType("System.String"));
            AlertData.Columns.Add("Location", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("ID", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("Time", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("Location", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("Status", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("IP", Type.GetType("System.String"));
            ActiveAlertData.Columns.Add("Activity", Type.GetType("System.String"));
            RetrieveAlerts(true, null);
        }

        private void AlertTable_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AlertTable.Columns.Count > 0)
                {
                    AlertTable.Columns[0].Visibility = Visibility.Hidden;
                    AlertTable.Columns[1].Width = 160;
                    AlertTable.Columns[2].Width = 120;
                }
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
        }

        private void ActiveAlertTable_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ActiveAlertTable.Columns.Count > 0)
                {
                    ActiveAlertTable.Columns[0].Visibility = Visibility.Hidden;
                    ActiveAlertTable.Columns[1].Width = 160;
                    ActiveAlertTable.Columns[2].Width = 120;
                    ActiveAlertTable.Columns[3].Width = 80;
                    ActiveAlertTable.Columns[4].Visibility = Visibility.Hidden;
                    ActiveAlertTable.Columns[5].Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
        }

        private void ButtonStartServer_Click(object sender, RoutedEventArgs e)
        {
            ConsoleAddItem("Server is started");
            serverRunning = true;
            Task.Factory.StartNew(() => StartServer());
        }

        private void ButtonStopServer_Click(object sender, RoutedEventArgs e)
        {
            ConsoleAddItem("Server is stopped");
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
            Thread.CurrentThread.Abort();
        }


        private void IstemciThreadi(TcpListener sunucu)
        {
            TcpClient istemci = sunucu.AcceptTcpClient();

            ConsoleAddItem(String.Format("A client connected. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));

            ConsoleAddItem(String.Format("Client connected with IP {0}", (((IPEndPoint)istemci.Client.RemoteEndPoint).Address)));

            NetworkStream yayin = istemci.GetStream();
            string LastID = null;

            //enter to an infinite cycle to be able to handle every change in stream
            while (serverRunning)
            {
                
                try
                {
                    Byte[] gelen_ham_baytlar = new Byte[istemci.Available];
                    yayin.Read(gelen_ham_baytlar, 0, gelen_ham_baytlar.Length);

                    //translate bytes of request to string
                    string gelen_veri = Encoding.UTF8.GetString(gelen_ham_baytlar);
                    if (gelen_veri.Length > 0)
                    {
                        ConsoleAddItem(String.Format("{0}: {1}", Thread.CurrentThread.ManagedThreadId, gelen_veri));
                        if (InsertAlert(gelen_veri, ((IPEndPoint)istemci.Client.RemoteEndPoint).Address.ToString(), out LastID))
                        {
                            ConsoleAddItem(String.Format("Alert info has been inserted to DB successfully!"));
                            RetrieveAlerts(true, null);
                        }
                        else
                        {
                            ConsoleAddItem(String.Format("Alert info could NOT be inserted!!!"));
                            // TODO: What to do if insert fails ?
                        }
                    }
                }
                // Client Drop
                catch (System.IO.IOException)
                {
                    ConsoleAddItem(String.Format("Connection lost. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
                    if(LastID != null)
                    {
                        string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
                        MySqlConnection conn = new MySqlConnection(connStr);
                        try
                        {
                            conn.Open();
                            ConsoleAddItem("DB Connection established to record client drop.");
                            ActivityUpdate(Activity.OFF_ClientDrop, (Int32.Parse(LastID) + 1).ToString(), false, conn);
                            StatusUpdate(Status.OFF, (Int32.Parse(LastID) + 1).ToString(), false, conn);
                            RetrieveAlerts(false, conn);
                            // job's done
                            ConsoleAddItem("Client drop has been recorded successfully");
                        }
                        catch(Exception ex)
                        {
                            ConsoleAddItem("Error while changing status to client drop: " + ex.Message);
                        }
                        if (conn.State == System.Data.ConnectionState.Open)
                        {
                            try
                            {
                                conn.Close();
                                ConsoleAddItem("DB Connection Closed.");
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        ConsoleAddItem("Alert ID is null! Cannot change!");
                    }
                    break;
                }
                // Other Exceptions
                catch (Exception ex)
                {
                    ConsoleAddItem(String.Format("Error on Thread ID {0}: " + ex.Message, Thread.CurrentThread.ManagedThreadId));
                }
            }
            Thread.CurrentThread.Abort();
        }


        private bool InsertAlert(string location, string IP, out string LastID)
        {
            bool result = false;
            LastID = null;
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                String alertTime = DateTime.Now.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("tr-TR"));
                conn.Open();
                ConsoleAddItem("DB Connection established for alert insert!");

                // Get next ID
                MySqlCommand GetID = new MySqlCommand("SELECT COUNT(ID) FROM anan.test", conn);
                LastID = GetID.ExecuteScalar().ToString();
                ConsoleAddItem("Got the last ID: " + LastID);

                // INSERT 
                MySqlCommand InsertAlert = new MySqlCommand(String.Format("INSERT INTO anan.test (Time, Location, Status, IP, Activity) VALUES ('{0}', '{1}', 'ON', '{2}', '{3}')", alertTime, location, IP, Activity.ON_Triggered), conn);
                InsertAlert.ExecuteNonQuery();
                ConsoleAddItem("Insert query was run succesfully!");

                // job's done
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
                }
                catch { }
            }
            return result;
        }

        private void RetrieveAlerts(bool withConnection, MySqlConnection conn)
        {
            ConsoleAddItem("Retrieving alert data from DB.");
            this.Dispatcher.Invoke(() => { AlertTable.ItemsSource = null; });
            AlertData.Rows.Clear();
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            if (withConnection) { conn = new MySqlConnection(connStr); }
            try
            {
                if (withConnection) { conn.Open(); }
                ConsoleAddItem("DB Connection established!");
                MySqlCommand RetrieveAllAlerts = new MySqlCommand(String.Format("SELECT * FROM anan.test ORDER BY ID DESC"), conn);
                ConsoleAddItem("RetrieveAllAlerts Query is executing...");
                using (MySqlDataReader reader = RetrieveAllAlerts.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AlertData.Rows.Add(new object[] { reader["ID"].ToString(), reader["Time"].ToString(), reader["Location"].ToString() });
                    }
                    ConsoleAddItem("All Alert records have been retrieved from DB.");
                }
                this.Dispatcher.Invoke(() => { AlertTable.ItemsSource = AlertData.DefaultView; });
                this.Dispatcher.Invoke(() => { AlertTable_Loaded(new object(), new RoutedEventArgs()); });

                //  Active Alerts
                this.Dispatcher.Invoke(() => { ActiveAlertTable.ItemsSource = null; });
                ActiveAlertData.Rows.Clear();
                MySqlCommand RetrieveActiveAlerts = new MySqlCommand(String.Format("SELECT * FROM anan.test WHERE Status = 'ON' ORDER BY ID DESC"), conn);
                ConsoleAddItem("RetrieveActiveAlerts Query is executing...");
                using (MySqlDataReader reader = RetrieveActiveAlerts.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ActiveAlertData.Rows.Add(new object[] { reader["ID"].ToString(), reader["Time"].ToString(), reader["Location"].ToString(), reader["Status"].ToString(), reader["IP"].ToString(), reader["Activity"].ToString() });
                    }
                    ConsoleAddItem("Active Alert records have been retrieved from DB.");
                }
                this.Dispatcher.Invoke(() => { ActiveAlertTable.ItemsSource = ActiveAlertData.DefaultView; });
                this.Dispatcher.Invoke(() => { ActiveAlertTable_Loaded(new object(), new RoutedEventArgs()); });
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
            if (withConnection && conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch { }
            }
        }

        //  log emekçisi :( tıktıktık
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

        // Active alert table Status column click event
        private void ActiveAlertTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ActiveAlertTable.CurrentColumn.Header.ToString() == "Status")
                {
                    //  store ID
                    string ID = ((DataRowView)ActiveAlertTable.CurrentCell.Item).Row.ItemArray[0].ToString();
                    //  dialog to shut down
                    AlertShutDownDialog win2 = new AlertShutDownDialog();
                    int result = win2.ReturnResult(ID);
                    if (result == 1)
                    {
                        string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
                        MySqlConnection conn = new MySqlConnection(connStr);
                        try
                        {
                            ConsoleAddItem("Alarm shut down mission has started for Alarm #" + ID);
                            conn.Open();
                            ConsoleAddItem("DB Connection established!");

                            // Status Update
                            StatusUpdate(Status.OFF, ID, false, conn);

                            // Activity Update
                            ActivityUpdate(Activity.OFF_AdminShutDown, ID, false, conn);

                            // Refresh Alerts
                            RetrieveAlerts(false, conn);

                        }
                        catch (Exception ex)
                        {
                            ConsoleAddItem("Error while changing alarm status: " + ex.Message);
                        }
                        if (conn.State == System.Data.ConnectionState.Open)
                        {
                            try
                            {
                                conn.Close();
                                ConsoleAddItem("DB Connection Closed.");
                            }
                            catch { }
                        }
                    }
                }
            }
            //  User clicked somewhere else rather than status column, thus do nothing.
           catch(Exception ex)
            {
            }
        }

        //  Update activity column to nextState
        private void ActivityUpdate(Activity nextState, string ID, bool withConnection, MySqlConnection conn)
        {
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            if (withConnection) { conn = new MySqlConnection(connStr); }
            try
            {
                if (withConnection) { conn.Open(); }
                MySqlCommand ActivityUpdate = new MySqlCommand(String.Format("UPDATE anan.test SET Activity='" + nextState + "' WHERE ID=" + ID), conn);
                ConsoleAddItem("Activity Update Query is executing for ID: " + ID);
                int arows = ActivityUpdate.ExecuteNonQuery();
                ConsoleAddItem("Alarm activity has been changed to " + nextState + ". Updated " + arows.ToString() + " rows.");
            }
            catch (Exception ex)
            {
                ConsoleAddItem("Error while changing alarm activity: " + ex.Message);
            }
            if (withConnection && conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch { }
            }
        }

        //  Update status column to ON or OFF
        private void StatusUpdate(Status nextStatus, string ID, bool withConnection, MySqlConnection conn)
        {
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            if (withConnection) { conn = new MySqlConnection(connStr); }
            try
            {
                if (withConnection) { conn.Open(); }
                MySqlCommand ActivityUpdate = new MySqlCommand(String.Format("UPDATE anan.test SET Status='" + nextStatus + "' WHERE ID=" + ID), conn);
                ConsoleAddItem("Status Update Query is executing for ID: " + ID);
                int arows = ActivityUpdate.ExecuteNonQuery();
                ConsoleAddItem("Alarm status has been changed to " + nextStatus + ". Updated " + arows.ToString() + " rows.");
            }
            catch (Exception ex)
            {
                ConsoleAddItem("Error while changing alarm status: " + ex.Message);
            }
            if (withConnection && conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch { }
            }
        }

    }
}
