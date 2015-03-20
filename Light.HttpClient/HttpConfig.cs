using System;
using System.Net.Sockets;
using System.Net;

namespace Light.HttpClient
{
	public class HttpConfig
	{
		int defaultBufferSize = 4096;

		public int DefaultBufferSize {
			get {
				return defaultBufferSize;
			}
			set {
				if (value < 1024)
					throw new ArgumentOutOfRangeException ("defaultBufferSize");
				defaultBufferSize = value;
			}
		}

		int poolCapacity = 100;

		public int PoolCapacity {
			get {
				return poolCapacity;
			}
			set {
				if (value < 10)
					throw new ArgumentOutOfRangeException ("poolCapacity");
				poolCapacity = value;
			}
		}

		int maxQueueCount = 300;

		public int MaxQueueCount {
			get {
				return maxQueueCount;
			}
			set {
				if (value < 10)
					throw new ArgumentOutOfRangeException ("maxQueueCount");
				maxQueueCount = value;
			}
		}
	}
}

