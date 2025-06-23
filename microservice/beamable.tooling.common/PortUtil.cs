using System.Net;
using System.Net.Sockets;

namespace Beamable.Server.Common;

/// <summary>
/// A utility class that offers the <see cref="FreeTcpPort"/> method
/// </summary>
public class PortUtil
{
	/// <summary>
	/// Asks the system to give us back the first port that is free. This method does
	/// not _reserve_ the port, so it is still possible the port may be bound before you can use it. 
	/// </summary>
	/// <returns></returns>
	public static int FreeTcpPort()
	{
		TcpListener l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		int port = ((IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}

	/// <summary>
	/// This is mainly to get a local endpoint with a free port to use
	/// </summary>
	/// <returns></returns>
	public static string FreeEndpoint()
	{
		return $"http://localhost:{PortUtil.FreeTcpPort()}"; //TODO should have a better way to get the local url
	}
}
