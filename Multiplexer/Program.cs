using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Multiplexer
{
    class Program
    {
        static BlockingCollection<byte[]> UploadQueue = new BlockingCollection<byte[]>();
        static ConcurrentDictionary<TcpClient, BlockingCollection<byte[]>> DownloadQueues = new ConcurrentDictionary<TcpClient, BlockingCollection<byte[]>>();

        static void Main(string[] args)
        {
            Task.Run(() => HandleControlChannel((host, port) =>
            {
                StartServer(host, port);
            }));

            IPAddress localIp = IPAddress.Parse("127.0.0.1");
            int localPort = 3333;

            var localserver = new TcpListener(localIp, localPort);
            localserver.Start();
            while (true)
            {
                Console.WriteLine("Waiting for clients to connect...");
                var client = localserver.AcceptTcpClient();
                Console.WriteLine("Client connected: " + client.ToString());

                var clientQueue = new BlockingCollection<byte[]>();
                DownloadQueues[client] = clientQueue;
                var stream = client.GetStream();

                // FIXME: how to properly handle client disposal?
                // local client uplink task. Read data from client and put into queue
                Task.Run(() =>
                {
                    int c;
                    byte[] buffer = new byte[256];
                    while ((c = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        UploadQueue.Add(buffer.Take(c).ToArray());
                    }

                    BlockingCollection<byte[]> q;
                    bool removed = DownloadQueues.TryRemove(client, out q);
                    Console.WriteLine($"Local connection is closed: {client}. Client's download queue removed: {removed}.");
                });

                // local client downlink task. Get data from queue and write to client
                Task.Run(() =>
                {
                    byte[] data;
                    while (null != (data = clientQueue.Take()))
                    {
                        stream.Write(data, 0, data.Length);
                    }
                });
            }
        }

        static void HandleControlChannel(Action<string, int> handleConnect)
        {
            while (true)
            {
                var line = Console.ReadLine();

                var toks = line.Split();
                switch (toks[0])
                {
                    case "connect":
                        try
                        {
                            handleConnect(
                                toks[1],
                                int.Parse(toks[2]));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }

                        break;
                    default:
                        Console.WriteLine("Unknown command: " + line);
                        break;
                }
            }
        }

        static Tuple<Task, Task> StartServer(string hostname, int port)
        {
            var remote = new TcpClient(hostname, port);
            var remoteStream = remote.GetStream();

            // remote download task
            var download = Task.Run(() =>
            {
                int c;
                byte[] buffer = new byte[256];
                while ((c = remoteStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    foreach (var q in DownloadQueues)
                    {
                        q.Value.Add(buffer.Take(c).ToArray());
                    }
                }

                Console.WriteLine("Remote connection is closed");
            });

            // remote upload task
            var upload = Task.Run(() =>
            {
                byte[] data;
                while (null != (data = UploadQueue.Take()))
                {
                    remoteStream.Write(data, 0, data.Length);
                }
            });

            return Tuple.Create(download, upload);
        }
    }
}
