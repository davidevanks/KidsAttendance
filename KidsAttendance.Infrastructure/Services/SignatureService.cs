using KidsAttendance.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace KidsAttendance.Infrastructure.Services;

public class SignatureService : ISignatureService
{
    private readonly IHostEnvironment _environment;

    public SignatureService(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> SaveBase64SignatureAsync(string? base64Signature, string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(base64Signature))
        {
            return null;
        }

        var marker = "base64,";
        var markerIndex = base64Signature.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var payload = markerIndex >= 0 ? base64Signature[(markerIndex + marker.Length)..] : base64Signature;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch
        {
            return null;
        }

        if (bytes.Length == 0 || bytes.Length > 1_000_000)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "signatures");
        Directory.CreateDirectory(uploadsRoot);

        var safePrefix = string.Concat(prefix.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(safePrefix))
        {
            safePrefix = "sig";
        }

        var fileName = $"{safePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        return $"/uploads/signatures/{fileName}";
    }
}
