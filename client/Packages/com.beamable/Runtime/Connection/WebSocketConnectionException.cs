using System;

namespace Connection
{
	public class WebSocketConnectionException : Exception
	{
		public WebSocketConnectionException(string message) : base(message) { }
	}
}
