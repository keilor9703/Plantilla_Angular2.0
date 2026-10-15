import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  numberAttribute,
  output,
  signal,
} from '@angular/core';

import { UiFileUploadComponent } from '../ui-file-upload/ui-file-upload.component';

/**
 * Selector especializado y reutilizable para documentos PDF. La comprobación
 * del navegador mejora la experiencia; el backend repite todas las reglas y es
 * siempre la autoridad de seguridad.
 */
@Component({
  selector: 'app-ui-pdf-upload',
  standalone: true,
  imports: [UiFileUploadComponent],
  templateUrl: './ui-pdf-upload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiPdfUploadComponent {
  readonly label = input('Documento PDF');
  readonly hint = input('PDF · máximo 24 MB.');
  readonly error = input('');
  readonly fileName = input('');
  readonly successText = input('PDF validado y listo para adjuntar.');
  readonly buttonText = input('Buscar PDF');
  readonly maxSizeBytes = input(24 * 1024 * 1024, { transform: numberAttribute });
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly loading = input(false, { transform: booleanAttribute });

  readonly fileSelected = output<File>();
  readonly clearRequested = output<void>();
  readonly validationError = output<string>();

  private validationVersion = 0;
  private readonly validating = signal(false);

  /** Unifica la validación estructural local y cualquier carga externa. */
  readonly isProcessing = computed(() => this.loading() || this.validating());

  /**
   * Verifica las marcas estructurales mínimas antes de emitir el archivo. No
   * confía únicamente en la extensión o en el MIME informado por el navegador.
   */
  async inspectFile(file: File): Promise<void> {
    const version = ++this.validationVersion;
    this.validating.set(true);

    try {
      const header = new Uint8Array(await file.slice(0, 5).arrayBuffer());
      const tail = new TextDecoder('ascii').decode(
        await file.slice(Math.max(0, file.size - 4096)).arrayBuffer(),
      );
      const validHeader = String.fromCharCode(...header) === '%PDF-';
      const validEnd = tail.includes('%%EOF');

      if (version !== this.validationVersion) return;
      if (!validHeader || !validEnd) {
        this.clearRequested.emit();
        this.validationError.emit(
          'El archivo seleccionado no contiene una estructura PDF reconocible.',
        );
        return;
      }

      this.fileSelected.emit(file);
    } catch {
      if (version === this.validationVersion) {
        this.clearRequested.emit();
        this.validationError.emit('No fue posible leer el archivo seleccionado.');
      }
    } finally {
      if (version === this.validationVersion) {
        this.validating.set(false);
      }
    }
  }

  /** Un error básico del selector también invalida cualquier selección anterior. */
  handleSelectorError(message: string): void {
    this.validationVersion++;
    this.validating.set(false);
    this.clearRequested.emit();
    this.validationError.emit(message);
  }

  clear(): void {
    this.validationVersion++;
    this.validating.set(false);
    this.clearRequested.emit();
  }
}
