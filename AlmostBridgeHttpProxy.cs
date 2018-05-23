using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InetTaskHTTPProxy
{
    public class AlmostBridgeHttpProxy
    {
        private static readonly Regex HttpRegex = new Regex(@"http://(?<url>[^\s\/:]+)", RegexOptions.IgnoreCase);

        private bool isWorking;
        private readonly int port;
        private readonly Task[] tasks;
        private TcpListener listener;

        public AlmostBridgeHttpProxy(int port, int tasksAmount = 10)
        {
            this.port = port;
            tasks = new Task[tasksAmount];
        }

        public void Start()
        {
            if (isWorking) return;
            isWorking = true;
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));

            listener.Start();

            for (var i = 0; i < tasks.Length; i++)
                tasks[i] = HandleClients(listener);
        }

        private async Task HandleClients(TcpListener tcpListener)
        {
            while (isWorking)
            {
                var client = await tcpListener.AcceptTcpClientAsync();
                await HandleClient(client);
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            while (true)
            {
                byte[] message;
                try
                {
                    message = await ReadHttpRequest(client.GetStream());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.GetType().Name + " - " + e.Message);
                    break;
                }

                if (message.Length == 0)
                    break;

                Console.WriteLine("Received message:");
                var decodedMessage = Encoding.ASCII.GetString(message);
                Console.WriteLine(decodedMessage);
                var url = decodedMessage.Split(' ')[1];
                Console.Write(url);

                var httpMatch = HttpRegex.Match(url);

                if (httpMatch.Success)
                {
                    var hostname = httpMatch.Groups["url"].Value;
                    Console.WriteLine(" - " + hostname + " - HTTP - OK");

                    using (var websiteClient = new TcpClient(hostname, 80))
                    {
                        websiteClient.Client.Send(message);
                        var reply = websiteClient.ReceiveAllAsync(500);
                        if (hostname.Equals("solod.zz.mu") && IsHtml(reply))
                        {
                            var encoding = Encoding.GetEncoding("windows-1251");
                            var decoded = encoding.GetString(reply);
                            var bodyTagIndex = decoded.IndexOf("<body>", StringComparison.Ordinal);
                            if (bodyTagIndex >= 0)
                            {
                                decoded = decoded.Substring(0, bodyTagIndex) + "<body>\r\n" + CustomBlock +
                                          decoded.Substring(bodyTagIndex + 6);
                                reply = encoding.GetBytes(decoded);
                            }
                        }

                        client.Client.Send(reply);
                    }
                }
                else
                {
                    Console.WriteLine(" - unsupported protocol.");
                    client.Client.Send(Encoding.ASCII.GetBytes("HTTP/1.0 501 Not Implemented\r\n"));
                    continue;
                }

                Console.WriteLine("------------------");
            }
        }

        private const string CustomBlock = "<style type=\"text/css\">" +
                                           "\r\n\t.block{" +
                                           "\r\n\t\tbackground: #ffff1a;" +
                                           "\r\n\t\twidth: 100%;" +
                                           "\r\n\t\ttext-align: center;" +
                                           "\r\n\t}\r\n</style>" +
                                           "\r\n<div class=\"block\"><font size =\"5\" color=\"blue\" face=\"fantasy\">Что-то что-то что-то банк.</font></div>";

        private static readonly Regex HtmlTagRegex = new Regex(@"<html[^>]*?>");
        private static bool IsHtml(byte[] bytes)
        {
            try
            {
                var bytesDecoded = Encoding.ASCII.GetString(bytes);
                return HtmlTagRegex.IsMatch(bytesDecoded);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Stop()
        {
            isWorking = false;
            listener.Stop();
            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {

            }
        }

        private static readonly byte[] DoubleCrLf = {13, 10, 13, 10};

        private static async Task<byte[]> ReadHttpRequest(NetworkStream stream)
        {
            var byteList = new List<byte>();
            var buf = new byte[1024];
            while (byteList.Count < 4 || !byteList.TakeLast(4).SequenceEqual(DoubleCrLf))
            {
                var bytesRead = await stream.ReadAsync(buf, 0, buf.Length);
                if (bytesRead == 0)
                    break;
                byteList.AddRange(buf.Take(bytesRead));
            }

            return byteList.ToArray();
        }
    }
}