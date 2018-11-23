using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using System.Runtime.CompilerServices;

namespace Shiny
{

    //  #1 TODO: Research on how to store mysql connection string in a secure way. encrypt or something

    //  #2 TODO: Add filtering functionality to AlertTable

    //  #3 TODO: Add history of alert. Reason is that, most likely we'll need to restrict number of alert shown alert table; may cause performance issues later on.
    //  Might be a window that pops-up with a button

    //  #4 TODO: Make alert table grid columns sortable. However whole row should be sorted, not only clicked column. TSP

    //  #5 TODO: Add a comment field on shut down dialog. It will be optional where some notes could be taken for that specific alarm.
    //  #6 TODO: Make related DB & code changes; add another table column in DB for comment, add new columns while inserting, retrieving alerts

    //  #8 TODO: THE COOL EFECT, when you move your cursor on console, the fadish effect is killer dude! Let's have everywhere xd TSP

    //  #9 TODO: Discuss windowed/fullscreen TSP
    //  Research on dynamic materials/controls
    //  try { If (izi pizi limon sukuizi) { kopipas(ehm) implement it; } else { callme } } catch {callmexd}


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
            ON_ClientDrop = 2,
            ON_Listening = 3,
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
            BringTop.IsChecked = Properties.Settings.Default.BringWindowToTop;
            RetrieveAlerts(true, null);
        }

        private void AlertTable_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AlertTable.Columns.Count > 0)
                {
                    AlertTable.Columns[0].Width = 60;
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
                    ActiveAlertTable.Columns[0].Width = 60;
                    ActiveAlertTable.Columns[1].Width = 160;
                    ActiveAlertTable.Columns[2].Width = 120;
                    ActiveAlertTable.Columns[3].Visibility = Visibility.Hidden;
                    ActiveAlertTable.Columns[4].Visibility = Visibility.Hidden;
                    ActiveAlertTable.Columns[5].Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                ConsoleAddItem(ex.Message);
            }
        }

        private void StartnStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!serverRunning)
            {
                serverRunning = true;
                Task.Factory.StartNew(() => StartServer());
                //StartnStopButton.Content = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Shiny;component/stop.png")), Stretch = Stretch.Uniform, Height = 100, Width = 100 };
                //StartnStopButton_Path.Data = Geometry.Parse("M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M9,9H15V15H9");
                StartnStopButton.Background = Brushes.DarkRed;
                //StartnStopButton_Path.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFAFAFA");
            }
            else
            {
                serverRunning = false;
                //StartnStopButton.Content = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Shiny;component/start.png")), Stretch = Stretch.Uniform, Height = 100, Width = 100 };
                //StartnStopButton_Path.Data = Geometry.Parse("M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z");
                StartnStopButton.Background = Brushes.LimeGreen;
                //StartnStopButton_Path.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFAFAFA");
            }
        }

        private void ConsoleAddItem(string item, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Console.Dispatcher.BeginInvoke(new Action(delegate ()
            {
                //While working with log, line number and member name additions should be considered.
                Console.Items.Add($"{item} Line: {lineNumber} Caller: {caller}");

                //Scroll to the end AUTOMATICALLY, BABY!!!
                if (VisualTreeHelper.GetChildrenCount(Console) > 0)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(Console, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
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
            //const string IP_Adresi = "127.0.0.1"; // kötü demiyom kanki güzel ama hata verdi valla xD edit: tamam ben malım lulxD
            const int Port_No = 5000;
            List<Thread> threadler = new List<Thread>();

            TcpListener sunucu = new TcpListener(IPAddress.Parse(IP_Adresi), Port_No);

            sunucu.Start();
            ConsoleAddItem(String.Format("Server has started on {0}:{1}", IP_Adresi, Port_No));
            SbarMessage(String.Format("Server has started on {0}:{1}", IP_Adresi, Port_No));

            while (serverRunning)
            {
                if (sunucu.Pending())
                {
                    threadler.Add(new Thread(() => IstemciThreadi(sunucu)));
                    threadler[threadler.Count - 1].Start();
                }
                await WaitAsynchronouslyAsync(100);
            }
            sunucu.Stop();
            ConsoleAddItem("Server is stopped");
            SbarMessage("Server is stopped");
            Thread.CurrentThread.Abort();
        }

        private void IstemciThreadi(TcpListener sunucu)
        {
            TcpClient istemci = sunucu.AcceptTcpClient();

            ConsoleAddItem(String.Format("A client connected with IP: {0}. Thread ID: {1}. Inserting alert", ((IPEndPoint)istemci.Client.RemoteEndPoint).Address, Thread.CurrentThread.ManagedThreadId));
            
            string ID = null;
            string gelen_veri = "";
            byte[] bytesToRead = null;
            int bytesRead = 0;

            try
            {
                InsertAlert(((IPEndPoint)istemci.Client.RemoteEndPoint).Address.ToString(), out ID);
                ConsoleAddItem("Alert info has been added to DB!");
            }
            catch(Exception ex)
            {
                ConsoleAddItem("Error while insertion alert: " + ex.Message);
            }

            //enter to an infinite cycle to be able to handle every change in stream
            while (serverRunning)
            {
                try
                {
                    using (NetworkStream yayin = istemci.GetStream())
                    {
                        bytesToRead = new byte[istemci.ReceiveBufferSize];
                        bytesRead = yayin.Read(bytesToRead, 0, istemci.ReceiveBufferSize);
                        gelen_veri = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                        ConsoleAddItem("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
                        if (bytesRead > 0)
                        {
                            ConsoleAddItem(String.Format("{0}: {1}", Thread.CurrentThread.ManagedThreadId, gelen_veri));
                            
                            if (UpdateAlert(gelen_veri, ((IPEndPoint)istemci.Client.RemoteEndPoint).Address.ToString(), ID))
                            {
                                ConsoleAddItem(String.Format("Alert info has been updated to DB successfully!"));
                                RetrieveAlerts(true, null);
                                break;
                            }
                            else
                            {
                                ConsoleAddItem(String.Format("Alert info could NOT be updated!!!"));
                                // TODO: What to do if update fails ?
                            }
                        }
                    }
                }
                // Client Drop
                catch (System.IO.IOException)
                {
                    ConsoleAddItem(String.Format("Connection lost. Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
                    if(ID != null)
                    {
                        string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
                        MySqlConnection conn = new MySqlConnection(connStr);
                        try
                        {
                            conn.Open();
                            ConsoleAddItem("DB Connection established to record client drop.");
                            ActivityUpdate(Activity.ON_ClientDrop, ID, false, conn);
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
                            catch (Exception ex)
                            {
                                ConsoleAddItem($"{ex.Message} You should've never seen this.");
                            }
                        }
                    }
                    else
                    {
                        ConsoleAddItem("Alert ID is null! Cannot change!");
                    }
                    RetrieveAlerts(true, null);
                    break;
                }
                // Other Exceptions
                catch (Exception ex)
                {
                    ConsoleAddItem(String.Format("Error on Thread ID {0}: " + ex.Message, Thread.CurrentThread.ManagedThreadId));
                    RetrieveAlerts(true, null);
                    break;
                }
            }

            // Play sound
            Assembly assembly;
            SoundPlayer sp;
            assembly = Assembly.GetExecutingAssembly();
            using (sp = new SoundPlayer(assembly.GetManifestResourceStream("Shiny.alert.wav")))
            {
                sp.Play();
            }

            // Bring the window to the front or Flash yellow in the taskbar; based on the user's selection.
            if (Properties.Settings.Default.BringWindowToTop)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (WindowState == WindowState.Minimized)
                    {
                        WindowState = WindowState.Normal;
                    }

                    Activate();
                    Topmost = true;
                    Topmost = false;
                    Focus();
                }), DispatcherPriority.ContextIdle);
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    var helper = new FlashWindowHelper(Application.Current);

                    // Flashes the window and taskbar 5 times and stays solid 
                    // colored until user focuses the main window
                    helper.FlashApplicationWindow();
                }), DispatcherPriority.ContextIdle);
            }

            ConsoleAddItem(String.Format("Aborting #{0}", Thread.CurrentThread.ManagedThreadId));
            Thread.CurrentThread.Abort();
        }


        private bool InsertAlert(string IP, out string ID)
        {
            bool result = false;
            ID = null;
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                String alertTime = DateTime.Now.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("tr-TR"));
                conn.Open();
                ConsoleAddItem("DB Connection established for alert insert!");

                // INSERT 
                MySqlCommand InsertAlert = new MySqlCommand(String.Format("INSERT INTO anan.test (Time, Location, Status, IP, Activity) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", alertTime, "unknown", Status.ON, IP, Activity.ON_Triggered), conn);
                InsertAlert.ExecuteNonQuery();
                ConsoleAddItem("Insert query was run succesfully!");

                // Get ID
                MySqlCommand GetID = new MySqlCommand(String.Format("SELECT ID FROM anan.test WHERE Time = '{0}' AND Location = '{1}' AND Status = '{2}' AND IP = '{3}' AND Activity = '{4}'", alertTime, "unknown", Status.ON, IP, Activity.ON_Triggered), conn);
                ID = GetID.ExecuteScalar().ToString();
                ConsoleAddItem("New alert has been added with ID: " + ID);

                // job's done
                result = true;
            }
            catch (Exception ex)
            {
                ConsoleAddItem("Error while inserting: " + ex.Message);
            }
            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch (Exception ex)
                {
                    ConsoleAddItem($"{ex.Message} You should've never seen this.");
                }
            }
            return result;
        }


        private bool UpdateAlert(string location, string IP, string ID)
        {
            bool result = false;
            string connStr = "server=earthpwn.ddns.net;user=anan;database=anan;port=6969;password=anan;SslMode=none"; //global
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ConsoleAddItem("DB Connection established for alert update!");

                // UPDATE 
                MySqlCommand UpdatetAlert = new MySqlCommand(String.Format("UPDATE anan.test SET Location = '{0}', Activity = '{1}' WHERE ID = '{2}' AND IP = '{3}'", location, Activity.ON_Listening, ID, IP), conn);
                UpdatetAlert.ExecuteNonQuery();
                ConsoleAddItem("Location info has been updated.");

                // job's done
                result = true;
            }
            catch (Exception ex)
            {
                ConsoleAddItem("Error while updating: " + ex.Message);
            }
            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch (Exception ex)
                {
                    ConsoleAddItem($"{ex.Message} You should've never seen this.");
                }
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
            if (withConnection && conn.State == ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    ConsoleAddItem("DB Connection Closed.");
                }
                catch (Exception ex)
                {
                    ConsoleAddItem($"{ex.Message} You should've never seen this.");
                }
            }
        }

        //  log emekçisi :( tıktıktık
        //  Emekçi kardeşim :(:( buna asgari mi yatırıyoruz bro?
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
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    bool missionDone = false;
                    //  store ID & IP
                    string ID = ((DataRowView)ActiveAlertTable.CurrentCell.Item).Row.ItemArray[0].ToString();
                    string IP = ((DataRowView)ActiveAlertTable.CurrentCell.Item).Row.ItemArray[4].ToString();
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
                            // Mission: Shut the Alarm Down!   Part 1: Sending Signal to Client
                            try
                            {
                                var client = new TcpClient();
                                var asyncresult = client.BeginConnect(IP, 5001, null, null);
                                var success = asyncresult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                                if (success)
                                {
                                    ConsoleAddItem("Server exist");
                                    using (NetworkStream nwStream = client.GetStream())
                                    {
                                        // YAZ
                                        String textToSend = "1";
                                        byte[] bytesToSend = Encoding.UTF8.GetBytes(textToSend);
                                        nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                                        ConsoleAddItem(">>>>>>>>>>>>> " + textToSend);
                                        // OKU,
                                        byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                                        int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                                        ConsoleAddItem("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
                                        // Verify server understood the command
                                        if (Encoding.ASCII.GetString(bytesToRead, 0, bytesRead) == "k")
                                        {
                                            ConsoleAddItem("Initiating hibernation sequence" /* Tospaa, 10.11.2018 */);
                                            missionDone = true;
                                        }
                                        else
                                        {
                                            ConsoleAddItem("Handshake failed. Skipping the DB update.");
                                        }

                                    }
                                }
                                else { ConsoleAddItem("Server is unreachable. Skipping DB update."); }
                            }
                            catch (SocketException)
                            {
                                ConsoleAddItem(String.Format("Couldn't send the signal to Alarm #{0}. Target is not online. Skipping the DB update.", ID));
                            }
                            catch (IOException)
                            {
                                ConsoleAddItem(String.Format("Couldn't send the signal to Alarm #{0}. Target is not online. Skipping the DB update.", ID));
                            }

                            conn.Open();
                            ConsoleAddItem("DB Connection established!");

                            // Mission accomplished! Update DB
                            if (missionDone)
                            {
                                // Mission: Shut the Alarm Down!   Part 2: Database Conversation

                                // Status Update
                                StatusUpdate(Status.OFF, ID, false, conn);

                                // Activity Update
                                ActivityUpdate(Activity.OFF_AdminShutDown, ID, false, conn);
                            }
                            // Houston, we may have a problem.
                            else
                            {
                                //MessageBox.Show("Alarm deaktif hale getirelemedi.")
                                ConsoleAddItem($"ID: {ID} IP: {IP} - Alarm deaktif hale getirelemedi.");
                                SbarMessage($"ID: {ID} IP: {IP} - Alarm deaktif hale getirelemedi.");
                            }

                            // Refresh Alerts
                            RetrieveAlerts(false, conn);

                        }
                        catch (Exception ex)
                        {
                            ConsoleAddItem("Error while changing alarm status: " + ex.Message);
                        }
                        if (conn.State == ConnectionState.Open)
                        {
                            try
                            {
                                conn.Close();
                                ConsoleAddItem("DB Connection Closed.");
                            }
                            catch (Exception ex)
                            {
                                ConsoleAddItem($"{ex.Message} You should've never seen this.");
                            }
                        }
                    }
                }
            }
        }

        private void ActiveAlertTable_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                grid.SelectedItem = null;
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
                catch (Exception ex)
                {
                    ConsoleAddItem($"{ex.Message} You should've never seen this.");
                }
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
                catch (Exception ex)
                {
                    ConsoleAddItem($"{ex.Message} You should've never seen this.");
                }
            }
        }

        public class FlashWindowHelper
        {
            private IntPtr mainWindowHWnd;
            private Application theApp;

            public FlashWindowHelper(Application app)
            {
                this.theApp = app;
            }

            public void FlashApplicationWindow()
            {
                InitializeHandle();
                Flash(this.mainWindowHWnd, 5);
            }

            public void StopFlashing()
            {
                InitializeHandle();

                if (Win2000OrLater)
                {
                    FLASHWINFO fi = CreateFlashInfoStruct(this.mainWindowHWnd, FLASHW_STOP, uint.MaxValue, 0);
                    FlashWindowEx(ref fi);
                }
            }

            private void InitializeHandle()
            {
                if (this.mainWindowHWnd == IntPtr.Zero)
                {
                    // Delayed creation of Main Window IntPtr as Application.Current passed in to ctor does not have the MainWindow set at that time
                    var mainWindow = this.theApp.MainWindow;
                    this.mainWindowHWnd = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                }
            }

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

            [StructLayout(LayoutKind.Sequential)]
            private struct FLASHWINFO
            {
                /// <summary>
                /// The size of the structure in bytes.
                /// </summary>
                public uint cbSize;
                /// <summary>
                /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
                /// </summary>
                public IntPtr hwnd;
                /// <summary>
                /// The Flash Status.
                /// </summary>
                public uint dwFlags;
                /// <summary>
                /// The number of times to Flash the window.
                /// </summary>
                public uint uCount;
                /// <summary>
                /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
                /// </summary>
                public uint dwTimeout;
            }

            /// <summary>
            /// Stop flashing. The system restores the window to its original stae.
            /// </summary>
            public const uint FLASHW_STOP = 0;

            /// <summary>
            /// Flash the window caption.
            /// </summary>
            public const uint FLASHW_CAPTION = 1;

            /// <summary>
            /// Flash the taskbar button.
            /// </summary>
            public const uint FLASHW_TRAY = 2;

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
            /// </summary>
            public const uint FLASHW_ALL = 3;

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            public const uint FLASHW_TIMER = 4;

            /// <summary>
            /// Flash continuously until the window comes to the foreground.
            /// </summary>
            public const uint FLASHW_TIMERNOFG = 12;

            /// <summary>
            /// Flash the spacified Window (Form) until it recieves focus.
            /// </summary>
            /// <param name="hwnd"></param>
            /// <returns></returns>
            public static bool Flash(IntPtr hwnd)
            {
                // Make sure we're running under Windows 2000 or later
                if (Win2000OrLater)
                {
                    FLASHWINFO fi = CreateFlashInfoStruct(hwnd, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);

                    return FlashWindowEx(ref fi);
                }
                return false;
            }

            private static FLASHWINFO CreateFlashInfoStruct(IntPtr handle, uint flags, uint count, uint timeout)
            {
                FLASHWINFO fi = new FLASHWINFO();
                fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
                fi.hwnd = handle;
                fi.dwFlags = flags;
                fi.uCount = count;
                fi.dwTimeout = timeout;
                return fi;
            }

            /// <summary>
            /// Flash the specified Window (form) for the specified number of times
            /// </summary>
            /// <param name="hwnd">The handle of the Window to Flash.</param>
            /// <param name="count">The number of times to Flash.</param>
            /// <returns></returns>
            public static bool Flash(IntPtr hwnd, uint count)
            {
                if (Win2000OrLater)
                {
                    FLASHWINFO fi = CreateFlashInfoStruct(hwnd, FLASHW_ALL | FLASHW_TIMERNOFG, count, 0);

                    return FlashWindowEx(ref fi);
                }

                return false;
            }

            /// <summary>
            /// A boolean value indicating whether the application is running on Windows 2000 or later.
            /// </summary>
            private static bool Win2000OrLater
            {
                get { return Environment.OSVersion.Version.Major >= 5; }
            }
        }
        
        private void BringTop_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BringWindowToTop = BringTop.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        public void SbarMessage(string message)
        {
            this.Dispatcher.Invoke(() => {
            sbar.IsActive = true;
            sbarMsg.Content = message;
            });
        }

        private void SbarMsg_ActionClick(object sender, RoutedEventArgs e)
        {
            sbar.IsActive = false;
        }
    }
}
