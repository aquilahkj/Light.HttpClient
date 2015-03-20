using System;

namespace Light.HttpClient
{
	public class HttpMission
	{
		HttpRequestPackage request;

		public HttpRequestPackage Request {
			get {
				return request;
			}
		}

		BufferData bufferData;

		public BufferData BufferData {
			get {
				return bufferData;
			}
		}

		HttpProcessAsyncResult result;

		public HttpProcessAsyncResult Result {
			get {
				return result;
			}
		}

		public HttpMission (HttpRequestPackage request, BufferData bufferData, HttpProcessAsyncResult result)
		{
			if (result == null)
				throw new ArgumentNullException ("result");
			if (bufferData == null)
				throw new ArgumentNullException ("bufferData");
			if (request == null)
				throw new ArgumentNullException ("request");

			this.result = result;
			this.bufferData = bufferData;
			this.request = request;

		}
	}
}

