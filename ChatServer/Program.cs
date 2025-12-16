using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Chatserver
{
    internal class Program
    {
        static List<IPEndPoint> clients = new List<IPEndPoint>();
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 9000);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            Console.WriteLine("Discovery server started on port 9000.");
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            // Read client endpoint info
            byte[] buffer = new byte[256];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string endpointStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] parts = endpointStr.Split(':');
            IPEndPoint clientEp = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));

            lock (clients)
            {
                if (!clients.Exists(ep => ep.ToString() == clientEp.ToString()))
                    clients.Add(clientEp);
            }

            // Send all other clients to this client
            List<IPEndPoint> peers;
            lock (clients)
            {
                peers = new List<IPEndPoint>(clients);
                peers.RemoveAll(ep => ep.ToString() == clientEp.ToString());
            }

            StringBuilder sb = new StringBuilder();
            foreach (var ep in peers)
            {
                sb.AppendLine($"{ep.Address}:{ep.Port}");
            }
            byte[] peerData = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(peerData, 0, peerData.Length);

            client.Close();
        }
    }
}
