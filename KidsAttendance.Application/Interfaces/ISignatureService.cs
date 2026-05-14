namespace KidsAttendance.Application.Interfaces;

public interface ISignatureService
{
    Task<string?> SaveBase64SignatureAsync(string? base64Signature, string prefix, CancellationToken cancellationToken = default);
}
