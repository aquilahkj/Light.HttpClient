using System;

namespace Light.HttpClient
{
	public static class ConvertUtility
	{
		public static bool ReadHexBytesToInt (byte[] buffer, int offset, int length, out int value)
		{
			value = 0;
			if (length > 8) {
				return false;
			}
			int pow = 0;
			int temp = 0;
			for (int i=offset+length-1; i>=offset; i--) {
				byte b = buffer [i];
				if (b >= 48 && b < 58) {
					temp = b - 48;
				} else if (b >= 65 && b < 71) {
					temp = b - 55;
				} else if (b >= 97 && b < 103) {
					temp = b - 87;
				} else {
					value = 0;
					return false;
				}

				int t = 1 << (pow * 4);
				temp *= t;
				value += temp;

				//				value += temp * (int)(Math.Pow (16, pow));
				pow++;
			}
			return true;
		}

		public static bool WriteIntToHexBytes (byte[] buffer, int offset, int value, out int length)
		{
			if (value == 0) {
				buffer [offset] = 48;
				length = 1;
				return true;
			}
			length = 0;
			int maxlen = buffer.Length - offset;
			//			int pow = 8;
			//			int t = 0;
			for (int i =7; i>=0; i--) {
				int temp = (value >> (i*4)) & 15;
				if (temp == 0 && length == 0) {
					continue;
				}
				if (length == maxlen) {
					return false;
				}
				byte b;
				if (temp >= 0 && temp < 10) {
					b = (byte)(temp + 48);
				} else {
					b = (byte)(temp + 87);
				}
				buffer [offset + length] = b;
				length++;
			}
			return true;
		}
	}
}

