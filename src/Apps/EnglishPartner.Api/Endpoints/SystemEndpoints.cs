using EnglishPartner.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Api.Endpoints;

internal static class SystemEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/system")
            .WithTags("System");
            // No global auth required here, we check it inside optionally, or we can enforce it.
            // Let's require authorization to upload files.

        group.MapPost("upload", async (
            [FromForm] IFormFile file,
            [FromForm] string folder,
            IFileStorageService fileStorage,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0) return Results.BadRequest("File is empty.");
            if (string.IsNullOrWhiteSpace(folder)) folder = "general";

            // Enforce basic security
            if (folder.Contains("..") || folder.Contains("/")) return Results.BadRequest("Invalid folder name.");

            await using var stream = file.OpenReadStream();
            var url = await fileStorage.UploadAsync(stream, file.FileName, folder, cancellationToken);
            
            return Results.Ok(new { Url = url });
        })
        .DisableAntiforgery() // Disabling antiforgery for simplicity in this API demo
        .RequireAuthorization()
        .WithSummary("Upload a file (Admin/User). Valid folders: avatars, topics, audio, general.");
    }
}
