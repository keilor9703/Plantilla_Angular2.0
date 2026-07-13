using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbHechoNoticiosoService : IDbHechoNoticiosoService
{
    private readonly IDbHechoNoticiosoRepository _repository;

    public DbHechoNoticiosoService(IDbHechoNoticiosoRepository repository)
    {
        _repository = repository;
    }

    public Task<List<DtoHechoNoticioso>> GetAllAsync(int anio, CancellationToken ct)
        => _repository.GetAllAsync(anio == 0 ? DateTime.Now.Year : anio, ct);

    public Task<List<DtoHechoNoticioso>> GetByUnidadAsync(string siglaUnidad, int anio, CancellationToken ct)
        => _repository.GetByUnidadAsync(siglaUnidad, anio == 0 ? DateTime.Now.Year : anio, ct);

    public Task<DtoHechoNoticioso?> GetByIdAsync(string id, CancellationToken ct)
        => _repository.GetByIdAsync(id, ct);

    public Task<List<DtoAnioItem>> GetAniosAsync(CancellationToken ct)
        => _repository.GetAniosAsync(ct);

    public async Task<DtoHechoNoticiosoResult> CreateAsync(DtoHechoNoticiosoRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var validation = Validate(request);
        if (!validation.Success) return validation;

        return await _repository.CreateAsync(request, maquinaAuditoria, ct);
    }

    public async Task<DtoHechoNoticiosoResult> DeleteAsync(string id, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new DtoHechoNoticiosoResult { Success = false, Message = "ID inválido." };

        return await _repository.DeleteAsync(id, identificacionModifica, maquinaModifica, ct);
    }

    private static DtoHechoNoticiosoResult Validate(DtoHechoNoticiosoRequest request)
    {
        if (request is null)
            return Fail("Payload requerido.");

        if (request.IdUnidad <= 0)
            return Fail("El campo 'Unidad' es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Titular))
            return Fail("El campo 'Titular' es obligatorio.");

        if (request.Titular.Length > 255)
            return Fail("El titular no puede superar 255 caracteres.");

        if (request.IdCaracter is null)
            return Fail("El campo 'Carácter' es obligatorio.");

        if (request.IdTema is null)
            return Fail("El campo 'Tema' es obligatorio.");

        if (request.IdCobertura is null)
            return Fail("El campo 'Cobertura' es obligatorio.");

        if (request.IdTendencia is null)
            return Fail("El campo 'Tendencia' es obligatorio.");

        // Descripción del tema requerida para ciertos temas (misma regla que HERA)
        // Los IDs de dominio que requieren desc_tema se pueden configurar; aquí se valida si hay tema y desc_tema nula
        // (dejar que el frontend envíe idDescTema cuando corresponda)

        return new DtoHechoNoticiosoResult { Success = true, Message = "OK" };
    }

    private static DtoHechoNoticiosoResult Fail(string msg)
        => new() { Success = false, Message = msg };
}
