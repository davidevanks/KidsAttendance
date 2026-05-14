SET NOCOUNT ON;
GO

USE [KidsAttendanceDb];
GO

DECLARE @AdminEmail NVARCHAR(256) = N'admin@kidsattendance.local';
DECLARE @AdminNormalizedEmail NVARCHAR(256) = UPPER(@AdminEmail);
-- Password seed: Test123
DECLARE @AdminPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEFIn8wS4Phahrao8UUoU0ap2VzEA6nWQJc+ZcYIBvVRCy81Y1Sj781thuF8RCvALAQ==';
DECLARE @AdminId NVARCHAR(450);
DECLARE @AdminRoleId NVARCHAR(450);

SELECT @AdminRoleId = Id
FROM dbo.AspNetRoles
WHERE NormalizedName = N'ADMIN';

IF @AdminRoleId IS NULL
BEGIN
    SET @AdminRoleId = N'role-admin';
    INSERT INTO dbo.AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
    VALUES (@AdminRoleId, N'Admin', N'ADMIN', CONVERT(NVARCHAR(36), NEWID()));
END;

SELECT @AdminId = Id
FROM dbo.AspNetUsers
WHERE NormalizedEmail = @AdminNormalizedEmail;

IF @AdminId IS NULL
BEGIN
    SET @AdminId = CONVERT(NVARCHAR(36), NEWID());

    INSERT INTO dbo.AspNetUsers
    (
        Id,
        FullName,
        UserName,
        NormalizedUserName,
        Email,
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnabled,
        AccessFailedCount,
        IsActive,
        CreatedAt
    )
    VALUES
    (
        @AdminId,
        N'Administrador General',
        @AdminEmail,
        @AdminNormalizedEmail,
        @AdminEmail,
        @AdminNormalizedEmail,
        1,
        @AdminPasswordHash,
        CONVERT(NVARCHAR(36), NEWID()),
        CONVERT(NVARCHAR(36), NEWID()),
        0,
        0,
        1,
        0,
        1,
        SYSDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.AspNetUsers
    SET
        FullName = N'Administrador General',
        IsActive = 1,
        EmailConfirmed = 1,
        PasswordHash = @AdminPasswordHash,
        UserName = @AdminEmail,
        NormalizedUserName = @AdminNormalizedEmail,
        Email = @AdminEmail,
        NormalizedEmail = @AdminNormalizedEmail,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @AdminId;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetUserRoles ur
    WHERE ur.UserId = @AdminId
      AND ur.RoleId = @AdminRoleId
)
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES (@AdminId, @AdminRoleId);
END;
GO
