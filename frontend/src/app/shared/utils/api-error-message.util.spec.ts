import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';

import {
  getApiErrorMessage,
  normalizarErrorApi,
} from './api-error-message.util';

describe('api-error-message.util', () => {
  it('presenta un error 503 seguro con su código de soporte', () => {
    const error = new HttpErrorResponse({
      status: 503,
      statusText: 'Service Unavailable',
      error: {
        code: 'DATABASE_UNAVAILABLE',
        providerCode: 'ORA-12170',
        detail: 'ORA-12170: dato interno que no debe mostrarse',
        correlationId: '619c0acea2994f84a5f6e9f590f92d42',
      },
    });

    const mensaje = getApiErrorMessage(error, 'Mensaje alternativo');

    expect(mensaje).toContain('El servicio no está disponible temporalmente.');
    expect(mensaje).toContain(
      'Informe al administrador y comparta este código: 619c0acea2994f84a5f6e9f590f92d42.',
    );
    expect(mensaje).not.toContain('ORA-12170');
    expect(normalizarErrorApi(error).providerCode).toBe('ORA-12170');
  });

  it('rechaza como código técnico cualquier mensaje no perteneciente al contrato', () => {
    const error = new HttpErrorResponse({
      status: 503,
      error: { providerCode: 'ORA-01400: no se puede insertar NULL en TABLA.COLUMNA' },
    });

    expect(normalizarErrorApi(error).providerCode).toBeNull();
  });

  it('resume hasta tres validaciones funcionales', () => {
    const error = new HttpErrorResponse({
      status: 400,
      statusText: 'Bad Request',
      error: {
        errors: {
          fecha: ['Seleccione una fecha válida.'],
          motivo: ['Ingrese el motivo.', 'El motivo supera el tamaño permitido.'],
          otro: ['Este cuarto mensaje no debe incluirse.'],
        },
      },
    });

    expect(getApiErrorMessage(error, 'Revise la información.')).toBe(
      'Seleccione una fecha válida. Ingrese el motivo. El motivo supera el tamaño permitido.',
    );
  });

  it('usa el encabezado de correlación cuando el cuerpo no lo incluye', () => {
    const error = new HttpErrorResponse({
      status: 500,
      statusText: 'Internal Server Error',
      headers: new HttpHeaders({
        'X-Correlation-ID': '0123456789abcdef0123456789abcdef',
      }),
      error: { code: 'INTERNAL_ERROR' },
    });

    expect(normalizarErrorApi(error).correlationId).toBe('0123456789abcdef0123456789abcdef');
  });
});
