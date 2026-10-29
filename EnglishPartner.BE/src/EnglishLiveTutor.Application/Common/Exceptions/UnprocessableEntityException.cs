namespace EnglishLiveTutor.Application.Common.Exceptions;

public class UnprocessableEntityException : Exception
{
    public IDictionary<string, object?>? Extensions { get; }

    public UnprocessableEntityException(string message, IDictionary<string, object?>? extensions = null)
        : base(message)
    {
        Extensions = extensions;
    }
}
