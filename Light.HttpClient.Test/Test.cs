using NUnit.Framework;
using System;
using Light.HttpClient;
using System.Text;

namespace Light.HttpClient.Test
{
	[TestFixture ()]
	public class Test
	{
		[Test ()]
		public void TestCase1 ()
		{
			string uri = "http://m.baidu.com";
			HttpWebClient client = new HttpWebClient (new Uri (uri), new HttpConfig ());
			HttpRequestPackage request = HttpRequestPackage.CreateGetRequest (new Uri (uri));
			IAsyncResult result = client.BeginSendRequest (request, null, null, null);
			bool ok = result.AsyncWaitHandle.WaitOne ();
			if (ok) {
				HttpResponsePackage response = client.EndSendRequest (result);
				Console.WriteLine (response.StatusCode);
				Console.Write (Encoding.UTF8.GetString (response.BodyData.Data, 0, response.BodyData.Length));
			}
			else {
				client.EndSendRequest (result);
			}
		}
	}
}

