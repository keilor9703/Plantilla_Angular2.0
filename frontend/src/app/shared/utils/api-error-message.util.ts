import { HttpErrorResponse } from '@angular/common/http';

export interface ErrorApiNormalizado {
  readonly estado: number;
  readonly codigo: string;
  readonly titulo: string;
  readonly mensaje: string;
  readonly correlationId: string | null;
  readonly traceId: string | null;
  readonly providerCode: string | null;
}

const ENCABEZADO_CORRELACION = 'X-Correlation-ID';

/**
 * Convierte cualquier respuesta ProblemDetails en información segura y
 * coherente para la interfaz. Nunca presenta HttpErrorResponse.message,
 * nombres de excepciones ni contenido técnico recibido fuera del contrato.
 */
export function normalizarErrorApi(
  error: HttpErrorResponse,
  mensajeAlternativo = 'No fue posible completar la operación.',
): ErrorApiNormalizado {
  const cuerpo = isRecord(error.error) ? error.error : null;
  const estado = error.status || 0;
  const detalle = textoSeguro(cuerpo?.['detail'], 500);
  const mensajeServidor = textoSeguro(cuerpo?.['message'], 500);
  const validaciones = extractValidationMessages(cuerpo?.['errors']);
  const correlationId = correlationIdSeguro(
    cuerpo?.['correlationId'] ?? error.headers.get(ENCABEZADO_CORRELACION),
  );
  const traceId = textoSeguro(cuerpo?.['traceId'], 100) || null;
  const providerCode = codigoProveedorSeguro(cuerpo?.['providerCode']);
  const codigo = codigoSeguro(cuerpo?.['code']) || codigoPorEstado(estado);

  const contenido = contenidoPorEstado(estado, {
    detalle,
    mensajeServidor,
    validaciones,
    mensajeAlternativo,
  });

  return {
    estado,
    codigo,
    titulo: contenido.titulo,
    mensaje: contenido.mensaje,
    correlationId,
    traceId,
    providerCode,
  };
}

/** Agrega la referencia que soporte puede buscar sin mostrar datos técnicos. */
export function mensajeConCodigoSoporte(error: ErrorApiNormalizado): string {
  return error.correlationId
    ? `${error.mensaje} Informe al administrador y comparta este código: ${error.correlationId}.`
    : error.mensaje;
}

/**
 * Obtiene un mensaje seguro sin propagar estructuras `any` de HttpClient
 * hacia los componentes consumidores.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    return mensajeConCodigoSoporte(normalizarErrorApi(error, fallback));
  }

  return extractMessage(error) || fallback;
}

interface ContenidoDisponible {
  readonly detalle: string;
  readonly mensajeServidor: string;
  readonly validaciones: string;
  readonly mensajeAlternativo: string;
}

function contenidoPorEstado(
  estado: number,
  contenido: ContenidoDisponible,
): { readonly titulo: string; readonly mensaje: string } {
  switch (estado) {
    case 0:
      return {
        titulo: 'Sin conexión con el servicio',
        mensaje: 'No fue posible comunicarse con el sistema. Verifique su conexión e intente nuevamente.',
      };
    case 400:
    case 422:
      return {
        titulo: 'Revise la información',
        mensaje:
          contenido.validaciones ||
          contenido.detalle ||
          contenido.mensajeServidor ||
          contenido.mensajeAlternativo,
      };
    case 401:
      return {
        titulo: 'Sesión no válida',
        mensaje: 'Su sesión debe validarse nuevamente para continuar.',
      };
    case 403:
      return {
        titulo: 'Acceso no autorizado',
        mensaje: 'No tiene permisos para realizar esta acción.',
      };
    case 404:
      return {
        titulo: 'Información no encontrada',
        mensaje: contenido.detalle || contenido.mensajeAlternativo,
      };
    case 409:
      return {
        titulo: 'La información cambió',
        mensaje:
          contenido.detalle ||
          contenido.mensajeServidor ||
          'Actualice la información e intente nuevamente.',
      };
    case 429:
      return {
        titulo: 'Demasiadas solicitudes',
        mensaje: 'Espere un momento antes de intentarlo nuevamente.',
      };
    case 502:
    case 503:
      return {
        titulo: 'Servicio no disponible',
        mensaje: 'El servicio no está disponible temporalmente. Intente nuevamente más tarde.',
      };
    case 504:
      return {
        titulo: 'Tiempo de espera agotado',
        mensaje: 'La operación tardó más de lo esperado. Intente nuevamente.',
      };
    default:
      if (estado >= 500) {
        return {
          titulo: 'No fue posible completar la operación',
          mensaje: 'Ocurrió un inconveniente interno. Intente nuevamente.',
        };
      }

      return {
        titulo: 'No fue posible completar la operación',
        mensaje: contenido.detalle || contenido.mensajeAlternativo,
      };
  }
}

function codigoPorEstado(estado: number): string {
  switch (estado) {
    case 0:
      return 'NETWORK_UNAVAILABLE';
    case 400:
    case 422:
      return 'VALIDATION_FAILED';
    case 401:
      return 'AUTHENTICATION_REQUIRED';
    case 403:
      return 'ACCESS_DENIED';
    case 404:
      return 'RESOURCE_NOT_FOUND';
    case 409:
      return 'CONFLICT';
    case 429:
      return 'RATE_LIMIT_EXCEEDED';
    case 502:
      return 'BAD_GATEWAY';
    case 503:
      return 'DEPENDENCY_UNAVAILABLE';
    case 504:
      return 'GATEWAY_TIMEOUT';
    default:
      return estado >= 500 ? 'INTERNAL_ERROR' : 'HTTP_ERROR';
  }
}

function codigoSeguro(value: unknown): string {
  const codigo = textoSeguro(value, 80).toUpperCase();
  return /^[A-Z][A-Z0-9_]{1,79}$/.test(codigo) ? codigo : '';
}

/** Admite identificadores técnicos acotados, nunca mensajes del proveedor. */
function codigoProveedorSeguro(value: unknown): string | null {
  const codigo = textoSeguro(value, 32).toUpperCase();
  return /^(?:ORA-\d{5}|DB-[A-Z0-9-]{1,27})$/.test(codigo) ? codigo : null;
}

function correlationIdSeguro(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }

  const correlationId = value.trim().toLowerCase();
  return /^[a-f0-9]{32}$/.test(correlationId) ? correlationId : null;
}

function textoSeguro(value: unknown, longitudMaxima: number): string {
  return typeof value === 'string' ? value.trim().slice(0, longitudMaxima) : '';
}

function extractMessage(value: unknown): string {
  if (!isRecord(value)) {
    return typeof value === 'string' ? value.trim() : '';
  }

  const detail = value['detail'];
  const message = value['message'];

  const validationMessage = extractValidationMessages(value['errors']);
  if (validationMessage) {
    return validationMessage;
  }

  if (typeof detail === 'string' && detail.trim()) {
    return detail.trim();
  }

  if (typeof message === 'string' && message.trim()) {
    return message.trim();
  }

  const nestedError = value['error'];
  return nestedError === value ? '' : extractMessage(nestedError);
}

/** Resume los errores de FluentValidation sin mostrar estructuras técnicas. */
function extractValidationMessages(value: unknown): string {
  if (!isRecord(value)) return '';

  const messages = Object.values(value)
    .flatMap((item) => (Array.isArray(item) ? item : [item]))
    .filter((item): item is string => typeof item === 'string' && !!item.trim())
    .map((item) => item.trim());

  return [...new Set(messages)].slice(0, 3).join(' ');
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
