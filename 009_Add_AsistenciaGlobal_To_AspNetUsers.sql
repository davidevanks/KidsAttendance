IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AspNetUsers')
      AND name = 'AsistenciaGlobal'
)
BEGIN
    ALTER TABLE dbo.AspNetUsers ADD AsistenciaGlobal BIT NOT NULL DEFAULT 0;
END;
