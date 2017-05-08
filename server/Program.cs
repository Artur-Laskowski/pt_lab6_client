using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace pt_lab6_server
{
    internal static class Program
    {
        private const int connectionPort = 13131;
        private const int drawingPort = 1337;
        private static void Main()
        {
            UdpClient myUdpClient;
            UdpClient myUdpClientA;
            try
            {
                var localIPEndPoint = new IPEndPoint(IPAddress.Any, connectionPort);
                myUdpClient = new UdpClient(localIPEndPoint);
                var localIPEndPointA = new IPEndPoint(IPAddress.Any, drawingPort);
                myUdpClientA = new UdpClient(localIPEndPointA);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            //IP klientów oraz ich ID
            Dictionary<IPEndPoint, byte[]> clients = new Dictionary<IPEndPoint, byte[]>();

            //kolejka danych (domyślny typ struktury)
            BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();

            //BlockingCollection<Dictionary<IPEndPoint, byte[]>> clients = new BlockingCollection<Dictionary<IPEndPoint, byte[]>>();
           
            //Wątek odbierający dane rysownicze
            Task dataReceiver = Task.Run(
                () =>
                {
                    //byte[] color = {0, 0, 0};
                    while (true)
                    {
                        var remoteIPEndPointA = new IPEndPoint(IPAddress.Any, 1337);
                        byte[] dataA;
                        try
                        {
                            dataA = myUdpClientA.Receive(ref remoteIPEndPointA);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            continue;
                        }

                        //
                        if (dataA.Length == 3)
                        {
                            Console.WriteLine("{0}> Started drawing", remoteIPEndPointA);
                            
                            dataA.CopyTo(clients[remoteIPEndPointA], 1);
                            continue;
                        }

                        var z = new byte[dataA.Length + clients[remoteIPEndPointA].Length];
                        dataA.CopyTo(z, 0);
                        clients[remoteIPEndPointA].CopyTo(z, dataA.Length);

                        queue.Add(z);
                    }
                }
            );

            Task dataSender = Task.Run(
                () =>
                {
                    while (true)
                    {
                        if (queue.Count == 0) continue;
                        var packet = queue.Take();
                        foreach (var client in clients)
                        {
                            myUdpClientA.SendAsync(packet, packet.Length, client.Key);
                        }
                    }
                }
            );
            while (true)
            {
                var remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = myUdpClient.Receive(ref remoteIPEndPoint);
                var msg = Encoding.ASCII.GetString(data, 0, data.Length);

                if (msg == "Connect")
                {
                    byte userID = 0;
                    for (byte i = 0; i < byte.MaxValue; i++)
                    {
                        /*if (!clients.ContainsValue(i))
                        {
                            userID = i;
                            break;
                        }*/
                        bool flag = false;
                        foreach (var c in clients)
                        {
                            if (c.Value[0] == i)
                            {
                                flag = true;
                            }
                        }
                        if (!flag)
                        {
                            userID = i;
                            break;
                        }
                    }

                    byte[] answer = {drawingPort / byte.MaxValue, drawingPort % byte.MaxValue, userID};
                    myUdpClient.SendAsync(answer, answer.Length, remoteIPEndPoint);

                    Console.WriteLine("{0}> {1}", remoteIPEndPoint, msg);

                    remoteIPEndPoint.Port++;
                    clients.Add(remoteIPEndPoint, new byte[] {userID, 0,0,0});
                }
                if (msg == "Disconnect")
                {
                    Console.WriteLine("{0}> {1}", remoteIPEndPoint, msg);

                    remoteIPEndPoint.Port++;
                    var res = clients.Remove(remoteIPEndPoint);
                    Console.WriteLine("Attempting to remove {0} - result: {1}", remoteIPEndPoint, res);
                }
            }
        }
    }
}