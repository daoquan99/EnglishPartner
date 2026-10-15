namespace EnglishLiveTutor.Application.Common.Interfaces;

/// <summary>
/// Marker interface indicating that the request requires a specific permission to execute.
/// </summary>
public interface IRequirePermission
{
    string Permission { get; }
}
