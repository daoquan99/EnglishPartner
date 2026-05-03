namespace EnglishPartner.SharedKernel.Settings;

public sealed class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    public string BasePath { get; set; } = "./uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public int MaxFileSizeMB { get; set; } = 10;
}
