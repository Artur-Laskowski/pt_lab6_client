using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace pt_lab6_client
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Color color;

        private Point currentPoint;
        private Task dataReceiver;

        private bool isConnected;
        private UdpClient myUdpClient;
        private UdpClient myUdpClientA;
        private int newPort;
        private readonly int port = 13131;
        private IPEndPoint remoteIPEndPointA;
        private readonly string server = "127.0.0.1";

        private CancellationTokenSource tokenSource2;

        private int userID;

        public MainWindow()
        {
            InitializeComponent();

            //Ustawienie losowego koloru na starcie programu
            var rand = new Random();
            sliderR.Value = rand.Next(255);
            sliderG.Value = rand.Next(255);
            sliderB.Value = rand.Next(255);

            updateColor();
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //Próba nawiązania połączenia z serwerem
            myUdpClient = new UdpClient();
            myUdpClient.Connect(server, port);
            string msg = "Connect";
            byte[] data = Encoding.ASCII.GetBytes(msg);

            try
            {
                myUdpClient.SendAsync(data, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);

            //Czekamy na odpowiedź
            try
            {
                data = myUdpClient.Receive(ref remoteIPEndPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to server!\n" + ex.Message);
                return;
            }

            //odczytanie portu do rysowania oraz ID użytkownika
            newPort = data[0] * byte.MaxValue + data[1];
            userID = data[2];

            label.Content = "Online, ID: " + userID;

            //nawiązanie połączenia na porcie rysowniczym
            try
            {
                myUdpClientA = new UdpClient();
                myUdpClientA.Connect(server, newPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }


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
                            ct.ThrowIfCancellationRequested();
                        }

                        byte[] dataA;

                        //Czekamy na dane od serwera
                        try
                        {
                            Console.WriteLine(IPAddress.Any.ToString());
                            remoteIPEndPointA = new IPEndPoint(IPAddress.Broadcast, newPort);
                            dataA = myUdpClientA.Receive(ref remoteIPEndPointA);
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message);
                            return;
                        }

                        Packet p;

                        //odczytujemy ID użytkownika
                        byte receivedUserID = dataA[dataA.Length - 4];

                        //odczytujemy wartość koloru
                        byte[] receivedColor =
                        {
                            dataA[dataA.Length - 3], dataA[dataA.Length - 2],
                            dataA[dataA.Length - 1]
                        };

                        //przepisujemy dane punktów do deserializacji
                        byte[] packetBytes = new byte[dataA.Length - 4];
                        Array.Copy(dataA, packetBytes, dataA.Length - 4);

                        //deserializacja punktów
                        IFormatter formatter = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream(dataA))
                        {
                            p = (Packet) formatter.Deserialize(stream);
                            data = stream.ToArray();
                        }

                        //Jeśli nie są to dane wysłane przez nas to rysujemy
                        if (receivedUserID != userID)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Line line = new Line
                                {
                                    Stroke = new SolidColorBrush(Color.FromRgb(receivedColor[0], receivedColor[1],
                                        receivedColor[2])),
                                    X1 = p.points[0],
                                    Y1 = p.points[1],
                                    X2 = p.points[2],
                                    Y2 = p.points[3]
                                };
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
            disconnect();
        }

        private void paintSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);

            byte[] colors = {(byte)sliderR.Value, (byte)sliderG.Value, (byte)sliderB.Value};

            //Wysyłamy informację o kolorze tylko po rozpoczęciu rysowania linii
            color = Color.FromRgb(colors[0], colors[1], colors[2]);
            if (isConnected)
            {
                try
                {
                    myUdpClientA.SendAsync(colors, colors.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void paintSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Line line = new Line
            {
                Stroke = new SolidColorBrush(color),
                X1 = currentPoint.X,
                Y1 = currentPoint.Y,
                X2 = e.GetPosition(this).X,
                Y2 = e.GetPosition(this).Y
            };


            currentPoint = e.GetPosition(this);

            //Jeśli jesteśmy połączeni, wysyłamy dane rysownicze
            if (isConnected)
            {
                double[] points = {line.X1, line.Y1, line.X2, line.Y2};

                Packet p = new Packet(points);

                //Serializujemy pakiet z punktami
                byte[] data;
                IFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, p);
                    data = stream.ToArray();
                }

                try
                {
                    myUdpClientA.SendAsync(data, data.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            paintSurface.Children.Add(line);
        }

        private void updateColor(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            updateColor();
        }

        private void updateColor()
        {
            sliderR.Background = new SolidColorBrush(Color.FromRgb(255, (byte) (255 - sliderR.Value), (byte) (255 - sliderR.Value)));
            sliderG.Background = new SolidColorBrush(Color.FromRgb((byte)(255 - sliderG.Value), 255, (byte)(255 - sliderG.Value)));
            sliderB.Background = new SolidColorBrush(Color.FromRgb((byte)(255 - sliderB.Value), (byte)(255 - sliderB.Value), 255));

            color = Color.FromRgb((byte) sliderR.Value, (byte) sliderG.Value, (byte) sliderB.Value);
            colorPreview.Background = new SolidColorBrush(color);
        }

        private void disconnect()
        {
            var msg = Encoding.ASCII.GetBytes("Disconnect");
            try
            {
                myUdpClient.SendAsync(msg, msg.Length);
                tokenSource2.Cancel();
                myUdpClientA.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            try
            {
                dataReceiver.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var v in ex.InnerExceptions)
                    Console.WriteLine(ex.Message + ' ' + v.Message);
            }
            finally
            {
                tokenSource2.Dispose();
            }
            isConnected = false;
            button1.IsEnabled = true;
            button2.IsEnabled = false;
            label.Content = "Offline";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            disconnect();
        }
    }

    [Serializable]
    internal struct Packet
    {
        public readonly double[] points;

        public Packet(double[] points)
        {
            this.points = new double[4];
            points.CopyTo(this.points, 0);
        }
    }
}