namespace EmbyDownloadsSync.Domain.Exceptions;

public class EmbyApiException : Exception
{
	public EmbyApiException(string message) : base(message)
	{
	}
}