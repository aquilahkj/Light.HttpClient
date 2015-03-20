using System;

namespace Light.HttpClient
{
	public class HttpException:Exception
	{
		HttpExceptionStatus status;

		public HttpExceptionStatus Status {
			get {
				return status;
			}
		}

		public HttpException (HttpExceptionStatus status):this(string.Empty,status)
		{

		}

		public HttpException (string message):this(message,HttpExceptionStatus.UnknownError)
		{

		}

		public HttpException (string message, HttpExceptionStatus status):base(message)
		{
			this.status = status;
		}

		public HttpException (string message, HttpExceptionStatus status, Exception innerException):base(message,innerException)
		{
			this.status = status;
		}

		public override string ToString ()
		{
			return string.Format ("[HttpException: status={0}]\r\n" + base.ToString (), status);
		}
	}
}

