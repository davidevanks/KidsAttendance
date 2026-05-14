IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AspNetUsers_PhoneNumber_NotNull'
      AND object_id = OBJECT_ID('dbo.AspNetUsers')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_AspNetUsers_PhoneNumber_NotNull
        ON dbo.AspNetUsers (PhoneNumber)
        WHERE PhoneNumber IS NOT NULL;
END;
