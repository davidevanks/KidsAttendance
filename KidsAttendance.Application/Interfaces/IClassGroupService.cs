using KidsAttendance.Application.DTOs;

namespace KidsAttendance.Application.Interfaces;

public interface IClassGroupService
{
    Task<IReadOnlyList<ClassGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClassGroupDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ClassGroupDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ClassGroupDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
