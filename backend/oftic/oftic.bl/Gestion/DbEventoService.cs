using Comun.Dtos.Eventos;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbEventoService : IDbEventoService
{
    private readonly IDbEventoRepository _repository;

    public DbEventoService(IDbEventoRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DtoEvento>> GetAllAsync(CancellationToken ct)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task<DtoEvento?> GetByIdAsync(long id, CancellationToken ct)
    {
        if (id <= 0) return null;
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<List<DtoEvento>> GetActivosAsync(DateTime? fechaReferencia, CancellationToken ct)
    {
        return await _repository.GetActivosAsync(fechaReferencia, ct);
    }

    public async Task<DtoEventoResult> CreateAsync(DtoEventoRequest request, string usuario, string maquina, CancellationToken ct)
    {
        var validation = Validate(request);
        if (!validation.Success) return validation;

        return await _repository.CreateAsync(request, usuario, maquina, ct);
    }

    public async Task<DtoEventoResult> UpdateAsync(long id, DtoEventoRequest request, string usuario, string maquina, CancellationToken ct)
    {
        if (id <= 0)
            return new DtoEventoResult { Success = false, Message = "ID inválido." };

        var validation = Validate(request);
        if (!validation.Success) return validation;

        return await _repository.UpdateAsync(id, request, usuario, maquina, ct);
    }

    public async Task<DtoEventoResult> DeleteAsync(long id, string usuario, string maquina, CancellationToken ct)
    {
        if (id <= 0)
            return new DtoEventoResult { Success = false, Message = "ID inválido." };

        return await _repository.DeleteAsync(id, usuario, maquina, ct);
    }

    private static DtoEventoResult Validate(DtoEventoRequest request)
    {
        if (request == null)
            return new DtoEventoResult { Success = false, Message = "Payload requerido." };

        if (string.IsNullOrWhiteSpace(request.Unidad))
            return new DtoEventoResult { Success = false, Message = "La unidad es requerida." };

        if (request.Unidad.Length > 100)
            return new DtoEventoResult { Success = false, Message = "La unidad no puede superar 100 caracteres." };

        if (string.IsNullOrWhiteSpace(request.Titulo))
            return new DtoEventoResult { Success = false, Message = "El título es requerido." };

        if (request.Titulo.Length > 300)
            return new DtoEventoResult { Success = false, Message = "El título no puede superar 300 caracteres." };

        if (request.FechaFin < request.FechaInicio)
            return new DtoEventoResult { Success = false, Message = "La fecha fin no puede ser menor a la fecha inicio." };

        if (request.Orden < 1)
            return new DtoEventoResult { Success = false, Message = "El orden debe ser mayor a 0." };

        return new DtoEventoResult { Success = true, Message = "OK" };
    }
}
