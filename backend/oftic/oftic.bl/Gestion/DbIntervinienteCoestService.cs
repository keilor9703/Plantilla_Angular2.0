using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbIntervinienteCoestService : IDbIntervinienteCoestService
{
    private readonly IDbIntervinienteCoestRepository _repository;

    public DbIntervinienteCoestService(IDbIntervinienteCoestRepository repository)
    {
        _repository = repository;
    }

    public async Task<DtoIntervinienteCoestResult> CreateAsync(DtoIntervinienteCoestRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var validation = Validate(request);
        if (!validation.Success) return validation;

        return await _repository.CreateAsync(request, maquinaAuditoria, ct);
    }

    public async Task<DtoIntervinienteCoestResult> DeleteAsync(string intervinienteId, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(intervinienteId))
            return new DtoIntervinienteCoestResult { Success = false, Message = "ID inválido." };

        return await _repository.DeleteAsync(intervinienteId, identificacionModifica, maquinaModifica, ct);
    }

    private static DtoIntervinienteCoestResult Validate(DtoIntervinienteCoestRequest request)
    {
        if (request is null)
            return Fail("Payload requerido.");

        if (string.IsNullOrWhiteSpace(request.MedioId))
            return Fail("El identificador del medio es requerido.");

        if (request.Grado is null)
            return Fail("El campo 'Grado' es obligatorio.");

        if (!string.IsNullOrWhiteSpace(request.Nombre) && request.Nombre.Length > 255)
            return Fail("El nombre no puede superar 255 caracteres.");

        if (!string.IsNullOrWhiteSpace(request.Cargo) && request.Cargo.Length > 255)
            return Fail("El cargo no puede superar 255 caracteres.");

        return new DtoIntervinienteCoestResult { Success = true, Message = "OK" };
    }

    private static DtoIntervinienteCoestResult Fail(string msg)
        => new() { Success = false, Message = msg };
}
