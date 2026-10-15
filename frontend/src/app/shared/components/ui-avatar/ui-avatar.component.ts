import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

export type UiAvatarSize = 'xs' | 'sm' | 'md' | 'lg';
export type UiAvatarShape = 'circle' | 'rounded';

/**
 * Avatar reutilizable que admite URL o Base64 y conserva un respaldo por iniciales.
 * Si la imagen no carga, el cambio al respaldo ocurre dentro del mismo componente.
 */
@Component({
  selector: 'app-ui-avatar',
  standalone: true,
  templateUrl: './ui-avatar.component.html',
  styleUrl: './ui-avatar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiAvatarComponent {
  readonly nombre = input.required<string>();
  readonly fuente = input<string | null | undefined>(null);
  readonly textoAlternativo = input('');
  readonly size = input<UiAvatarSize>('md');
  readonly shape = input<UiAvatarShape>('circle');

  private readonly fuenteConError = signal<string | null>(null);

  readonly iniciales = computed(() => {
    const partes = this.nombre().trim().split(/\s+/).filter(Boolean);

    if (!partes.length) return '?';
    const primerNombre = partes[0] ?? '?';
    if (partes.length === 1) return primerNombre.slice(0, 2).toLocaleUpperCase('es');

    return `${primerNombre[0] ?? '?'}${partes.at(-1)?.[0] ?? ''}`.toLocaleUpperCase('es');
  });

  readonly fuenteImagen = computed(() => {
    const fuente = this.normalizarFuente(this.fuente());
    return fuente && fuente !== this.fuenteConError() ? fuente : null;
  });

  readonly clases = computed(() =>
    ['ui-avatar', `ui-avatar--${this.size()}`, `ui-avatar--${this.shape()}`].join(' '),
  );

  readonly alt = computed(
    () => this.textoAlternativo().trim() || `Fotografía de ${this.nombre().trim()}`,
  );

  registrarError(): void {
    this.fuenteConError.set(this.fuenteImagen());
  }

  private normalizarFuente(valor: string | null | undefined): string | null {
    const fuente = valor?.trim();
    if (!fuente) return null;

    // Una fuente ya preparada no requiere transformación.
    if (/^(data:image\/|https?:\/\/|blob:)/i.test(fuente)) return fuente;

    const fuenteCompacta = fuente.replace(/\s+/g, '');

    // Un JPEG Base64 normalmente comienza por /9j/. Esa barra inicial no es
    // una ruta del servidor: debe convertirse en data URI antes de asignarla
    // al atributo src para evitar solicitudes erróneas como GET /9j/4AAQ...
    const tipoImagen = this.resolverTipoImagenBase64(fuenteCompacta);
    if (tipoImagen) return `data:${tipoImagen};base64,${fuenteCompacta}`;

    // Solo después de descartar las firmas Base64 se acepta una ruta relativa.
    if (fuente.startsWith('/')) return fuente;

    // Compatibilidad con fotografías JPEG Base64 sin una firma reconocible.
    return `data:image/jpeg;base64,${fuenteCompacta}`;
  }

  private resolverTipoImagenBase64(valor: string): string | null {
    if (valor.startsWith('/9j/')) return 'image/jpeg';
    if (valor.startsWith('iVBORw0KGgo')) return 'image/png';
    if (valor.startsWith('R0lGOD')) return 'image/gif';
    if (valor.startsWith('UklGR')) return 'image/webp';
    if (valor.startsWith('Qk')) return 'image/bmp';
    if (valor.startsWith('PHN2Zy')) return 'image/svg+xml';

    return null;
  }
}
