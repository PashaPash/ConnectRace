using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ConnectRace
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = Connect("github.com", 443, TimeSpan.FromMilliseconds(1000));

            Console.WriteLine("Please wait...");

            Console.ReadLine();
            socket.Blocking = false;
        }

        private static Socket Connect(string serverName, int port, TimeSpan timeout)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(serverName);
            IPAddress serverIPv4 = null;
            IPAddress serverIPv6 = null;
            foreach (IPAddress ipAddress in ipAddresses)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    serverIPv4 = ipAddress;
                }
                else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    serverIPv6 = ipAddress;
                }
            }
            ipAddresses = new IPAddress[] { serverIPv4, serverIPv6 };
            Socket[] sockets = new Socket[2];

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            void Cancel()
            {
                for (int i = 0; i < sockets.Length; ++i)
                {
                    try
                    {
                        if (sockets[i] != null && !sockets[i].Connected)
                        {
                            // --------------
                            Thread.Sleep(5000); // just a random lag
                            Console.WriteLine("Press enter to continue");
                            // --------------

                            sockets[i].Dispose();
                            sockets[i] = null;
                        }
                    }
                    catch { }
                }
            }
            cts.Token.Register(Cancel);

            Socket availableSocket = null;
            for (int i = 0; i < sockets.Length; ++i)
            {
                try
                {
                    if (ipAddresses[i] != null)
                    {
                        sockets[i] = new Socket(ipAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        // --------------
                        Thread.Sleep(2000); // Emulate slow connect
                        //---------------
                        sockets[i].Connect(ipAddresses[i], port);
                        
                        if (sockets[i] != null) // sockets[i] can be null if cancel callback is executed during connect()
                        {
                            if (sockets[i].Connected)
                            {
                                availableSocket = sockets[i];
                                break;
                            }
                            else
                            {
                                sockets[i].Dispose();
                                sockets[i] = null;
                            }
                        }
                    }
                }
                catch { }
            }

            return availableSocket;
        }
    }
}
