using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pt_lab6_server
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint localIPEndPoint = new IPEndPoint(IPAddress.Any, 13131);
            UdpClient myUdpClient = new UdpClient(localIPEndPoint);
            IPEndPoint localIPEndPointA = new IPEndPoint(IPAddress.Any, 1337);
            UdpClient myUdpClientA = new UdpClient(localIPEndPointA);
            IPEndPoint remoteIPEndPoint;

            HashSet<IPEndPoint> clients = new HashSet<IPEndPoint>();

            int userID = 0;

            BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();

            byte[] data;
            string msg;

            Task dataReceiver = Task.Run(
                () =>
                {
                    while (true)
                    {
                        var remoteIPEndPointA = new IPEndPoint(IPAddress.Any, 1337);
                        var dataA = myUdpClientA.Receive(ref remoteIPEndPointA);
                        Console.WriteLine("{0}> {1}", remoteIPEndPointA, "receiving packets");
                        queue.Add(dataA);
                    }
                }
            );

            Task dataSender = Task.Run(
                () =>
                {
                    while (true)
                    {
                        if (queue.Count != 0)
                        {
                            var packet = queue.Take();
                            foreach (IPEndPoint client in clients)
                            {
                                Console.WriteLine("{0}> {1}", client, "sending packets");
                                myUdpClientA.SendAsync(packet, packet.Length, client);
                            }
                        }
                    }
                }
            );
            while (true)
            {
                remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                data = myUdpClient.Receive(ref remoteIPEndPoint);
                msg = Encoding.ASCII.GetString(data, 0, data.Length);

                if (msg == "Connect")
                {
                    byte[] answer = Encoding.ASCII.GetBytes("1337 " + userID);
                    myUdpClient.SendAsync(answer, answer.Length, remoteIPEndPoint);
                    userID++;

                    Console.WriteLine("{0}> {1}", remoteIPEndPoint, msg);

                    remoteIPEndPoint.Port++;
                    clients.Add(remoteIPEndPoint);

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
