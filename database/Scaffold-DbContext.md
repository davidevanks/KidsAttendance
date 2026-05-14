# Scaffold-DbContext (Database First)

Ejecutar desde la raíz de la solución:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef dbcontext scaffold "Server=.;Database=KidsAttendanceDb;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer `
  --project KidsAttendance.Infrastructure `
  --startup-project KidsAttendance.Web `
  --context KidsAttendanceDbContext `
  --context-dir Persistence `
  --output-dir Persistence/Entities `
  --use-database-names `
  --data-annotations `
  --no-onconfiguring `
  --force
```

Notas:
- Este proyecto usa enfoque **Database First**.
- No usar migraciones Code First.
- Reejecutar el scaffold cuando cambie el esquema SQL base.
