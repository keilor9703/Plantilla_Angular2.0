using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbMedioCoestService : IDbMedioCoestService
{
    private readonly IDbMedioCoestRepository _repository;

    // IDs reales de tipo de medio en COEST_CTRL_DOMINIO (PADRE_ID=4)
    private const decimal TipoRadio       = 5;
    private const decimal TipoTelevision  = 7;
    private const decimal TipoPrensa      = 6;
    private const decimal TipoWeb         = 9;
    private const decimal TipoRedesSoc    = 75;

    public DbMedioCoestService(IDbMedioCoestRepository repository)
    {
        _repository = repository;
    }

    public Task<List<DtoMedioCoest>> GetByNoticiaAsync(string noticiaId, CancellationToken ct)
        => _repository.GetByNoticiaAsync(noticiaId, ct);

    public async Task<DtoMedioCoestResult> CreateAsync(DtoMedioCoestRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var validation = Validate(request);
        if (!validation.Success) return validation;

        return await _repository.CreateAsync(request, maquinaAuditoria, ct);
    }

    public async Task<DtoMedioCoestResult> DeleteAsync(string medioId, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(medioId))
            return new DtoMedioCoestResult { Success = false, Message = "ID inválido." };

        return await _repository.DeleteAsync(medioId, identificacionModifica, maquinaModifica, ct);
    }

    private static DtoMedioCoestResult Validate(DtoMedioCoestRequest request)
    {
        if (request is null)
            return Fail("Payload requerido.");

        if (string.IsNullOrWhiteSpace(request.NoticiaId))
            return Fail("El identificador del hecho noticioso es requerido.");

        if (request.IdMedio is null)
            return Fail("El campo 'Medio' es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Titular))
            return Fail("El campo 'Titular' es obligatorio.");

        if (request.Titular.Length > 255)
            return Fail("El titular no puede superar 255 caracteres.");

        if (request.Fecha is null)
            return Fail("El campo 'Fecha' es obligatorio.");

        if (request.Fecha > DateTime.Now)
            return Fail("La fecha de la noticia no puede ser mayor a la fecha actual.");

        // Duración obligatoria para Radio y TV
        if ((request.IdMedio == TipoRadio || request.IdMedio == TipoTelevision) && request.Duracion is null)
            return Fail("El campo 'Duración' es obligatorio para medios de Televisión o Radio.");

        // Evidencia obligatoria para TV, Prensa y Radio
        if ((request.IdMedio == TipoTelevision || request.IdMedio == TipoPrensa || request.IdMedio == TipoRadio) && request.IdEvidencia is null)
            return Fail("El campo 'Evidencia' es obligatorio para medios de Televisión, Prensa o Radio.");

        // URL obligatoria para Web y Redes Sociales
        if ((request.IdMedio == TipoWeb || request.IdMedio == TipoRedesSoc) && string.IsNullOrWhiteSpace(request.Url))
            return Fail("El campo 'URL' es obligatorio para medios Web y Redes Sociales.");

        return new DtoMedioCoestResult { Success = true, Message = "OK" };
    }

    private static DtoMedioCoestResult Fail(string msg)
        => new() { Success = false, Message = msg };
}
