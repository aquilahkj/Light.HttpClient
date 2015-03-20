using System;
using System.Threading;

namespace Light.HttpClient
{
	class HttpAsyncResult:IAsyncResult
	{
		object asyncState = null;
		AsyncCallback callback = null;
		HttpConnection connection = null;
		ManualResetEvent handle = null;
		bool isCompleted = false;
		Exception innerException = null;
		HttpResponsePackage response = null;
		bool completedSynchronously = false;
		bool isEnd = false;

		public void SetEnd ()
		{
			this.isEnd = true;
		}

		internal HttpResponsePackage Response {
			get {
				return response;
			}
		}

		internal Exception InnerException {
			get {
				return innerException;
			}
		}

		internal HttpAsyncResult (HttpConnection connection, AsyncCallback callback, object @object)
		{
			this.connection = connection;
			this.callback = callback;
			this.asyncState = @object;
			this.connection.OnCompleteHandler = OnConnectionComplete;
			this.connection.OnExceptionHandler = OnConnectionException;
		}

		void OnConnectionComplete (object sender, HttpResponsePackage response)
		{
			lock (this) {
				HttpConnection connection = sender as HttpConnection;
				connection.OnCompleteHandler = null;//-= OnConnectionComplete;
				connection.OnExceptionHandler = null;//-= OnConnectionException;
//				connection.Close ();
				this.connection = null;
				this.response = response;
				this.isCompleted = true;
				if (this.handle != null) {
					this.handle.Set ();
					if (this.isEnd) {
						this.handle.Dispose ();
						this.handle = null;
					}
				}

			}
			if (this.callback != null) {
				this.callback.BeginInvoke (this, null, null);
			}
		}

		void OnConnectionException (object sender, Exception ex)
		{
			lock (this) {
				HttpConnection connection = sender as HttpConnection;
				connection.OnCompleteHandler = null;//-= OnConnectionComplete;
				connection.OnExceptionHandler = null;//-= OnConnectionException;
				this.connection = null;
				this.innerException = ex;
				this.isCompleted = true;
				if (this.handle != null) {
					this.handle.Set ();
					if (this.isEnd) {
						this.handle.Dispose ();
						this.handle = null;
					}
				}

			}
			if (this.callback != null) {
				this.callback.BeginInvoke (this, null, null);
			}
		}
		#region IAsyncResult implementation
		public object AsyncState {
			get {
				return this.asyncState;
			}
		}

		public WaitHandle AsyncWaitHandle {
			get {
				lock (this) {
					if (this.handle == null) {
						this.handle = new ManualResetEvent (this.isCompleted);
					}
					return this.handle;
				}
			}
		}

		public bool CompletedSynchronously {
			get {
				return this.completedSynchronously;
			}
		}

		public bool IsCompleted {
			get {
				return this.isCompleted;
			}
		}

		public bool GoException {
			get {
				return this.innerException != null;
			}
		}
		//
		//		public void Abort ()
		//		{
		//			lock (this) {
		//				if (this.handle != null) {
		//					this.handle.Set ();
		//					this.handle.Close ();
		//					this.handle = null;
		//				}
		//			}
		////			this.connection.Abort ();
		//			this.connection = null;
		////			this.connection.CallClose ();
		//		}

		public void Clear ()
		{
			lock (this) {
				if (this.handle != null) {
//					this.handle.Set ();
					this.handle.Close ();
					this.handle = null;
				}
			}
		}
		#endregion
	}
}

