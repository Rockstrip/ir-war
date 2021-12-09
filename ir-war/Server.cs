﻿using System;
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
        private event Action<byte[]> OnDataReceived;

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
            Console.WriteLine("START CONNECTION");
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

                        OnDataReceived.Invoke(bytes);
                        
                        Console.WriteLine("read data");
                    }
                }
                catch (IOException e)
                {
                    clientStream.Close();
                    tcpClient.Dispose();
                    clientStream.Dispose();
                    Console.WriteLine(e.Message);
                    break;
                }
            }

            Console.WriteLine("END CONNECTION");
        }
        private void SendToAllClients(byte[] data)
        {
            foreach (var stream in networkStreams)
            {
                if (stream.CanWrite)
                {
                    Console.WriteLine("send data");
                    stream.Write(data, 0, sizeof(float) * 3);
                }
            }
        }
    }
}
