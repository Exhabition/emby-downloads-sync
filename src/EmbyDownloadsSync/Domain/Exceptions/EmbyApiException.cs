namespace EmbyDownloadsSync.Domain.Exceptions;

public class EmbyApiException(string message) : Exception(message);