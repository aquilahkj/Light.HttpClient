using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Light.HttpClient
{
	public class HttpWebClient:IDisposable
	{
		static Regex IPREGEX = new Regex ("^((?:(?:25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d)))\\.){3}(?:25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d))))$");

		IPEndPoint requestEndPoint = null;
		HttpConnectionPool connectionPool = null;
		string host = null;
		int port = 0;

		public HttpWebClient (Uri httpHostUri, HttpConfig config)
		{
			if (httpHostUri == null)
				throw new ArgumentNullException ("httpHostUri");
			if (httpHostUri.Scheme != "http") {
				throw new Exception (httpHostUri + " not http uri");
			}
			Inernal (httpHostUri.Host, httpHostUri.Port, config);
		}

		public HttpWebClient (string host, HttpConfig config) : this (host, 80, config)
		{

		}

		public HttpWebClient (string host, int port, HttpConfig config)
		{
			if (string.IsNullOrEmpty (host))
				throw new ArgumentNullException ("host");
			if (port <= 0 || port > 65535) {
				throw new ArgumentOutOfRangeException ("port");
			}
			Inernal (host, port, config);
		}

		IPAddress ParseIPAddree (string host)
		{
			if (IPREGEX.IsMatch (host)) {
				IPAddress address;
				if (IPAddress.TryParse (host, out address)) {
					return address;
				}
				else {
					throw new Exception (string.Format ("ip host {0} error exist", host));
				}
			}
			else {
				IPHostEntry hostIp = Dns.GetHostEntry (host);
				if (hostIp.AddressList == null || hostIp.AddressList.Length == 0) {
					throw new Exception (string.Format ("domain host {0} not exist", host));
				}
				else {
					return hostIp.AddressList [0];
				}
			}
		}

		void Inernal (string host, int port, HttpConfig config)
		{
			IPAddress address = ParseIPAddree (host);
			this.requestEndPoint = new IPEndPoint (address, port);
			this.connectionPool = new HttpConnectionPool (this.requestEndPoint, config);
			this.host = host;
			this.port = port;
		}

		public IAsyncResult BeginSendRequest (HttpRequestPackage request, AsyncCallback callback, object @object)
		{
			return BeginSendRequest (request, null, callback, @object);
		}

		public IAsyncResult BeginSendRequest (HttpRequestPackage request, BufferData responseBuffer, AsyncCallback callback, object @object)
		{
			if (request == null)
				throw new ArgumentNullException ("request");
			Uri uri = request.RequestUri;
			if (uri.Scheme != "http" || uri.Host != this.host || uri.Port != this.port) {
				throw new ArgumentException ("request");
			}
			HttpConnection connection = this.connectionPool.GetConnection ();
			if (connection == null) {
				throw new HttpException ("connection pool is overload", HttpExceptionStatus.ConnectionPoolOverload);
			}
			if (responseBuffer == null)
				responseBuffer = new BufferData ();
			HttpAsyncResult result = new HttpAsyncResult (connection, callback, @object);
			connection.SendRequest (request, responseBuffer);
			return result;
		}

		public HttpResponsePackage EndSendRequest (IAsyncResult ar)
		{
			HttpAsyncResult result = (HttpAsyncResult)ar;
			result.SetEnd ();
			if (!result.IsCompleted) {
				return null;
			}
			else {
				result.Clear ();
				if (result.GoException) {
					throw result.InnerException;
				}
				else {
					return result.Response;
				}
			}
		}

		public override string ToString ()
		{
			return this.connectionPool.ToString ();
		}

		#region IDisposable implementation

		bool disposed;

		~HttpWebClient ()
		{
			//必须为false
			Dispose (false);
		}

		public void Dispose ()
		{
			//必须为true
			Dispose (true);
			//通知垃圾回收机制不再调用终结器（析构器）
			GC.SuppressFinalize (this);
		}

		/// <summary>
		/// 非密封类修饰用protected virtual
		/// 密封类修饰用private
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose (bool disposing)
		{
			if (disposed) {
				return;
			}
			disposed = true;
			if (disposing) {
				// 清理托管资源
				//				if (managedResource != null)
				//				{
				//					managedResource.Dispose();
				//					managedResource = null;
				//				}

			}
			if (this.connectionPool != null) {
				this.connectionPool.Dispose ();
			}
			// 清理非托管资源
			//			if (nativeResource != IntPtr.Zero)
			//			{
			//				Marshal.FreeHGlobal(nativeResource);
			//				nativeResource = IntPtr.Zero;
			//			}
			//让类型知道自己已经被释放

		}

		#endregion
	}
}

