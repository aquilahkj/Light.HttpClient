using System;
using System.Text;

namespace Light.HttpClient.Demo
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string uri = "http://192.168.67.48:8080/ecop/api.do?action=ECOP_SERVICE_0014&servernum=7DFACBC320A9E3E100001rgE";
			HttpConfig config = new HttpConfig ();
			HttpWebClient client = new HttpWebClient (new Uri (uri), config);


			HttpRequestPackage request = HttpRequestPackage.CreateGetRequest (new Uri (uri));
			IAsyncResult result = client.BeginSendRequest (request, null, null);
			bool ok = result.AsyncWaitHandle.WaitOne (60000);
			if (ok) {
				HttpResponsePackage response = client.EndSendRequest (result);
				Console.WriteLine (response.StatusCode);
				Console.Write (Encoding.UTF8.GetString (response.BodyData.Data, 0, response.BodyData.Length));
			}
			else {
				client.EndSendRequest (result);
			}

			Console.ReadLine ();
		}

	
	}
}
