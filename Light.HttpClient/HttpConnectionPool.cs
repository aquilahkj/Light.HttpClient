using System;
using System.Net;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Light.HttpClient
{
	internal class HttpConnectionPool:IDisposable
	{
		HttpConfig config = null;
		IPEndPoint endPoint = null;
		int capacity = 100;
		SocketArgsPool socketArgsPool = null;
		ConcurrentStack<HttpConnection> connections = new ConcurrentStack<HttpConnection> ();
		int totalCreateCount = 0;
		public HttpConnectionPool (IPEndPoint endPoint)
			: this (endPoint, new HttpConfig ())
		{

		}

		public HttpConnectionPool (IPEndPoint endPoint, HttpConfig config)
		{
			if (config.PoolCapacity > 0) {
				this.capacity = config.PoolCapacity;
			}
			this.config = config;
			this.endPoint = endPoint;
			this.socketArgsPool = new SocketArgsPool (this.capacity, 4096);
		}

		public HttpConnection GetConnection ()
		{
			HttpConnection connection;
			if (!connections.TryPop (out connection)) {
				SocketAsyncEventArgs args = this.socketArgsPool.CheckOut ();
				if (args == null) {
					return null;
				}
				System.Threading.Interlocked.Increment (ref this.totalCreateCount);
				connection = new HttpConnection (this.endPoint, args, this.config, OnCloseConnection);
			}
			return connection;

		}

		void OnCloseConnection (object sender, SocketAsyncEventArgs args)
		{
			HttpConnection connection = sender as HttpConnection;
			if (connections.Count > this.capacity / 2) {
				connection.Clear ();
				this.socketArgsPool.CheckIn (args);
//				this.bufferPool.CheckIn (args);
				System.Threading.Interlocked.Decrement (ref this.totalCreateCount);
			}
			else {
				this.connections.Push (connection);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[HttpConnectionPool: Count={0}; TotalCount={1};]", this.connections.Count, this.totalCreateCount);
		}

		#region IDisposable implementation

		bool disposed;

		~HttpConnectionPool ()
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
			if (disposing) {
				// 清理托管资源
				//				if (managedResource != null)
				//				{
				//					managedResource.Dispose();
				//					managedResource = null;
				//				}

			}
			DateTime dt = DateTime.Now;
			while (true) {
				if (DateTime.Now.Subtract (dt).TotalSeconds > 5) {
					break;
				}

				HttpConnection connection;
				if (!connections.TryPop (out connection)) {
					if (this.totalCreateCount == 0) {
						break;
					}
				}
				else {
					connection.Clear ();
					System.Threading.Interlocked.Decrement (ref this.totalCreateCount);
				}
			}
			this.socketArgsPool.Dispose ();
			// 清理非托管资源
			//			if (nativeResource != IntPtr.Zero)
			//			{
			//				Marshal.FreeHGlobal(nativeResource);
			//				nativeResource = IntPtr.Zero;
			//			}
			//让类型知道自己已经被释放
			disposed = true;
		}

		#endregion
	}
}

