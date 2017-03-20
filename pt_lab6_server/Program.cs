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

            HashSet<IPEndPoint> ppl = new HashSet<IPEndPoint>();

            int userID = 0;

            BlockingCollection<byte> queue2 = new BlockingCollection<byte>();

            byte[] data;
            string msg;

            Task dataReceiver = Task.Run(
                () =>
                {
                    while (true)
                    {
                        var remoteIPEndPointA = new IPEndPoint(IPAddress.Any, 1337);
                        var dataA = myUdpClientA.Receive(ref remoteIPEndPointA);

                        //ppl.Add(remoteIPEndPointA);

                        //var msgA = Encoding.ASCII.GetString(dataA, 0, dataA.Length);
                        //int id = Int32.Parse(msgA.Substring(0, msgA.IndexOf(";")));

                        //dataA = Encoding.ASCII.GetBytes(msgA);
                        foreach (IPEndPoint dude in ppl)
                        {
                            Console.WriteLine("{0}> {1}", dude, "sending packets");
                            myUdpClientA.Send(dataA, dataA.Length, dude);
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

/*
                    remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    //remoteIPEndPoint.Port = 1337;
                    data = myUdpClient.Receive(ref remoteIPEndPoint);
                    Console.WriteLine("{0}> {1}", remoteIPEndPoint, Encoding.ASCII.GetString(data, 0, data.Length));
*/

                    remoteIPEndPoint.Port++;
                    ppl.Add(remoteIPEndPoint);

                }
            }
        }
    }
}
