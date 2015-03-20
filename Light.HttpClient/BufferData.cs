using System;

namespace Light.HttpClient
{
	public class BufferData
	{
		byte[] data = null;

		public byte[] Data
		{
			get{
				return data;
			}
		}

		int length = 0;

		public int Length {
			get {
				return length;
			}
		}

		public void ResetBuffer ()
		{
			this.length = 0;
		}

		public void ClearTempBuffer ()
		{
			this.length = 0;
			this.data = null;
		}

		public void WriteBuffer (byte[] buffer, int offset, int length)
		{
			if (length == 0)
				return;
			if (this.data == null) {
				this.length = 0;
				int size = 4096;
				if (length > size) {
					size = length * 2;
				}
				this.data = new byte[size];
			}
			if (this.length + length > this.data.Length) {
				int size = (this.length + length) * 2;
				byte[] newbuffer = new byte[size];
				Buffer.BlockCopy (this.data, 0, newbuffer, 0, this.length);
				this.data = newbuffer;
			}

			Buffer.BlockCopy (buffer, offset, this.data, this.length, length);
			this.length += length;
		}
	}
}

