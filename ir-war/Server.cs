using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ir_war
{
    public class Server
    {
        private TcpListener tcplistener;
        private Thread listenThread;
        private ConcurrentBag<NetworkStream> networkStreams;
        private event Action<string> OnDataReceived;

        public Server()
        {
            tcplistener = new TcpListener(IPAddress.Any, 3000);
            listenThread = new Thread(ListenForClients);
            networkStreams = new ConcurrentBag<NetworkStream>();
            OnDataReceived += SendToAllClients;
        }
        public void Start() => listenThread.Start();
        private void ListenForClients()
        {
            tcplistener.Start();
            while (true)
            {
                TcpClient client = tcplistener.AcceptTcpClient();

                Thread clientThread = new Thread(ClientProcess);
                clientThread.Start(client);
            }
        }
        private void ClientProcess(object? client)
        {
            TcpClient tcpClient = (TcpClient) client!;
            NetworkStream clientStream = tcpClient.GetStream();
            networkStreams.Add(clientStream);

            while (true)
            {
                try
                {
                    if (clientStream.CanRead)
                    {
                        byte[] bytes = new byte[sizeof(float) * 3];
                        clientStream.Read(bytes, 0, sizeof(float) * 3);

                        string returnData = Encoding.UTF8.GetString(bytes);
                        OnDataReceived.Invoke(returnData);
                        Console.WriteLine(returnData);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
            }
        }
        private void SendToAllClients(string data)
        {
            foreach (var stream in networkStreams)
            {
                if (stream.CanWrite)
                {
                    var mes = Encoding.UTF8.GetBytes(data);
                    stream.Write(mes, 0, sizeof(float) * 3);
                }
            }
        }
    }
}
