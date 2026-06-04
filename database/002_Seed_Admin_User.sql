SET NOCOUNT ON;
GO

USE [KidsAttendanceDb];
GO

DECLARE @CoordinadorAccount NVARCHAR(256) = N'coordinador';
DECLARE @CoordinadorNormalizedAccount NVARCHAR(256) = UPPER(@CoordinadorAccount);
DECLARE @CoordinadorEmail NVARCHAR(256) = @CoordinadorAccount + N'@local.invalid';
DECLARE @CoordinadorNormalizedEmail NVARCHAR(256) = UPPER(@CoordinadorEmail);
-- Password seed: Test123
DECLARE @CoordinadorPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEFIn8wS4Phahrao8UUoU0ap2VzEA6nWQJc+ZcYIBvVRCy81Y1Sj781thuF8RCvALAQ==';
DECLARE @CoordinadorId NVARCHAR(450);
DECLARE @CoordinadorRoleId NVARCHAR(450);

SELECT @CoordinadorRoleId = Id
FROM dbo.AspNetRoles
WHERE NormalizedName = N'COORDINADOR';

IF @CoordinadorRoleId IS NULL
BEGIN
    SET @CoordinadorRoleId = N'role-coordinador';
    INSERT INTO dbo.AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
    VALUES (@CoordinadorRoleId, N'Coordinador', N'COORDINADOR', CONVERT(NVARCHAR(36), NEWID()));
END;

SELECT @CoordinadorId = Id
FROM dbo.AspNetUsers
WHERE NormalizedUserName = @CoordinadorNormalizedAccount
   OR NormalizedEmail = @CoordinadorNormalizedEmail;

IF @CoordinadorId IS NULL
BEGIN
    SET @CoordinadorId = CONVERT(NVARCHAR(36), NEWID());

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
        @CoordinadorId,
        N'Coordinador General',
        @CoordinadorAccount,
        @CoordinadorNormalizedAccount,
        @CoordinadorEmail,
        @CoordinadorNormalizedEmail,
        1,
        @CoordinadorPasswordHash,
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
        FullName = N'Coordinador General',
        IsActive = 1,
        EmailConfirmed = 1,
        PasswordHash = @CoordinadorPasswordHash,
        UserName = @CoordinadorAccount,
        NormalizedUserName = @CoordinadorNormalizedAccount,
        Email = @CoordinadorEmail,
        NormalizedEmail = @CoordinadorNormalizedEmail,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @CoordinadorId;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetUserRoles ur
    WHERE ur.UserId = @CoordinadorId
      AND ur.RoleId = @CoordinadorRoleId
)
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES (@CoordinadorId, @CoordinadorRoleId);
END;
GO
