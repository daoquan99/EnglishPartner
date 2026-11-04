namespace EnglishLiveTutor.Application.Common.Exceptions;

public class FeedbackAudioException : Exception
{
    public string Code { get; }

    public FeedbackAudioException(string code) : base(code)
    {
        Code = code;
    }
}
