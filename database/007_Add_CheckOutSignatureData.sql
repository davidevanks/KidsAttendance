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

IF COL_LENGTH('dbo.AttendanceRecords', 'CheckOutSignatureData') IS NULL
BEGIN
    ALTER TABLE dbo.AttendanceRecords
    ADD CheckOutSignatureData VARBINARY(MAX) NULL;
END;
GO

PRINT 'OK: Columna CheckOutSignatureData disponible en AttendanceRecords.';
GO
