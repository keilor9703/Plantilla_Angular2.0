using Comun.Dtos;
using Comun.Dtos.Administracion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.ApiInterfaz
{
    public interface IApiWebToken
    {
        Task<DtoRespuesta<string>> ObtenerTokenSeviciosAsync(DtoUsuarioPip usuario);
        Task<DtoRespuesta<string>> ObtenerTokenPipAsync(CancellationToken ct);
        Task<string> GetTokenAsync(string usuario, string contrasena);
        Task<DtoFuncionario> GetFuncionarioAsync(string token, string identificacion);
        Task<DtoFuncionario> GetFuncionarioPersonajeMesAsync(string token, string identificacion);
        Task<string> GetFotoFuncionarioAsync(string token, string identificacion);
        Task<List<DtoUnidadOrg>> GetUnidadesPipAsync(CancellationToken ct = default);
        Task<List<DtoSliderPublico>> GetSlidersPublicosAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default);
        Task<List<DtoNoticia>> GetNoticiasInternasAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default);
        Task<bool> RegistrarLikeNoticiaAsync(DtoLikeNoticiaRequest request, CancellationToken ct = default);
        Task<List<DtoModalVigente>> GetModalesVigentesAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default);
        Task<List<DtoRadioEmisora>> GetEmisorasAsync(DtoConsultaSliderPublicoRequest? request = null, CancellationToken ct = default);
    }
}
