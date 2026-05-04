using Comun.Dtos.Administracion;

namespace Datos.Interfaz
{
    public interface IDbCuentaEmailRepository
    {
        List<DtoCuentaEmail> ConsultarCuentas(int idCuenta);
        bool GestionarCuenta(DtoCuentaEmailRequest request);
        bool EliminarCuenta(int idCuenta);
        string GetNextRadicado(int idCuentaEmail);
        string GetSiglaSistema();
    }
}
