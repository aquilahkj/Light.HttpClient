using System;
using System.Threading;

namespace Light.HttpClient
{
	public class HttpProcessAsyncResult:IAsyncResult
	{
		object asyncState = null;
		AsyncCallback callback = null;
		ManualResetEvent handle = null;
		bool isCompleted = false;
		Exception innerException = null;
		HttpResponsePackage response = null;
		bool completedSynchronously = false;
		bool isEnd = false;

		public HttpProcessAsyncResult (AsyncCallback callback, object @object)
		{
			this.callback = callback;
			this.asyncState = @object;
		}

		public bool IsEnd {
			get {
				return isEnd;
			}
		}

		internal HttpResponsePackage Response {
			get {
				return response;
			}
		}

		public bool GoException {
			get {
				return this.innerException != null;
			}
		}

		internal Exception InnerException {
			get {
				return innerException;
			}
		}

		public void SetEnd ()
		{
			this.isEnd = true;
		}

		internal void SetResponse(HttpResponsePackage response)
		{
			lock (this) {
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

		internal void SetException(Exception exception)
		{
			lock (this) {
				this.innerException = exception;
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

		public void Clear ()
		{
			lock (this) {
				if (this.handle != null) {
					this.handle.Close ();
					this.handle = null;
				}
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

		#endregion


	}
}

