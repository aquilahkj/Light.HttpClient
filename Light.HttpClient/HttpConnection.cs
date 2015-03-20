using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Light.HttpClient
{
	public delegate void HttpConnectionCloseEventHandler (object sender, SocketAsyncEventArgs args);
	public delegate void HttpConnectionCompleteEventHandler (object sender, HttpResponsePackage response);
	public delegate void HttpConnectionExceptionEventHandler (object sender, Exception exception);
	internal class HttpConnection
	{
		const int MAX_HEADER_SIZE = 8192;
		const byte CR = 13;
		const byte LF = 10;
		HttpConnectionCloseEventHandler OnCloseHandler;
		public HttpConnectionCompleteEventHandler OnCompleteHandler;
		public HttpConnectionExceptionEventHandler OnExceptionHandler;
		private bool keepAlive;
		private SocketAsyncEventArgs receiveArgs;
		IPEndPoint enpoint;
		BufferData responseBodyBuffer = null;
		HttpRequestPackage request = null;
		int sendTimes = 0;
		bool isConnect = false;
		byte[] dataBuffer = null;
		bool isUseClose = false;
		bool isProcessComplete = false;
		int dataLength = 0;
		int checkHeadIndex;
		int headerLength;
		HttpResponsePackage responsePackage = null;
		int chunkedIndex;
		int chunkedBlockIndex;
		int chunkedBlockLength;
		bool isException = false;

		public bool KeepAlive {
			get {
				return keepAlive;
			}
		}

		int defaultBufferSize = 4096;

		public HttpConnection (IPEndPoint enpoint, SocketAsyncEventArgs args, HttpConfig config, HttpConnectionCloseEventHandler closeHandler)
		{
			this.enpoint = enpoint;
			this.receiveArgs = args;
			this.receiveArgs.Completed += ReceivedCompleted;
			if (config.DefaultBufferSize > 0) {
				this.defaultBufferSize = config.DefaultBufferSize;
			}
			this.dataBuffer = new byte[this.defaultBufferSize];
			OnCloseHandler = closeHandler;
		}

		public void SendRequest (HttpRequestPackage request, BufferData bufferData)
		{
			this.responseBodyBuffer = bufferData;
			this.request = request;
			this.keepAlive = request.KeepAlive;
			this.dataLength = 0;

			this.checkHeadIndex = 0;
			this.headerLength = 0;
			this.responsePackage = null;
			this.isProcessComplete = false;
			this.chunkedIndex = 0;
			this.chunkedBlockIndex = 0;
			this.chunkedBlockLength = 0;
			this.isException = false;

			SendData ();
		}

		void CloseConnection (SocketAsyncEventArgs args)
		{
			lock (this) {
				if (isUseClose) {
					return;
				}
				this.isUseClose = true;
				this.isConnect = false;

				Socket socket = args.UserToken as Socket;
				if (socket != null) {
					try {
						socket.Shutdown (SocketShutdown.Both);
						socket.Close ();
					}
					catch {

					}
					args.UserToken = null;
				}
			}
		}

		void Connect ()
		{
			lock (this) {
				this.sendTimes = 0;
				this.isUseClose = false;
				SocketAsyncEventArgs args = new SocketAsyncEventArgs ();
				args.Completed += ConnectComplete;
				args.RemoteEndPoint = this.enpoint;
				Socket socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.SendTimeout = 1000;
				socket.ReceiveTimeout = 1000;
				args.UserToken = socket;
				socket.InvokeAsyncMethod (socket.ConnectAsync, ConnectComplete, args);
			}
		}

		void ConnectComplete (object sender, SocketAsyncEventArgs args)
		{
			Socket socket = sender as Socket;
			args.Completed -= ConnectComplete;
			SocketError status = args.SocketError;
			args.UserToken = null;
			args.Dispose ();
			if (status == SocketError.Success) {
				this.isConnect = true;
				this.receiveArgs.UserToken = socket;
				socket.InvokeAsyncMethod (socket.ReceiveAsync, ReceivedCompleted, this.receiveArgs);
				SendData ();
			}
			else {
				lock (this) {
					this.isConnect = false;
					try {
						socket.Shutdown (SocketShutdown.Both);
						socket.Close ();
					}
					catch {

					}
				}
				OnException (new HttpException (string.Format ("connection {0} failed,error: {1}", this.request.RequestUri, status), HttpExceptionStatus.ConnectFailure));
			}
		}

		void CheckDataBufferCapacity (int value)
		{
			if (value + this.dataLength > this.dataBuffer.Length) {
				int newCap = (value + this.dataLength) * 2;
				byte[] newBuffer = new byte[newCap];
				Buffer.BlockCopy (this.dataBuffer, 0, newBuffer, 0, this.dataLength);
				this.dataBuffer = newBuffer;
			}
		}

		void SendData ()
		{
			SocketAsyncEventArgs args = this.receiveArgs;
			if (args == null) {
				return;
			}
			try {
				Socket socket = args.UserToken as Socket;
				if (socket == null) {
					Connect ();
					return;
				}
				else if (!socket.Connected || !this.isConnect) {
					CloseConnection (args);
					Connect ();
					return;
				}

				byte[] headerBuffer = this.request.GetHeaderBuffer ();
				socket.Send (headerBuffer, 0, headerBuffer.Length, SocketFlags.None);
				BufferData data = this.request.BodyData;
				if (data != null) {
					socket.Send (data.Data, 0, data.Length, SocketFlags.None);
				}
				this.sendTimes++;
			}
			catch (Exception ex) {
				CloseConnection (args);
				if (this.sendTimes == 0) {
					OnException (new HttpException ("send data error:" + ex.Message, HttpExceptionStatus.SendFailure, ex));
				}
				else {
					Connect ();
				}
			}
		}

		void ReceivedCompleted (object sender, SocketAsyncEventArgs args)
		{
			if (args.SocketError != SocketError.Success) {
				CloseConnection (args); //Graceful disconnect
				if (!this.isProcessComplete) {
					OnException (new HttpException ("exception response endding " + args.SocketError, HttpExceptionStatus.ConnectionClosed));
				}
				return;
			}

			if (args.BytesTransferred == 0) {
				CloseConnection (args); //Graceful disconnect
				if (!this.isProcessComplete) {
					OnException (new HttpException ("error response endding", HttpExceptionStatus.RequestCanceled));
				}
				return;
			}


			if (this.isProcessComplete) {
				return;
			}

			try {
				ProcessData (args.Buffer, args.Offset, args.BytesTransferred);
			}
			catch (Exception ex) {
				CloseConnection (args); 
				OnException (ex);
				return;
			}

			if (this.isProcessComplete && this.isConnect && !this.isException) {
				HttpResponsePackage package = this.responsePackage;
				this.responsePackage = null;
				this.keepAlive = package.KeepAlive;
				if (!this.keepAlive) {
					CloseConnection (args);
				}
				if (OnCompleteHandler != null) {
					try {
						OnCompleteHandler (this, package);
					}
					catch {
//						Console.WriteLine ("OnCompleteHandler error:" + e.Message);
					}
					finally {
						CallbackClose ();
					}
				}
			} 
			ListenForData (args);
		}

		private void ListenForData (SocketAsyncEventArgs args)
		{
			lock (this) {
				Socket socket = args.UserToken as Socket;
				if (socket != null && socket.Connected) {
					socket.InvokeAsyncMethod (socket.ReceiveAsync, ReceivedCompleted, args);
				}
			}
		}

		void ProcessData (byte[] buffer, int offset, int count)
		{
			CheckDataBufferCapacity (count);
			Buffer.BlockCopy (buffer, offset, this.dataBuffer, this.dataLength, count);
			this.dataLength += count;
			if (this.responsePackage == null) {
				DetectHeader ();
			}
			if (this.responsePackage != null) {
				DetectBody ();
			}
		}

		void OnException (Exception ex)
		{
			lock (this) {
				if (!this.isException) {
					this.isException = true;
					if (OnExceptionHandler != null) {
						try {
							OnExceptionHandler (this, ex);
						}
						catch (Exception e) {
//								Console.WriteLine ("OnException error:" + e.Message);
						}
						finally {
							CallbackClose ();
						}
					}
				}
			}
		}

		void CallbackClose ()
		{
			OnCloseHandler (this, this.receiveArgs);
		}

		internal void Clear ()
		{
			if (this.keepAlive) {
				CloseConnection (this.receiveArgs);
			}
			this.receiveArgs.Completed -= ReceivedCompleted; //MUST Remember This!
			this.receiveArgs = null;
			this.OnCloseHandler = null;
		}

		void DetectHeader ()
		{
			if (this.dataLength < 10) {
				return;
			}

			bool flag = false;
			for (; this.checkHeadIndex < this.dataLength - 3; this.checkHeadIndex++) {
				if (this.dataBuffer [checkHeadIndex] == CR && this.dataBuffer [checkHeadIndex + 1] == LF && this.dataBuffer [checkHeadIndex + 2] == CR && this.dataBuffer [checkHeadIndex + 3] == LF) {
					this.headerLength = checkHeadIndex + 4;
					flag = true;
					break;
				}
			}
			if (flag) {
				this.checkHeadIndex = 0;
				this.responseBodyBuffer.ResetBuffer ();
				this.responsePackage = new HttpResponsePackage (this.dataBuffer, 0, this.headerLength, this.responseBodyBuffer);
			}
			else {
				checkHeadIndex -= 3;
				if (checkHeadIndex < 0) {
					checkHeadIndex = 0;
				}
				if (this.dataLength > MAX_HEADER_SIZE) {
					throw new HttpException (string.Format ("header is too long,size is {0}", this.dataLength), HttpExceptionStatus.HeaderLengthLimitExceeded);
				}
			}
		}

		void DetectBody ()
		{
			int contentLength = this.responsePackage.ContentLength;

			if (contentLength == -1) {
				if (this.chunkedIndex == 0) {
					this.chunkedIndex = this.headerLength;
				}
				ReadChunkedBlock ();
			}
			else if (contentLength == 0) {
				this.isProcessComplete = true;
			}
			else {
				BufferData data = this.responsePackage.BodyData;
				int bodyIndex = this.headerLength;
				int currentLength = this.dataLength - bodyIndex;
				int length = currentLength - data.Length;
				int offset = bodyIndex + data.Length;
				data.WriteBuffer (this.dataBuffer, offset, length);
				if (data.Length >= contentLength) {
					this.isProcessComplete = true;
				}
				else if (data.Length > contentLength) {
					throw new HttpException (string.Format ("Content-Length is {0},Receive data length is{1}", contentLength, data.Length), HttpExceptionStatus.MessageLengthLimitExceeded);
				}
			}
		}

		void ReadChunkedBlock ()
		{
			BufferData data = this.responsePackage.BodyData;
			if (this.dataLength >= this.chunkedIndex + 3) {
				if (this.chunkedBlockIndex == 0) {
					bool flag = false;
					int maxlen = this.dataLength - this.chunkedIndex > 10 ? 10 : this.dataLength - this.chunkedIndex;
					int startIndex = this.chunkedIndex;
					int endIndex = this.chunkedIndex + maxlen;
					for (; startIndex < endIndex - 1; startIndex++) {
						if (this.dataBuffer [startIndex] == CR && this.dataBuffer [startIndex + 1] == LF) {
							flag = true;
							break;
						}
					}

					if (!flag) {
						if (maxlen == 10) {
							throw new HttpException ("chunked flag length error");
						}
						else {
							//maybe chunked flag not complete
							return;
						}
					}

					int chunkNumLength = startIndex - this.chunkedIndex;
					if (ConvertUtility.ReadHexBytesToInt (this.dataBuffer, this.chunkedIndex, chunkNumLength, out this.chunkedBlockLength)) {
						this.chunkedBlockIndex = this.chunkedIndex + chunkNumLength + 2;
					}
					else {
						throw new HttpException (string.Format ("chunked flag error,flag is {0}", Encoding.ASCII.GetString (this.dataBuffer, this.chunkedIndex, chunkNumLength)), HttpExceptionStatus.RequestError);
					}
				}

				if (this.chunkedBlockLength > 0) {
					int nextChunkedIndex = this.chunkedBlockIndex + this.chunkedBlockLength + 2;
					if (this.dataLength >= nextChunkedIndex) {
						data.WriteBuffer (this.dataBuffer, this.chunkedBlockIndex, chunkedBlockLength);
						this.chunkedIndex = nextChunkedIndex;
						this.chunkedBlockIndex = 0;
						this.chunkedBlockLength = 0;
						ReadChunkedBlock ();
					}
				}
				else if (this.chunkedBlockLength == 0) {
					if (this.dataLength >= this.chunkedBlockIndex + 2) {
						if (this.dataBuffer [this.chunkedBlockIndex] == CR &&
						    this.dataBuffer [this.chunkedBlockIndex + 1] == LF) {
							this.chunkedBlockIndex = 0;
							this.isProcessComplete = true;
						}
						else {
							throw new HttpException (string.Format ("request body message endding error,endding bytes {0} {1}", this.dataBuffer [this.chunkedBlockIndex], this.dataBuffer [this.chunkedBlockIndex + 1]), HttpExceptionStatus.RequestError);
						}
					}
					else {
						return;
					}
				}
				else {
					throw new HttpException (string.Format ("chunked flag number error,flag is {0}", this.chunkedBlockLength), HttpExceptionStatus.RequestError);
				}
			} 
		}
	}
}

