SET NOCOUNT ON;
GO

USE [KidsAttendanceDb];
GO

DECLARE @AdminRoleId NVARCHAR(450);
DECLARE @CoordinadorRoleId NVARCHAR(450);

SELECT @AdminRoleId = Id
FROM dbo.AspNetRoles
WHERE NormalizedName = N'ADMIN';

SELECT @CoordinadorRoleId = Id
FROM dbo.AspNetRoles
WHERE NormalizedName = N'COORDINADOR';

IF @AdminRoleId IS NOT NULL AND @CoordinadorRoleId IS NULL
BEGIN
    UPDATE dbo.AspNetRoles
    SET
        [Name] = N'Coordinador',
        NormalizedName = N'COORDINADOR',
        ConcurrencyStamp = CONVERT(NVARCHAR(36), NEWID())
    WHERE Id = @AdminRoleId;

    SET @CoordinadorRoleId = @AdminRoleId;
END;

IF @CoordinadorRoleId IS NULL
BEGIN
    SET @CoordinadorRoleId = N'role-coordinador';

    INSERT INTO dbo.AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
    VALUES (@CoordinadorRoleId, N'Coordinador', N'COORDINADOR', CONVERT(NVARCHAR(36), NEWID()));
END;

IF @AdminRoleId IS NOT NULL AND @AdminRoleId <> @CoordinadorRoleId
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    SELECT ur.UserId, @CoordinadorRoleId
    FROM dbo.AspNetUserRoles ur
    WHERE ur.RoleId = @AdminRoleId
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AspNetUserRoles existingUr
          WHERE existingUr.UserId = ur.UserId
            AND existingUr.RoleId = @CoordinadorRoleId
      );

    DELETE FROM dbo.AspNetUserRoles
    WHERE RoleId = @AdminRoleId;

    DELETE FROM dbo.AspNetRoles
    WHERE Id = @AdminRoleId;
END;

UPDATE dbo.AspNetUsers
SET
    UserName = N'coordinador',
    NormalizedUserName = N'COORDINADOR',
    Email = N'coordinador@local.invalid',
    NormalizedEmail = N'COORDINADOR@LOCAL.INVALID',
    FullName = CASE
        WHEN FullName = N'Administrador General' THEN N'Coordinador General'
        ELSE FullName
    END,
    UpdatedAt = SYSDATETIME()
WHERE NormalizedUserName = N'ADMIN'
   OR NormalizedEmail = N'ADMIN@LOCAL.INVALID';
GO
