using KidsAttendance.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Infrastructure.Persistence;

public partial class KidsAttendanceDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public KidsAttendanceDbContext(DbContextOptions<KidsAttendanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
    public DbSet<TeacherClassGroup> TeacherClassGroups => Set<TeacherClassGroup>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<ChildGuardian> ChildGuardians => Set<ChildGuardian>();
    public DbSet<ChildGroupHistory> ChildGroupHistories => Set<ChildGroupHistory>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => x.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<ClassGroup>(entity =>
        {
            entity.ToTable("ClassGroups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TeacherClassGroup>(entity =>
        {
            entity.ToTable("TeacherClassGroups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TeacherUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.AssignedByUserId).HasMaxLength(450);
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.ToTable("Guardians");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SecondaryPhoneNumber).HasMaxLength(30);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => x.PhoneNumber).IsUnique();
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.ToTable("Children");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => x.CurrentClassGroupId);
        });

        modelBuilder.Entity<ChildGuardian>(entity =>
        {
            entity.ToTable("ChildGuardians");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Relationship).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsPrimary).HasDefaultValue(false);
            entity.Property(x => x.IsAuthorizedPickup).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => new { x.ChildId, x.GuardianId }).IsUnique();
            entity.HasIndex(x => x.ChildId);
            entity.HasIndex(x => x.GuardianId);
        });

        modelBuilder.Entity<ChildGroupHistory>(entity =>
        {
            entity.ToTable("ChildGroupHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChangedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(250);
            entity.Property(x => x.ChangedAt).HasDefaultValueSql("SYSDATETIME()");
        });

        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.ToTable("AttendanceSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IsOpen).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.HasIndex(x => x.SessionDate);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("AttendanceRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenNumber).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("CheckedIn");
            entity.Property(x => x.CheckInTeacherId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.CheckOutTeacherId).HasMaxLength(450);
            entity.Property(x => x.CheckInSignatureData).HasColumnType("varbinary(max)");
            entity.Property(x => x.CheckOutSignatureData).HasColumnType("varbinary(max)");
            entity.Property(x => x.CheckInSignaturePath).HasMaxLength(500);
            entity.Property(x => x.CheckOutSignaturePath).HasMaxLength(500);
            entity.Property(x => x.CheckInTime).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(x => x.AttendanceSessionId);
            entity.HasIndex(x => x.ChildId);
            entity.HasIndex(x => x.ClassGroupId);
            entity.HasIndex(x => x.TokenNumber);
            entity.HasIndex(x => new { x.AttendanceSessionId, x.ClassGroupId, x.TokenNumber })
                .HasDatabaseName("IX_AttendanceRecords_ActiveTokenBySessionGroup")
                .HasFilter("[Status] = N'CheckedIn' AND [TokenNumber] IS NOT NULL");
        });
    }
}
