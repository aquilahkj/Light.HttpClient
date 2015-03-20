using System;
using System.Text;

namespace Light.HttpClient
{
	public class HttpContentType
	{
		string content;
		string contentType;

		public string ContentType {
			get {
				return contentType;
			}
		}

		string charset = null;

		public string Charset {
			get {
				return charset;
			}
		}

		string rawCharset = null;

		public HttpContentType (string content)
		{
			this.content = content;
			if (string.IsNullOrEmpty (this.content)) {
				return;
			}
			string[] array = content.Split (';');
			contentType = array [0].Trim ().ToLower ();
			for (int i = 1; i < array.Length; i++) {
				string item = array [i].Trim ();
				if (item.IndexOf (HttpProtocol.CHARSET, StringComparison.OrdinalIgnoreCase) >= 0) {
					this.rawCharset = item;
					this.charset = HttpContentType.GetCharsetValue (item);
					break;
				}
			}
		}

		public static string GetCharsetValue (string content)
		{
			if (string.IsNullOrEmpty (content)) {
				return string.Empty;
			}
			int i = content.IndexOf ("=");
			if (i < HttpProtocol.CHARSET.Length) {
				return string.Empty;
			}
			if (i == content.Length - 1) {
				return string.Empty;
			}
			string charset = content.Substring (i + 1);
			if (charset.IndexOf ('\"') >= 0) {
				charset = charset.Replace ("\"", "");
			}
			if (charset.IndexOf ('\'') >= 0) {
				charset = charset.Replace ("\'", "");
			}
			charset = charset.Trim ();
			return charset;
		}

		public static Encoding ParseEncoding (string charset)
		{
			Encoding encoding = null;
			if (!string.IsNullOrEmpty (charset)) {
				try {
					string c = null;
					int s = charset.IndexOf (',');
					if (s > 0) {
						c = charset.Substring (0, s);
					}
					else {
						c = charset;
					}
					c = c.Trim ().ToLower ();
					if (c.Equals ("utf8", StringComparison.OrdinalIgnoreCase)) {
						c = "utf-8";
					}
					encoding = Encoding.GetEncoding (c);
				}
				catch {

				}
			}
			return encoding;
		}

		Encoding encoding = null;
		bool isLoadEncoding = false;

		public Encoding GetEncoding ()
		{
			if (!this.isLoadEncoding) {
				this.isLoadEncoding = true;
				this.encoding = HttpContentType.ParseEncoding (this.charset);
			}
			return this.encoding;

		}

		public void SetEncoding (Encoding encoding)
		{
			if (encoding == null) {
				return;
			}
			this.encoding = encoding;
			this.isLoadEncoding = true;
			string name = encoding.WebName.ToLower ();
			string newCharset = string.Concat (HttpProtocol.CHARSET, "=", name);
			if (this.rawCharset == null) {
				this.content = string.Concat (this.content, ";", newCharset);
			}
			else {
				this.content = this.content.Replace (this.rawCharset, newCharset);
			}
			this.charset = name;
			this.rawCharset = newCharset;
		}

		public override string ToString ()
		{
			return this.content;
		}
	}
}

