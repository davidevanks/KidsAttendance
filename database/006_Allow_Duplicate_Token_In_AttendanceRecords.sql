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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AttendanceRecords')
      AND name = N'IX_AttendanceRecords_ActiveTokenBySessionGroup'
)
BEGIN
    CREATE INDEX IX_AttendanceRecords_ActiveTokenBySessionGroup
        ON dbo.AttendanceRecords(AttendanceSessionId, ClassGroupId, TokenNumber)
        WHERE [Status] = N'CheckedIn' AND [TokenNumber] IS NOT NULL;
END;
GO

PRINT 'OK: Regla de ficha actualizada para permitir duplicados.';
GO
