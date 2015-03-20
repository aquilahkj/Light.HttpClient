using System;
using System.Net.Sockets;

namespace Light.HttpClient
{
	public delegate Boolean SocketAsyncMethod (SocketAsyncEventArgs args);

	public static class ExtensionMethods
	{
		public static void InvokeAsyncMethod (this Socket socket, SocketAsyncMethod method, EventHandler<SocketAsyncEventArgs> callback, SocketAsyncEventArgs args)
		{
			try {
				if (!method (args)) {
					callback (socket, args);
				}
			}
			catch (Exception e) {
				args.SocketError = SocketError.NotConnected;
				callback (socket, args);
			}
		}
	}
}

