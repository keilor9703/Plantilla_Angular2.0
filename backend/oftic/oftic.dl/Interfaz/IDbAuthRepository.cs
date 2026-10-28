using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datos.Interfaz
{
    public interface IDbAuthRepository
    {
        Task<(long? idUsuario, List<long> roles)> GetUsuarioYRolesAsync(string usuario, CancellationToken ct);

        /// <summary>Devuelve la identificación (cédula) del usuario, o null si no se encuentra.</summary>
        Task<string?> GetIdentificacionAsync(string usuario, CancellationToken ct);
    }
}
