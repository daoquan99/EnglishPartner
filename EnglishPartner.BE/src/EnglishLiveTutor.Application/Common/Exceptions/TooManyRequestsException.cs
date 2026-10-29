namespace EnglishLiveTutor.Application.Common.Exceptions;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message)
    {
    }
}
