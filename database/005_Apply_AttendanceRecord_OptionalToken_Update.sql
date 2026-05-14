SET NOCOUNT ON;
GO

USE [KidsAttendanceDb];
GO

IF OBJECT_ID(N'dbo.AttendanceRecords', N'U') IS NULL
BEGIN
    RAISERROR('La tabla dbo.AttendanceRecords no existe en KidsAttendanceDb.', 16, 1);
    RETURN;
END;
GO

-- 1) TokenNumber opcional: normaliza vacios a NULL y luego altera a nullable
UPDATE dbo.AttendanceRecords
SET TokenNumber = NULL
WHERE LTRIM(RTRIM(ISNULL(TokenNumber, N''))) = N'';
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AttendanceRecords')
      AND name = N'TokenNumber'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.AttendanceRecords
    ALTER COLUMN TokenNumber NVARCHAR(30) NULL;
END;
GO

-- 2) Eliminar columna TokenReturned (si existe), removiendo primero su default constraint
IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AttendanceRecords')
      AND name = N'TokenReturned'
)
BEGIN
    DECLARE @dfName SYSNAME;

    SELECT @dfName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AttendanceRecords')
      AND c.name = N'TokenReturned';

    IF @dfName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.AttendanceRecords DROP CONSTRAINT [' + @dfName + N'];');
    END;

    ALTER TABLE dbo.AttendanceRecords
    DROP COLUMN TokenReturned;
END;
GO

-- 3) Re-crear índice único filtrado de ficha activa para permitir TokenNumber NULL
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AttendanceRecords')
      AND name = N'UX_AttendanceRecords_ActiveTokenBySessionGroup'
)
BEGIN
    DROP INDEX UX_AttendanceRecords_ActiveTokenBySessionGroup ON dbo.AttendanceRecords;
END;
GO

CREATE UNIQUE INDEX UX_AttendanceRecords_ActiveTokenBySessionGroup
    ON dbo.AttendanceRecords(AttendanceSessionId, ClassGroupId, TokenNumber)
    WHERE [Status] = N'CheckedIn' AND TokenNumber IS NOT NULL;
GO

PRINT 'OK: Actualizacion de AttendanceRecords aplicada.';
GO
