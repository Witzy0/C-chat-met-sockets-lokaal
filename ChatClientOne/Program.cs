using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClientOne
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int listenPort = 9001;
            string serverIp = "127.0.0.1";
            int serverPort = 9000;

            Console.WriteLine($"Starting ChatClientOne on port {listenPort}...");

            // Start listening for incoming messages
            Thread listenerThread = new Thread(() => Listen(listenPort));
            listenerThread.Start();

            // Register with discovery server
            var peerEp = RegisterWithServer(serverIp, serverPort, listenPort);

            // Start een thread om periodiek peers op te halen totdat er een geldige lijst is
            Thread peerUpdateThread = new Thread(() =>
            {
                while (peerEp == null) // Blijf peers ophalen totdat er een geldige lijst is
                {
                    peerEp = RegisterWithServer(serverIp, serverPort, listenPort);
                    if (peerEp == null)
                    {
                        Console.WriteLine("No peers found yet. Retrying...");
                    }
                    Thread.Sleep(5000); // Wacht 5 seconden voordat je opnieuw registreert
                }
                Console.WriteLine($"Connected to peer: {peerEp}");
            });
            peerUpdateThread.Start();

            // Chat loop
            while (true)
            {
                Console.Write("Message: ");
                string msg = Console.ReadLine();
                if (peerEp != null)
                {
                    SendMessage(peerEp, msg);
                }
                else
                {
                    Console.WriteLine("No peer available to send the message.");
                }
            }
        }

        static IPEndPoint RegisterWithServer(string serverIp, int serverPort, int listenPort)
        {
            try
            {
                Console.WriteLine($"Connecting to server at {serverIp}:{serverPort}...");
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
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"Listening for incoming messages on port {port}...");

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Listen: {ex.Message}");
            }
        }

        static void SendMessage(IPEndPoint peerEp, string msg)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(peerEp);
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(msg);
                stream.Write(data, 0, data.Length);
                client.Close();
                Console.WriteLine($"Message sent to {peerEp}: {msg}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
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
