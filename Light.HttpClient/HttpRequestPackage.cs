using System;
using System.Text;

namespace Light.HttpClient
{
	public class HttpRequestPackage
	{
		public static HttpRequestPackage CreateGetRequest (Uri requestUri)
		{
			HttpRequestPackage package = new HttpRequestPackage (requestUri, HttpMethod.GET, null);
			return package;
		}

		public static HttpRequestPackage CreatePostRequest (Uri requestUri, BufferData requestData)
		{
			if (requestData == null)
				throw new ArgumentNullException ("requestData");
			HttpRequestPackage package = new HttpRequestPackage (requestUri, HttpMethod.POST, requestData);
			return package;
		}

		Uri requestUri;

		public Uri RequestUri {
			get {
				return requestUri;
			}
		}

		HttpMethod method;

		public HttpMethod Method {
			get {
				return method;
			}
		}

		BufferData bodyData;

		HttpRequestPackage (Uri requestUri, HttpMethod method, BufferData data)
		{
			this.bodyData = data;
			this.method = method;
			this.requestUri = requestUri;
		}

		public BufferData BodyData {
			get {
				return bodyData;
			}
		}

		bool keepAlive = true;

		public bool KeepAlive {
			get {
				return keepAlive;
			}
			set {
				keepAlive = value;
			}
		}

		HttpContentType contentType;

		public HttpContentType ContentType {
			get {
				return contentType;
			}
			set {
				contentType = value;
			}
		}

		public byte[] GetHeaderBuffer ()
		{
			string methodString;
			if (this.method == HttpMethod.POST) {
				methodString = HttpProtocol.POST;
			}
			else {
				methodString = HttpProtocol.GET;
			}
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{0} {1} {2}\r\n", methodString, this.requestUri.PathAndQuery, HttpProtocol.DEFAULT_HTTP_VERSION);
			sb.AppendFormat ("{0}: {1}\r\n", HttpProtocol.HOST, this.requestUri.Host);
			if (this.contentType != null) {
				sb.AppendFormat ("{0}: {1}\r\n", HttpProtocol.CONTENT_TYPE, this.contentType);
			}
			sb.AppendFormat ("{0}: {1}\r\n", HttpProtocol.CONNECTION, this.keepAlive ? HttpProtocol.KEEP_ALIVE : HttpProtocol.CLOSE);
			if (this.method == HttpMethod.POST) {
				if (this.bodyData != null) {
					sb.AppendFormat ("{0}: {1}\r\n", HttpProtocol.CONTENT_LENGTH, this.bodyData.Length);
				}
				else {
					sb.AppendFormat ("{0}: 0\r\n", HttpProtocol.CONTENT_LENGTH);
				}
			}
			sb.Append ("\r\n");
			return Encoding.ASCII.GetBytes (sb.ToString ());
		}
	}
}

