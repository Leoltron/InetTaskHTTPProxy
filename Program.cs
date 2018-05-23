using System;

namespace InetTaskHTTPProxy
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var proxy = new AlmostBridgeHttpProxy(30000);
            proxy.Start();
            Console.ReadKey(true);
            proxy.Stop();
        }
    }
}