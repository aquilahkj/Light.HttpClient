using System;
using System.Collections.Concurrent;

namespace Light.HttpClient
{
	public class BufferDataPool
	{
		ConcurrentQueue<BufferData> queue = new ConcurrentQueue<BufferData> ();
		int minCapacity;
		int maxCapacity;
		int totalCreateCount = 0;

		public BufferDataPool (int minCapacity, int maxCapacity)
		{
			this.maxCapacity = maxCapacity;
			this.minCapacity = minCapacity;
			for (int i = 0; i < this.minCapacity; i++) {
				System.Threading.Interlocked.Increment (ref this.totalCreateCount);
				queue.Enqueue (new BufferData ());
			}
		}

		public int Count {
			get {
				return this.queue.Count;
			}
		}

		public BufferData GetBufferData ()
		{
			BufferData bufferData;
			if (this.queue.TryDequeue (out bufferData)) {
				return bufferData;
			}
			else {
				System.Threading.Interlocked.Increment (ref this.totalCreateCount);
				return new BufferData ();
			}
		}

		public void SetBufferData (BufferData bufferData)
		{
			if (this.queue.Count < this.maxCapacity) {
				queue.Enqueue (bufferData);
			}
			else {
				System.Threading.Interlocked.Decrement (ref this.totalCreateCount);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[BufferDataPool: Count={0}; TotalCount={1}]", Count, this.totalCreateCount);
		}
	}
}

