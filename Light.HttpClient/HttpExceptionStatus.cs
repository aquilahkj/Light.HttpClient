using System;

namespace Light.HttpClient
{
	public enum HttpExceptionStatus
	{
		ConnectionPoolOverload,
		ConnectFailure,
		ReceiveFailure,
		SendFailure,
		RequestCanceled,
		ProtocolError,
		ConnectionClosed,
		KeepAliveFailure,
		UnknownError,
		MessageLengthLimitExceeded,
		HeaderLengthLimitExceeded,
		RequestError
	}
}

