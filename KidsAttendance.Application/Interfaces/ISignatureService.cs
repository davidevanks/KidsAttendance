namespace KidsAttendance.Application.Interfaces;

public interface ISignatureService
{
    byte[]? DecodeBase64Signature(string? base64Signature);

    Task<string?> SaveBase64SignatureAsync(string? base64Signature, string prefix, CancellationToken cancellationToken = default);
}
