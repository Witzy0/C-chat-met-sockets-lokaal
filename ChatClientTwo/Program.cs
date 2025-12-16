using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClientTwo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int listenPort = 9002;
            string serverIp = "127.0.0.1";
            int serverPort = 9000; // Update this to match the server's port

            // Start listening for incoming messages
            Thread listenerThread = new Thread(() => Listen(listenPort));
            listenerThread.Start();

            // Register with discovery server
            var peerEp = RegisterWithServer(serverIp, serverPort, listenPort);

            // Chat loop
            while (true)
            {
                Console.Write("Message: ");
                string msg = Console.ReadLine();
                if (peerEp != null)
                    SendMessage(peerEp, msg);
            }
        }

        static IPEndPoint RegisterWithServer(string serverIp, int serverPort, int listenPort)
        {
            try
            {
                TcpClient client = new TcpClient(serverIp, serverPort);
                NetworkStream stream = client.GetStream();
                string myEp = $"{GetLocalIPAddress()}:{listenPort}";
                byte[] data = Encoding.UTF8.GetBytes(myEp);
                stream.Write(data, 0, data.Length);

                // Read peer info
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string peers = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                client.Close();

                Console.WriteLine($"Registered with server. Peers: {peers}");

                if (!string.IsNullOrEmpty(peers))
                {
                    string[] lines = peers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] parts = lines[0].Split(':');
                    return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering with server: {ex.Message}");
            }
            return null;
        }

        static void Listen(int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"\nReceived: {msg}");
                client.Close();
            }
        }

        static void SendMessage(IPEndPoint peerEp, string msg)
        {
            TcpClient client = new TcpClient();
            client.Connect(peerEp);
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
            client.Close();
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "127.0.0.1";
        }
    }
}
