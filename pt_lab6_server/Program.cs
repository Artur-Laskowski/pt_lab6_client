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

            //HashSet<Tuple<IPEndPoint, byte>> clients = new HashSet<Tuple<IPEndPoint, byte>>();
            Dictionary<IPEndPoint, byte> clients = new Dictionary<IPEndPoint, byte>();

            byte userID = 0;

            BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();

            byte[] data;
            string msg;

            Task dataReceiver = Task.Run(
                () =>
                {
                    byte[] color = {0,0,0};
                    while (true)
                    {
                        byte[] z;

                        var remoteIPEndPointA = new IPEndPoint(IPAddress.Any, 1337);
                        var dataA = myUdpClientA.Receive(ref remoteIPEndPointA);
                        if (dataA.Length == 3)
                        {
                            Console.WriteLine("Received color info!");
                            dataA.CopyTo(color, 0);
                            continue;
                        }

                        z = new byte[dataA.Length + color.Length + 1];
                        dataA.CopyTo(z, 0);
                        color.CopyTo(z, dataA.Length);
                        z[z.Length - 1] = clients[remoteIPEndPointA];

                        //Console.WriteLine("{0}> {1}", remoteIPEndPointA, "receiving packets");
                        //Console.Write("V");
                        queue.Add(z);
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
                            foreach (var client in clients)
                            {
                                //Console.WriteLine("{0}> {1}", client, "sending packets");
                                //Console.Write("^");
                                myUdpClientA.SendAsync(packet, packet.Length, client.Key);
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

                    Console.WriteLine("{0}> {1}", remoteIPEndPoint, msg);

                    remoteIPEndPoint.Port++;
                    clients.Add(remoteIPEndPoint, userID);
                    userID++;
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
