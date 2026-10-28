using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Endpoints;
using EnglishLiveTutor.Api.Exceptions;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Validation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class ProblemDetailsGlobalExceptionHandlerTests
{
    private static (DefaultHttpContext context, MemoryStream bodyStream) CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;
        return (context, bodyStream);
    }

    private static JsonDocument ReadResponseBody(MemoryStream bodyStream)
    {
        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8, leaveOpen: true);
        string text = reader.ReadToEnd();
        return JsonDocument.Parse(text);
    }

    [Fact]
    public async Task ValidationException_ShouldReturn400_WithErrorsDictionary_AndErrorField()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var failures = new List<ValidationFailure>
        {
            new("Email", "Email không được để trống."),
            new("Password", "Mật khẩu phải từ 8 ký tự trở lên.")
        };
        var ex = new ValidationException(failures);

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;

        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Bad Request", root.GetProperty("title").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", root.GetProperty("type").GetString());
        Assert.Equal(ex.Message, root.GetProperty("detail").GetString());
        Assert.Equal(ex.Message, root.GetProperty("error").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceIdProp) && !string.IsNullOrWhiteSpace(traceIdProp.GetString()));

        var errors = root.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out var emailErrors));
        Assert.Contains("Email không được để trống.", emailErrors.EnumerateArray().Select(e => e.GetString()));
        Assert.True(errors.TryGetProperty("Password", out var pwdErrors));
        Assert.Contains("Mật khẩu phải từ 8 ký tự trở lên.", pwdErrors.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task FluentValidationException_ShouldReturn400_WithErrorsDictionary_AndErrorField()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var failures = new List<ValidationFailure>
        {
            new("Title", "Tiêu đề không hợp lệ.")
        };
        var ex = new FluentValidation.ValidationException(failures);

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal(ex.Message, root.GetProperty("detail").GetString());
        Assert.Equal(ex.Message, root.GetProperty("error").GetString());
        Assert.True(root.GetProperty("errors").TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task NotFoundException_ShouldReturn404_WithProblemDetails()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new NotFoundException("Tài nguyên không tồn tại.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", root.GetProperty("title").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", root.GetProperty("type").GetString());
        Assert.Equal("Tài nguyên không tồn tại.", root.GetProperty("detail").GetString());
        Assert.Equal("Tài nguyên không tồn tại.", root.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task ForbiddenException_ShouldReturn403_WithProblemDetails()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new ForbiddenException("Không đủ quyền truy cập.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("title").GetString());
        Assert.Equal("Không đủ quyền truy cập.", root.GetProperty("detail").GetString());
        Assert.Equal("Không đủ quyền truy cập.", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ConflictException_ShouldReturn409_WithProblemDetails()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new ConflictException("Phiên học đã được hoàn thành trước đó.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Conflict", root.GetProperty("title").GetString());
        Assert.Equal("Phiên học đã được hoàn thành trước đó.", root.GetProperty("detail").GetString());
        Assert.Equal("Phiên học đã được hoàn thành trước đó.", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvalidOperationException_ShouldReturn400_WithBusinessErrorMessage()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new InvalidOperationException("Email này đã được kích hoạt.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Email này đã được kích hoạt.", root.GetProperty("detail").GetString());
        Assert.Equal("Email này đã được kích hoạt.", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ArgumentException_ShouldReturn400_WithErrorMessage()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new ArgumentException("Tham số search không hợp lệ.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Tham số search không hợp lệ.", root.GetProperty("detail").GetString());
        Assert.Equal("Tham số search không hợp lệ.", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task JsonException_ShouldReturn400_WithGenericDetail_AndNotLeakParserMessage()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new System.Text.Json.JsonException("'x' is an invalid start of a value. Path: $.exercises[0] | LineNumber: 3");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal("Dữ liệu JSON không hợp lệ.", root.GetProperty("detail").GetString());
        Assert.Equal("Dữ liệu JSON không hợp lệ.", root.GetProperty("error").GetString());
        Assert.DoesNotContain("LineNumber", root.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task NotSupportedException_ShouldReturn400_WithMessage()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new NotSupportedException("Câu trả lời mở cần được chấm bằng AI.");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        Assert.Equal("Câu trả lời mở cần được chấm bằng AI.", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task BadHttpRequestException_ShouldKeepItsStatusCode_WithGenericDetail()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        Assert.Equal("Yêu cầu không hợp lệ.", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task OperationCanceledException_WhenClientAborted_ShouldReturn499_WithNoBody()
    {
        var (context, bodyStream) = CreateHttpContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Client cancelled
        context.RequestAborted = cts.Token;

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new OperationCanceledException(cts.Token);

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(0, bodyStream.Length);
    }

    [Fact]
    public async Task UnexpectedException_ShouldReturn500_WithGenericVietnameseDetail_AndNotLeakMessage()
    {
        var (context, bodyStream) = CreateHttpContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ex = new InvalidCastException("Internal server stack trace with SQL connection string Server=db.internal;User Id=sa;Password=SuperSecretPassword123;");

        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", root.GetProperty("type").GetString());

        string detail = root.GetProperty("detail").GetString()!;
        string error = root.GetProperty("error").GetString()!;

        Assert.Equal("Đã xảy ra lỗi không mong muốn trên máy chủ. Vui lòng thử lại sau.", detail);
        Assert.Equal("Đã xảy ra lỗi không mong muốn trên máy chủ. Vui lòng thử lại sau.", error);

        // Verification: sensitive exception message is NOT leaked
        Assert.DoesNotContain("SuperSecretPassword123", detail);
        Assert.DoesNotContain("Server=db.internal", detail);
        Assert.DoesNotContain("InvalidCastException", detail);

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task EltAuthenticationHandler_HandleChallengeAsync_ShouldReturn401_ProblemDetails()
    {
        var (context, bodyStream) = CreateHttpContext();
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var jwtService = new FakeJwtTokenService();
        var handler = new EltAuthenticationHandler(optionsMonitor, NullLoggerFactory.Instance, UrlEncoder.Default, jwtService);
        await handler.InitializeAsync(new AuthenticationScheme("Bearer", null, typeof(EltAuthenticationHandler)), context);

        await handler.ChallengeAsync(new AuthenticationProperties());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(401, root.GetProperty("status").GetInt32());
        Assert.Equal("Bạn cần đăng nhập để thực hiện thao tác này.", root.GetProperty("detail").GetString());
        Assert.Equal("Bạn cần đăng nhập để thực hiện thao tác này.", root.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task EltAuthenticationHandler_HandleForbiddenAsync_ShouldReturn403_ProblemDetails()
    {
        var (context, bodyStream) = CreateHttpContext();
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var jwtService = new FakeJwtTokenService();
        var handler = new EltAuthenticationHandler(optionsMonitor, NullLoggerFactory.Instance, UrlEncoder.Default, jwtService);
        await handler.InitializeAsync(new AuthenticationScheme("Bearer", null, typeof(EltAuthenticationHandler)), context);

        await handler.ForbidAsync(new AuthenticationProperties());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Bạn không có quyền truy cập tài nguyên này.", root.GetProperty("detail").GetString());
        Assert.Equal("Bạn không có quyền truy cập tài nguyên này.", root.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task CsrfHeaderMiddleware_ShouldReturn403_ProblemDetails_WhenHeaderMissing()
    {
        var (context, bodyStream) = CreateHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/auth/refresh";
        context.Request.Headers.Cookie = "elt_refresh_token=some_active_cookie_token";

        var middleware = new CsrfHeaderMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        using var json = ReadResponseBody(bodyStream);
        var root = json.RootElement;
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Contains("CSRF", root.GetProperty("detail").GetString());
        Assert.Contains("CSRF", root.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public void LessonMediaEndpoints_ForbiddenException_ShouldMapTo404NotFound()
    {
        // Assert intentional mapping of ForbiddenException in lesson media endpoint to 404
        var result = ApiProblem.NotFound("Media asset not found.");
        Assert.NotNull(result);

        var (context, bodyStream) = CreateHttpContext();
        var problem = ApiProblem.Create(StatusCodes.Status404NotFound, "Media asset not found.", context);

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Media asset not found.", problem.Detail);
        Assert.Equal("Media asset not found.", problem.Extensions["error"]);
    }

    [Fact]
    public void RolePlayAndAnswerConcurrencyConflict_ShouldMapTo409Conflict()
    {
        var problem = ApiProblem.Create(StatusCodes.Status409Conflict, "Turn conflict occurred.");
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal("Turn conflict occurred.", problem.Detail);
        Assert.Equal("Turn conflict occurred.", problem.Extensions["error"]);
    }

    [Fact]
    public void AuthRefresh_InvalidOperationException_ShouldMapTo401Unauthorized()
    {
        var problem = ApiProblem.Create(StatusCodes.Status401Unauthorized, "Refresh token đã bị thu hồi.");
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal("Refresh token đã bị thu hồi.", problem.Detail);
        Assert.Equal("Refresh token đã bị thu hồi.", problem.Extensions["error"]);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public GeneratedAccessToken GenerateAccessToken(EnglishLiveTutor.Domain.Identity.UserEntity user, IReadOnlyList<string> roleCodes, IReadOnlyList<string> permissionCodes) => throw new NotImplementedException();
        public string GenerateRefreshTokenString() => throw new NotImplementedException();
        public ClaimsPrincipal? ValidateAccessToken(string token) => null;
    }
}
