using Comun.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Negocio.Interfaz
{
    public interface IDbRolService
    {
        Task<List<DtoRolAdmin>> GetRolesAdminAsync(CancellationToken ct);
        Task<DtoSaveRolResult> SaveRolAdminAsync(DtoSaveRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoSaveRolResult> SetRolEstadoAsync(long idRol, int vigente, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<List<DtoRol>> GetRolesAsync(CancellationToken ct);
        Task<List<DtoRolAsignado>> GetRolesAsignadosAsync(string usuario, CancellationToken ct);
        Task<DtoGuardarUsuarioResult> AsignarRolAsync(long idUsuario, DtoAsignarRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoGuardarUsuarioResult> EditarRolAsync(long rolId, long idUsuario, DtoEditarRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoGuardarUsuarioResult> EliminarRolAsync(long rolId, long idUsuario, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
    }
}
