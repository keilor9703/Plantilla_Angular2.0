using Comun.Dtos;

namespace Datos.Interfaz;

public interface IDbRedesSocialesRepository
{
    Task<IEnumerable<DtoRedesSociales>> GetAllAsync();
    Task<DtoRedesSociales?> GetByIdAsync(int id);
    Task<DtoRedesSocialesResult> CreateAsync(DtoRedesSociales redesSociales, string usuario);
    Task<DtoRedesSocialesResult> UpdateAsync(DtoRedesSociales redesSociales, string usuario);
    Task<DtoRedesSocialesResult> DeleteAsync(int id, string usuario);
    Task<IEnumerable<DtoDominio>> GetDominiosAsync();
    Task<IEnumerable<DtoUnidad>> GetUnidadesAsync();
}