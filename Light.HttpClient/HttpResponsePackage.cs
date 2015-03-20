using System;
using System.Text;

namespace Light.HttpClient
{
	public class HttpResponsePackage
	{
		string version;

		string Version {
			get {
				return version;
			}
		}

		string statusCode = null;

		public string StatusCode {
			get {
				return statusCode;
			}
		}

		string reasonPhrase = null;

		public string ReasonPhrase {
			get {
				return reasonPhrase;
			}
		}

		HttpContentType contentType = null;

		public HttpContentType ContentType {
			get {
				return contentType;
			}
		}

		int contentLength = 0;

		public int ContentLength {
			get {
				return contentLength;
			}
		}

		bool keepAlive = false;

		public bool KeepAlive {
			get {
				return keepAlive;
			}
		}

		string connection = null;

		public string Connection {
			get {
				return connection;
			}
		}

		BufferData bodyData = null;

		public BufferData BodyData {
			get {
				return bodyData;
			}
		}

		internal HttpResponsePackage (byte[] buffer, int offset, int length, BufferData data)
		{
			string headerString = Encoding.ASCII.GetString (buffer, offset, length);
			string[] headers = headerString.Split (new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			string[] parts = headers [0].Split (new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 3) {
				throw new HttpException ("header part error: " + headerString, HttpExceptionStatus.RequestError);
			}
			this.version = parts [0].Trim ();
			if (this.version != HttpProtocol.DEFAULT_HTTP_VERSION) {
				throw new HttpException ("this protocol version is " + this.version, HttpExceptionStatus.ProtocolError);
			}
			this.statusCode = parts [1].Trim ();
			this.reasonPhrase = parts [2].Trim ();
			for (int i = 1; i < headers.Length; i++) {
				if (headers [i].Trim () == string.Empty)
					continue;
				string[] kv = headers [i].Split (new char[] { ':' }, 2);
				if (kv.Length == 2) {
					string key = kv [0].Trim ();
					if (key == string.Empty) {
						continue;
					}
					if (key == HttpProtocol.CONTENT_TYPE) {
						this.contentType = new HttpContentType (kv [1].Trim ());
					}
					else if (key == HttpProtocol.TRANSFER_ENCODING && kv [1].Trim () == HttpProtocol.CHUNKED) {
						this.contentLength = -1;
					}
					else if (key == HttpProtocol.CONTENT_LENGTH) {
						this.contentLength = int.Parse (kv [1]);
					}
					else if (key == HttpProtocol.CONNECTION) {
						string value = kv [1].Trim ().ToLower ();
						connection = value;
						if (value.Equals (HttpProtocol.KEEP_ALIVE, StringComparison.OrdinalIgnoreCase)) {
							this.keepAlive = true;
						}
						else {
							this.keepAlive = false;
						}
					} 
				}
			}
			this.bodyData = data;
		}
	}
}

