using KidsAttendance.Application.DTOs;
using KidsAttendance.Application.Interfaces;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Infrastructure.Services;

public class ClassGroupService : IClassGroupService
{
    private readonly KidsAttendanceDbContext _dbContext;

    public ClassGroupService(KidsAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ClassGroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClassGroups
            .AsNoTracking()
            .OrderBy(x => x.MinAge)
            .ThenBy(x => x.MaxAge)
            .Select(x => new ClassGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                MinAge = x.MinAge,
                MaxAge = x.MaxAge,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassGroupDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClassGroups
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ClassGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                MinAge = x.MinAge,
                MaxAge = x.MaxAge,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(ClassGroupDto dto, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var exists = await _dbContext.ClassGroups.AnyAsync(x => x.Name == dto.Name.Trim(), cancellationToken);
        if (exists)
        {
            return (false, "Ya existe un grupo con ese nombre.");
        }

        var entity = new ClassGroup
        {
            Name = dto.Name.Trim(),
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            IsActive = dto.IsActive
        };

        _dbContext.ClassGroups.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(ClassGroupDto dto, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var entity = await _dbContext.ClassGroups.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (entity is null)
        {
            return (false, "Grupo no encontrado.");
        }

        var exists = await _dbContext.ClassGroups.AnyAsync(x => x.Id != dto.Id && x.Name == dto.Name.Trim(), cancellationToken);
        if (exists)
        {
            return (false, "Ya existe un grupo con ese nombre.");
        }

        entity.Name = dto.Name.Trim();
        entity.MinAge = dto.MinAge;
        entity.MaxAge = dto.MaxAge;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClassGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return (false, "Grupo no encontrado.");
        }

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private static string? Validate(ClassGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "El nombre del grupo es requerido.";
        }

        if (dto.MinAge < 0 || dto.MaxAge < 0 || dto.MaxAge < dto.MinAge)
        {
            return "El rango de edades no es válido.";
        }

        return null;
    }
}
