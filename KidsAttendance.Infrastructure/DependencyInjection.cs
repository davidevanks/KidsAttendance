using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KidsAttendance.Application.Interfaces;

namespace KidsAttendance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KidsAttendanceDb")
            ?? throw new InvalidOperationException("Connection string 'KidsAttendanceDb' is not configured.");

        services.AddDbContext<KidsAttendanceDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IClassGroupService, ClassGroupService>();
        services.AddScoped<ISignatureService, SignatureService>();

        return services;
    }
}
