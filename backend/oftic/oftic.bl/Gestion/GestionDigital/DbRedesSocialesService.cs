using Comun.Dtos;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbRedesSocialesService : IDbRedesSocialesService
{
    private readonly IDbRedesSocialesRepository _repository;

    public DbRedesSocialesService(IDbRedesSocialesRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DtoRedesSociales>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DtoRedesSociales?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DtoRedesSocialesResult> CreateAsync(DtoRedesSociales redesSociales, string usuario)
    {
        return await _repository.CreateAsync(redesSociales, usuario);
    }

    public async Task<DtoRedesSocialesResult> UpdateAsync(DtoRedesSociales redesSociales, string usuario)
    {
        return await _repository.UpdateAsync(redesSociales, usuario);
    }

    public async Task<DtoRedesSocialesResult> DeleteAsync(int id, string usuario)
    {
        return await _repository.DeleteAsync(id, usuario);
    }

    public async Task<IEnumerable<DtoDominio>> GetDominiosAsync()
    {
        return await _repository.GetDominiosAsync();
    }

    public async Task<IEnumerable<DtoUnidad>> GetUnidadesAsync()
    {
        return await _repository.GetUnidadesAsync();
    }
}