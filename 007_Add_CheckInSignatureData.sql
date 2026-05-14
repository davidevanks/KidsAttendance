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

IF COL_LENGTH('dbo.AttendanceRecords', 'CheckInSignatureData') IS NULL
BEGIN
    ALTER TABLE dbo.AttendanceRecords
    ADD CheckInSignatureData VARBINARY(MAX) NULL;
END;
GO

PRINT 'OK: Columna CheckInSignatureData disponible en AttendanceRecords.';
GO