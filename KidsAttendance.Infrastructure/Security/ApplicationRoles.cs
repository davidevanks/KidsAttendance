namespace KidsAttendance.Infrastructure.Security;

/// <summary>
/// Centraliza los nombres de rol usados por Identity y por la autorización de la aplicación.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// Rol con acceso completo a la administración del sistema.
    /// </summary>
    public const string Coordinador = "Coordinador";

    /// <summary>
    /// Rol para maestros con acceso operativo a sus grupos asignados.
    /// </summary>
    public const string Teacher = "Teacher";

    /// <summary>
    /// Rol para el equipo de refrigerio.
    /// </summary>
    public const string SnackTeam = "SnackTeam";

    public const string CoordinadorOrTeacher = Coordinador + "," + Teacher;
    public const string SnackTeamOrCoordinador = SnackTeam + "," + Coordinador;
}
