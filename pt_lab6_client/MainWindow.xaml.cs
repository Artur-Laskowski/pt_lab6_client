using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace pt_lab6_client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int userID = 0;
        private int newPort = 0;
        int port = 13131;
        private string server = "127.0.0.1";//"89.69.240.103";//"127.0.0.1";
        private UdpClient myUdpClient;
        private UdpClient myUdpClientA;
        private IPEndPoint remoteIPEndPointA;
        private Task dataReceiver;

        private CancellationTokenSource tokenSource2;

        private bool isConnected;


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            myUdpClient = new UdpClient();
            myUdpClient.Connect(server, port);
            string msg = "Connect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            myUdpClient.Send(data, data.Length);
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                data = myUdpClient.Receive(ref remoteIPEndPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się nawiązać połączenia z serwerem!\n" + ex.Message);
                return;
            }
            msg = Encoding.ASCII.GetString(data, 0, data.Length);
            //myUdpClient.Close();

            newPort = Int32.Parse(msg.Substring(0, 4));
            userID = Int32.Parse(msg.Substring(5, msg.Length - 5));
            label.Content = "Port: " + newPort + " ID: " + userID;

            myUdpClientA = new UdpClient();
            myUdpClientA.Connect(server, newPort);


            tokenSource2 = new CancellationTokenSource();
            CancellationToken ct = tokenSource2.Token;

            dataReceiver = Task.Run(
                () =>
                {
                    ct.ThrowIfCancellationRequested();

                    while (true)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            ct.ThrowIfCancellationRequested();
                        }

                        byte[] dataA;

                        try
                        {
                            remoteIPEndPointA = new IPEndPoint(IPAddress.Any, newPort);
                            dataA = myUdpClientA.Receive(ref remoteIPEndPointA);
                        }
                        catch (Exception exception)
                        {
                            return;
                        }

                        Packet p;

                        IFormatter formatter = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream(dataA))
                        {
                            p = (Packet)formatter.Deserialize(stream);
                            data = stream.ToArray();
                        }
                        if (p.userID != userID)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Line line = new Line();
                                line.Stroke = new SolidColorBrush(Color.FromRgb(p.color[0], p.color[1], p.color[2]));
                                line.X1 = p.points[0];
                                line.Y1 = p.points[1];
                                line.X2 = p.points[2];
                                line.Y2 = p.points[3];
                                paintSurface.Children.Add(line);
                            }));
                        }
                    }
                }, tokenSource2.Token);

            isConnected = true;
            button1.IsEnabled = false;
            button2.IsEnabled = true;

        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var msg = Encoding.ASCII.GetBytes("Disconnect");
            myUdpClient.Send(msg, msg.Length);
            tokenSource2.Cancel();
            myUdpClientA.Close();
            try
            {
                dataReceiver.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var v in ex.InnerExceptions)
                    Console.WriteLine(ex.Message + " " + v.Message);
            }
            finally
            {
                tokenSource2.Dispose();
            }
            isConnected = false;
            button1.IsEnabled = true;
            button2.IsEnabled = false;
        }

        private Point currentPoint;

        private void paintSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);
        }

        private void paintSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Line line = new Line();

                byte[] colors = { Byte.Parse(textBoxR.Text), Byte.Parse(textBoxG.Text), Byte.Parse(textBoxB.Text) };

                Color color = Color.FromRgb(colors[0], colors[1], colors[2]);

                line.Stroke = new SolidColorBrush(color);
                line.X1 = currentPoint.X;
                line.Y1 = currentPoint.Y;
                line.X2 = e.GetPosition(this).X;
                line.Y2 = e.GetPosition(this).Y;

                currentPoint = e.GetPosition(this);

                if (isConnected)
                {
                    double[] points = {line.X1, line.Y1, line.X2, line.Y2};

                    Packet p = new Packet(points, userID, colors);

                    byte[] data;
                    IFormatter formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream())
                    {
                        formatter.Serialize(stream, p);
                        data = stream.ToArray();
                    }

                    myUdpClientA.Send(data, data.Length);
                }
                paintSurface.Children.Add(line);
            }
        }
    }

    [Serializable]
    class Packet
    {
        public double[] points = new double[4];
        public int userID;
        public byte[] color = new byte[3];

        public Packet(double[] points, int userID, byte[] color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                this.points[i] = points[i];
            }
            this.userID = userID;
            this.color[0] = color[0];
            this.color[1] = color[1];
            this.color[2] = color[2];
        }
    }
}
