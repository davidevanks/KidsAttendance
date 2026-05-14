# KidsAttendance

Sistema web para control de asistencia de niños en clases dominicales.

## Requisitos

- .NET SDK 10
- SQL Server 2019
- PowerShell o terminal compatible con `dotnet`

## Crear base de datos

1. Abrir SQL Server Management Studio.
2. Ejecutar el script:
   - [database/001_Create_KidsAttendance_Database.sql](C:/Users/ascen/source/repos/AsistenciaMinisterioInfantil/database/001_Create_KidsAttendance_Database.sql)

El script crea `KidsAttendanceDb`, tablas, constraints, índices y datos iniciales.

## Scaffold-DbContext (Database First)

Referencia:
- [database/Scaffold-DbContext.md](C:/Users/ascen/source/repos/AsistenciaMinisterioInfantil/database/Scaffold-DbContext.md)

Comando base:

```powershell
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

## Configuración

Ajustar `ConnectionStrings:KidsAttendanceDb` en:

- [KidsAttendance.Web/appsettings.json](C:/Users/ascen/source/repos/AsistenciaMinisterioInfantil/KidsAttendance.Web/appsettings.json)
- [KidsAttendance.Web/appsettings.Development.json](C:/Users/ascen/source/repos/AsistenciaMinisterioInfantil/KidsAttendance.Web/appsettings.Development.json)

## Usuario admin inicial

El seed de admin es configurable:

```json
"AdminSeed": {
  "Enabled": true,
  "Email": "admin@local.test",
  "Password": "Admin1234",
  "FullName": "Administrador General"
}
```

Después de crear el admin, volver `Enabled` a `false`.

## Correr el proyecto

```powershell
dotnet build KidsAttendance.slnx
dotnet run --project KidsAttendance.Web
```

## Roles actuales

- Admin
- Teacher
- SnackTeam

## Flujo básico

1. Iniciar sesión en `/Account/Login`.
2. Admin:
   - Gestiona grupos, usuarios, asignaciones, tutores y niños.
   - Consulta reportes y descarga Excel.
3. Teacher:
   - Registra entrada/salida.
   - Gestiona tutores y niños según grupo asignado.
4. SnackTeam:
   - Consulta conteo de presentes por grupo.
