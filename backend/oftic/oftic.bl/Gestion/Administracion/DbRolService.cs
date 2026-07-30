using Comun.Dtos;
using Datos.Interfaz;
using Negocio.Interfaz;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Negocio.Gestion
{
    public class DbRolService : IDbRolService
    {
        private readonly IDbRolRepository _repo;

        public DbRolService(IDbRolRepository repo)
        {
            _repo = repo;
        }

        public Task<List<DtoRolAdmin>> GetRolesAdminAsync(CancellationToken ct) => _repo.GetRolesAdminAsync(ct);

        public Task<DtoSaveRolResult> SaveRolAdminAsync(DtoSaveRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
            => _repo.SaveRolAdminAsync(request, usuarioAuditoria, maquinaAuditoria, ct);

        public Task<DtoSaveRolResult> SetRolEstadoAsync(long idRol, int vigente, string usuarioAuditoria, string maquinaAuditoria, string? observaciones = null, CancellationToken ct = default)
            => _repo.SetRolEstadoAsync(idRol, vigente, usuarioAuditoria, maquinaAuditoria, observaciones, ct);

        public Task<List<DtoRol>> GetRolesAsync(CancellationToken ct) => _repo.GetRolesAsync(ct);

        public Task<List<DtoRolAsignado>> GetRolesAsignadosAsync(string usuario, CancellationToken ct)
            => _repo.GetRolesAsignadosAsync(usuario, ct);

        public Task<DtoGuardarUsuarioResult> AsignarRolAsync(long idUsuario, DtoAsignarRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
            => _repo.AsignarRolAsync(idUsuario, request, usuarioAuditoria, maquinaAuditoria, ct);

        public Task<DtoGuardarUsuarioResult> EditarRolAsync(long rolId, long idUsuario, DtoEditarRolRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
            => _repo.EditarRolAsync(rolId, idUsuario, request, usuarioAuditoria, maquinaAuditoria, ct);

        public Task<DtoGuardarUsuarioResult> EliminarRolAsync(long rolId, long idUsuario, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
            => _repo.EliminarRolAsync(rolId, idUsuario, usuarioAuditoria, maquinaAuditoria, ct);
    }
}
