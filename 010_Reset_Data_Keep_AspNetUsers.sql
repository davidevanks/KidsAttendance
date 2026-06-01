/*
    010_Reset_Data_Keep_AspNetUsers.sql
    -----------------------------------
    Purpose:
      Reset database data for a fresh configuration while keeping:
        - [dbo].[AspNetUsers]
        - [dbo].[AspNetRoles]

    Notes:
      - Intended for non-production use.
      - Deletes all rows from target tables.
      - Reseeds identity columns to start from initial values.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Disable constraints to avoid FK-order issues during cleanup */
    IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL ALTER TABLE dbo.AspNetRoleClaims NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserClaims NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserLogins NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserTokens NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AttendanceRecords', 'U') IS NOT NULL ALTER TABLE dbo.AttendanceRecords NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL ALTER TABLE dbo.AttendanceSessions NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ChildGroupHistory', 'U') IS NOT NULL ALTER TABLE dbo.ChildGroupHistory NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ChildGuardians', 'U') IS NOT NULL ALTER TABLE dbo.ChildGuardians NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.Children', 'U') IS NOT NULL ALTER TABLE dbo.Children NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.TeacherClassGroups', 'U') IS NOT NULL ALTER TABLE dbo.TeacherClassGroups NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.Guardians', 'U') IS NOT NULL ALTER TABLE dbo.Guardians NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ClassGroups', 'U') IS NOT NULL ALTER TABLE dbo.ClassGroups NOCHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.__SchemaVersion', 'U') IS NOT NULL ALTER TABLE dbo.__SchemaVersion NOCHECK CONSTRAINT ALL;

    /* Identity & app tables (excluding AspNetUsers and AspNetRoles) */
    IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL DELETE FROM dbo.AspNetRoleClaims;
    IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserClaims;
    IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserLogins;
    IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserTokens;

    /* Business tables */
    IF OBJECT_ID('dbo.AttendanceRecords', 'U') IS NOT NULL DELETE FROM dbo.AttendanceRecords;
    IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL DELETE FROM dbo.AttendanceSessions;
    IF OBJECT_ID('dbo.ChildGroupHistory', 'U') IS NOT NULL DELETE FROM dbo.ChildGroupHistory;
    IF OBJECT_ID('dbo.ChildGuardians', 'U') IS NOT NULL DELETE FROM dbo.ChildGuardians;
    IF OBJECT_ID('dbo.Children', 'U') IS NOT NULL DELETE FROM dbo.Children;
    IF OBJECT_ID('dbo.TeacherClassGroups', 'U') IS NOT NULL DELETE FROM dbo.TeacherClassGroups;
    IF OBJECT_ID('dbo.Guardians', 'U') IS NOT NULL DELETE FROM dbo.Guardians;
    IF OBJECT_ID('dbo.ClassGroups', 'U') IS NOT NULL DELETE FROM dbo.ClassGroups;
    IF OBJECT_ID('dbo.__SchemaVersion', 'U') IS NOT NULL DELETE FROM dbo.__SchemaVersion;

    /* Re-enable constraints and re-trust them */
    IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL ALTER TABLE dbo.AspNetRoleClaims WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserClaims WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserLogins WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL ALTER TABLE dbo.AspNetUserTokens WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AttendanceRecords', 'U') IS NOT NULL ALTER TABLE dbo.AttendanceRecords WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL ALTER TABLE dbo.AttendanceSessions WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ChildGroupHistory', 'U') IS NOT NULL ALTER TABLE dbo.ChildGroupHistory WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ChildGuardians', 'U') IS NOT NULL ALTER TABLE dbo.ChildGuardians WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.Children', 'U') IS NOT NULL ALTER TABLE dbo.Children WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.TeacherClassGroups', 'U') IS NOT NULL ALTER TABLE dbo.TeacherClassGroups WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.Guardians', 'U') IS NOT NULL ALTER TABLE dbo.Guardians WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.ClassGroups', 'U') IS NOT NULL ALTER TABLE dbo.ClassGroups WITH CHECK CHECK CONSTRAINT ALL;
    IF OBJECT_ID('dbo.__SchemaVersion', 'U') IS NOT NULL ALTER TABLE dbo.__SchemaVersion WITH CHECK CHECK CONSTRAINT ALL;

    /* Reseed identity columns where applicable */
    IF OBJECT_ID('dbo.AttendanceRecords', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.AttendanceRecords'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.AttendanceRecords', RESEED, 0);
    IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.AttendanceSessions'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.AttendanceSessions', RESEED, 0);
    IF OBJECT_ID('dbo.ChildGroupHistory', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.ChildGroupHistory'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.ChildGroupHistory', RESEED, 0);
    IF OBJECT_ID('dbo.Children', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.Children'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.Children', RESEED, 0);
    IF OBJECT_ID('dbo.ClassGroups', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.ClassGroups'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.ClassGroups', RESEED, 0);
    IF OBJECT_ID('dbo.Guardians', 'U') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID('dbo.Guardians'), 'Id', 'IsIdentity') = 1 DBCC CHECKIDENT ('dbo.Guardians', RESEED, 0);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
