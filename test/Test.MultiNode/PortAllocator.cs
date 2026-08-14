namespace Test.MultiNode
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Allocates free TCP ports on the loopback interface so the temporary stack does not collide
    /// with anything already running on the box. A port is discovered by binding to port 0 (the OS
    /// assigns a free ephemeral port) and immediately releasing it.
    /// </summary>
    public static class PortAllocator
    {
        /// <summary>
        /// Get a single free loopback port.
        /// </summary>
        /// <returns>A free port number.</returns>
        public static int GetFreePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Get several distinct free loopback ports.
        /// </summary>
        /// <param name="count">Number of ports to allocate.</param>
        /// <returns>A list of distinct free ports.</returns>
        public static List<int> GetFreePorts(int count)
        {
            HashSet<int> ports = new HashSet<int>();
            while (ports.Count < count)
            {
                ports.Add(GetFreePort());
            }
            return new List<int>(ports);
        }
    }
}
