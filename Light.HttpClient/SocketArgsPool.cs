using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Light.HttpClient
{
	/// <summary>
	/// Pools SocketAsyncEventArgs objects to avoid repeated allocations.
	/// </summary>
	public class SocketArgsPool : IDisposable
	{
		Stack<SocketAsyncEventArgs> argsPool;
		int totalBytes;
		byte[] buffer;
		Stack<int> freeIndexPool;
		int currentIndex;
		int bufferSize;

		/// <summary>
		/// Pools SocketAsyncEventArgs objects to avoid repeated allocations.
		/// </summary>
		/// <param name="capacity">The ammount to SocketAsyncEventArgs to create and pool.</param>
		public SocketArgsPool (int capacity, int bufferSize)
		{
			this.argsPool = new Stack<SocketAsyncEventArgs> (capacity);
			this.totalBytes = capacity * bufferSize;
			this.bufferSize = bufferSize;

			this.currentIndex = 0;
			this.freeIndexPool = new Stack<Int32> ();
			this.buffer = new Byte[totalBytes];

			for (int i = 0; i < capacity; i++) {
				this.argsPool.Push (new SocketAsyncEventArgs ());
			}
		}

		/// <summary>
		/// Checks an SocketAsyncEventArgs back into the pool.
		/// </summary>
		/// <param name="item">The SocketAsyncEventsArgs to check in.</param>
		public void CheckIn (SocketAsyncEventArgs args)
		{
			lock (argsPool) {
				this.argsPool.Push (args);
				this.freeIndexPool.Push(args.Offset);
				args.SetBuffer(null, 0, 0);
			}
		}

		/// <summary>
		/// Check out an SocketAsyncEventsArgs from the pool.
		/// </summary>
		/// <returns>The SocketAsyncEventArgs.</returns>
		public SocketAsyncEventArgs CheckOut ()
		{
			lock (argsPool) {
				if (argsPool.Count == 0) {
					return null;
				}
				SocketAsyncEventArgs args = argsPool.Pop ();

				if (freeIndexPool.Count > 0)
					args.SetBuffer (this.buffer, this.freeIndexPool.Pop (), this.bufferSize);
				else {
					args.SetBuffer (this.buffer, this.currentIndex, this.bufferSize);
					this.currentIndex += this.bufferSize;
				}
				return args;
			}
		}

		/// <summary>
		/// The number of available objects in the pool.
		/// </summary>
		public int Available {
			get {
				lock (argsPool) {
					return argsPool.Count;
				}
			}
		}
		#region IDisposable Members
		private Boolean disposed = false;

		~SocketArgsPool ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		private void Dispose (bool disposing)
		{
			if (!this.disposed) {
				if (disposing) {
					foreach (SocketAsyncEventArgs args in argsPool) {
						args.Dispose ();
					}
				}

				disposed = true;
			}
		}
		#endregion
	}
}

