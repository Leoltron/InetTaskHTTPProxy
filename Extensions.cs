using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace InetTaskHTTPProxy
{
    public static class Extensions
    {
        public static IEnumerable<T> TakeLast<T>(this IReadOnlyList<T> list, int amount)
        {
            for (var i = list.Count - amount; i < list.Count; i++)
                yield return list[i];
        }

        public static byte[] ReceiveAllAsync(this TcpClient tcpClient, int timeoutMillis)
        {
            tcpClient.ReceiveTimeout = timeoutMillis;

            int bytesRead;
            var buf = new byte[1024];
            var reply = new List<byte>();
            do
            {
                try
                {
                    bytesRead = tcpClient.Client.Receive(buf);
                    reply.AddRange(buf.Take(bytesRead));
                }
                catch (SocketException)
                {
                    break;
                }
            } while (bytesRead > 0);

            return reply.ToArray();
        }
    }
}