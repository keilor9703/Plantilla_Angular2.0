import { Component, OnInit, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../../core/services/toast.service';
import {
  ModalService,
  DtoModal,
  DtoModalRequest,
  DtoModalInteraccion,
} from '../../../core/services/administracion/modal.service';
import { DominioService, DtoUnidad } from '../../../core/services/administracion/dominio.service';
import { AlertService } from '../../../core/services/alert.service';

@Component({
  selector: 'app-modal-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './modal-admin.html',
  styleUrls: ['./modal-admin.scss'],
})
export class ModalAdminComponent implements OnInit {
  minimized = false;
  loading = false;
  saving = false;
  showFormModal = false;

  lista: DtoModal[] = [];
  editingId: number | null = null;

  form: DtoModalRequest = this.formVacio();

  // Upload de recurso
  uploadingRecurso = false;
  recursoPreviewUrl = '';
  recursoFile: File | null = null;
  @ViewChild('fileInput') fileInputRef?: ElementRef<HTMLInputElement>;

  // Panel de auditoría
  auditoria: DtoModalInteraccion[] = [];
  auditoriaTitulo = '';
  loadingAuditoria = false;
  mostrarAuditoria = false;

  readonly tiposRecurso = ['TEXTO', 'IMAGEN', 'VIDEO'];
  readonly tiposAccion = ['INFO', 'ACEPTAR', 'CONFIRMAR'];
  unidades: DtoUnidad[] = [];
  unidadesLoading = false;

  // Table / pagination / search
  searchQuery = '';
  minSearchChars = 2;
  pageSizeOptions = [5, 10, 20, 30, 50, 100, 0]; // 0 => Todos
  pageSize = 10;
  currentPage = 1;
  // Actions menu
  openActionId: number | null = null;
  openActionAbove = false;
  private _docClickHandler?: (ev: MouseEvent) => void;

  get filteredModales(): DtoModal[] {
    const q = (this.searchQuery ?? '').trim().toLowerCase();
    if (q.length >= this.minSearchChars) {
      return (this.lista ?? []).filter((item) =>
        ((item.titulo ?? '') + ' ' + (item.tipoRecurso ?? '') + ' ' + (item.tipoAccion ?? '') + ' ' + (item.unidad ?? '') )
          .toLowerCase()
          .includes(q)
      );
    }
    return (this.lista ?? []).slice();
  }

  get totalFiltered(): number {
    return this.filteredModales.length;
  }

  get pageCount(): number {
    if (!this.pageSize || this.pageSize === 0) return 1;
    return Math.max(1, Math.ceil(this.totalFiltered / this.pageSize));
  }

  get pageNumbers(): Array<number | string> {
    const pages: Array<number | string> = [];
    const count = this.pageCount;
    for (let i = 1; i <= count; i++) pages.push(i);
    return pages;
  }

  get pagedModales(): DtoModal[] {
    const arr = this.filteredModales;
    if (!this.pageSize || this.pageSize === 0) return arr;
    const start = (this.currentPage - 1) * this.pageSize;
    return arr.slice(start, start + this.pageSize);
  }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  onPageSizeChange(v: number): void {
    this.pageSize = v;
    this.currentPage = 1;
  }

  canGoPrev(): boolean {
    return this.currentPage > 1;
  }

  canGoNext(): boolean {
    return this.currentPage < this.pageCount;
  }

  prevPage(): void {
    if (this.canGoPrev()) this.currentPage--;
  }

  nextPage(): void {
    if (this.canGoNext()) this.currentPage++;
  }

  goToPage(p: number | string): void {
    const n = Number(p);
    if (!Number.isNaN(n) && n >= 1 && n <= this.pageCount) this.currentPage = n;
  }

  constructor(
    private toast: ToastService,
    private modalService: ModalService,
    private dominioService: DominioService
    ,private alert: AlertService
  ) {}

  ngOnInit(): void {
    this.cargarUnidades();
    this.cargar();
    // close actions menu when clicking outside
    this._docClickHandler = (ev: MouseEvent) => {
      if (this.openActionId !== null) {
        this.openActionId = null;
        this.openActionAbove = false;
      }
    };
    document.addEventListener('click', this._docClickHandler);
  }

  ngOnDestroy(): void {
    if (this._docClickHandler) document.removeEventListener('click', this._docClickHandler);
  }

  toggleActions(itemId: number, event?: Event): void {
    if (event) event.stopPropagation();
    const willOpen = this.openActionId !== itemId;
    this.openActionId = willOpen ? itemId : null;
    // determine if popover should open upwards based on available space
    try {
      const el = (event && (event.currentTarget as HTMLElement)) || (event && (event.target as HTMLElement));
      if (willOpen && el) {
        const rect = el.getBoundingClientRect();
        const spaceBelow = window.innerHeight - rect.bottom;
        const spaceAbove = rect.top;
        const approxPopover = 220; // estimated popover height
        this.openActionAbove = spaceBelow < approxPopover && spaceAbove > spaceBelow;
      } else {
        this.openActionAbove = false;
      }
    } catch (e) {
      this.openActionAbove = false;
    }
  }

  isActionsOpen(itemId: number): boolean {
    return this.openActionId === itemId;
  }

  private cargarUnidades(): void {
    this.unidadesLoading = true;
    this.dominioService.getUnidades().subscribe({
      next: (items) => {
        this.unidades = (items ?? [])
          .slice()
          .sort((a, b) => (a.text ?? '').localeCompare(b.text ?? ''));
        this.unidadesLoading = false;
      },
      error: () => {
        this.unidades = [];
        this.unidadesLoading = false;
        this.toast.error('Modales', 'No fue posible cargar las unidades.');
      },
    });
  }

  cargar(): void {
    this.loading = true;
    this.modalService.getAll().subscribe({
      next: (data) => {
        this.lista = data ?? [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Error', 'No se pudieron cargar los modales');
      },
    });
  }

  nuevo(): void {
    this.editingId = null;
    this.limpiarPreviewLocal();
    this.form = this.formVacio();
    this.showFormModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  editar(item: DtoModal): void {
    this.editingId = item.idModal;
    this.limpiarPreviewLocal();
    this.form = {
      Titulo: item.titulo,
      Descripcion: item.descripcion,
      TipoRecurso: item.tipoRecurso,
      RutaRecurso: item.rutaRecurso,
      TipoAccion: item.tipoAccion,
      FechaInicio: item.fechaInicio?.substring(0, 16) ?? '',
      FechaFin: item.fechaFin?.substring(0, 16) ?? '',
      Unidad: item.unidad,
      Orden: item.orden,
      Vigente: item.vigente,
    };
    this.recursoPreviewUrl = item.rutaRecurso ?? '';
    this.recursoFile = null;
    this.showFormModal = true;
    // close actions menu if open
    this.openActionId = null;
    this.openActionAbove = false;
    try { document.body.classList.add('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  cerrarFormulario(): void {
    this.showFormModal = false;
    this.editingId = null;
    this.limpiarPreviewLocal();
    this.form = this.formVacio();
    try { document.body.classList.remove('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  private limpiarPreviewLocal(): void {
    if (this.recursoPreviewUrl && this.recursoPreviewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(this.recursoPreviewUrl);
    }
    this.recursoPreviewUrl = '';
    this.recursoFile = null;
  }

  toggleFormVigente(): void {
    this.form.Vigente = this.form.Vigente === 1 ? 0 : 1;
  }

  guardar(): void {
    if (!this.form.Titulo?.trim()) {
      this.toast.warning('Guardar', 'El título es requerido');
      return;
    }
    if (!this.form.FechaInicio || !this.form.FechaFin) {
      this.toast.warning('Guardar', 'Las fechas de vigencia son requeridas');
      return;
    }
    if (this.form.FechaFin < this.form.FechaInicio) {
      this.toast.warning('Guardar', 'La fecha fin no puede ser menor a la fecha inicio');
      return;
    }

    this.saving = true;
    const normalized = {
      ...this.form,
      FechaInicio: this.normalizarFecha(this.form.FechaInicio),
      FechaFin: this.normalizarFecha(this.form.FechaFin),
    };

    let prevRuta = '';
    const doSave = (rutaRecurso?: string) => {
      const payload: DtoModalRequest = { ...normalized, RutaRecurso: rutaRecurso ?? (this.form.RutaRecurso ?? '') };
      const op$ = this.editingId ? this.modalService.update(this.editingId, payload) : this.modalService.create(payload);
      op$.subscribe({
        next: (resp) => {
          this.saving = false;
          if (resp.success) {
            this.toast.success('Guardar', resp.message);
            // If there was a previous stored recurso (server URL) and it differs from the newly saved ruta, attempt deletion
            try {
              const prev = prevRuta ?? '';
              const newRuta = rutaRecurso ?? (this.form.RutaRecurso ?? '');
              if (prev && prev !== newRuta && prev.startsWith('/api/Modal/recurso/')) {
                const prevFile = prev.split('/').pop() ?? '';
                if (prevFile) {
                  this.modalService.deleteRecurso(prevFile).subscribe({
                    next: () => { /* best-effort delete */ },
                    error: () => { /* ignore */ },
                  });
                }
              }
            } catch (e) {
              // ignore
            }

            this.cerrarFormulario();
            this.cargar();
          } else {
            this.toast.warning('Guardar', resp.message);
          }
        },
        error: (err) => {
          this.saving = false;
          const msg = err?.error?.message ?? err?.message ?? 'Error al guardar el modal';
          this.toast.error('Guardar', msg);
        },
      });
    };

    // If there's a new local file selected, upload it first, then save
    if (this.recursoFile) {
      prevRuta = this.form.RutaRecurso ?? '';
      this.uploadingRecurso = true;
      this.modalService.uploadRecurso(this.recursoFile).subscribe({
        next: (resp) => {
          this.uploadingRecurso = false;
          if (!resp?.success) {
            this.saving = false;
            this.toast.warning('Recurso', resp?.message ?? 'No fue posible cargar el archivo.');
            return;
          }
          // after successful save, remove previously stored recurso if it exists and differs from new
          doSave(resp.url);
          // hook to delete prev file after save success: handled inside doSave's subscribe using closure prevRuta
        },
        error: () => {
          this.uploadingRecurso = false;
          this.saving = false;
          this.toast.error('Recurso', 'Error al cargar el archivo.');
        },
      });
    } else {
      // No new file — use existing RutaRecurso
      doSave();
    }
  }

  cambiarEstado(item: DtoModal): void {
    const nuevoEstado = item.vigente === 1 ? 0 : 1;
    this.modalService.toggleVigente(item.idModal, nuevoEstado).subscribe({
      next: (resp) => {
        if (resp.success) {
          this.toast.success('Estado', resp.message);
          this.cargar();
        } else {
          this.toast.warning('Estado', resp.message);
        }
      },
      error: () => this.toast.error('Estado', 'Error al cambiar estado'),
    });
  }

  async eliminar(item: DtoModal): Promise<void> {
    const confirmed = await this.alert.confirm(
      'Eliminar modal',
      `¿Está seguro de eliminar el modal "${item.titulo}"?`,
      'Eliminar',
      'Cancelar'
    );
    if (!confirmed) return;

    // If the user is editing this modal and has a local file selected, clear it locally
    if (this.editingId === item.idModal && (this.recursoFile || (this.recursoPreviewUrl && this.recursoPreviewUrl.startsWith('blob:')))) {
      this.nuevo();
    }

    this.modalService.delete(item.idModal).subscribe({
      next: (resp) => {
        if (resp.success) {
          this.toast.success('Eliminar', resp.message);
          // Attempt to delete server-side recurso if present
          try {
            const ruta = item.rutaRecurso ?? '';
            if (ruta && ruta.startsWith('/api/Modal/recurso/')) {
              const file = ruta.split('/').pop() ?? '';
              if (file) {
                this.modalService.deleteRecurso(file).subscribe({ next: () => {}, error: () => {} });
              }
            }
          } catch (e) { /* ignore */ }

          // clear editing form/preview if we deleted the currently edited item
          if (this.editingId === item.idModal) {
            this.cerrarFormulario();
          }
          this.cargar();
        } else {
          this.toast.warning('Eliminar', resp.message);
        }
      },
      error: () => this.toast.error('Eliminar', 'Error al eliminar el modal'),
    });
  }

  onTipoRecursoChange(): void {
    // When tipoRecurso changes clear any selected local recurso and reset input
    try {
      if (this.recursoPreviewUrl && this.recursoPreviewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(this.recursoPreviewUrl);
      }
    } catch (e) {
      // ignore
    }
    this.recursoPreviewUrl = '';
    this.recursoFile = null;
    this.form.RutaRecurso = '';

    if (this.fileInputRef && this.fileInputRef.nativeElement) {
      try { this.fileInputRef.nativeElement.value = ''; } catch (e) { /* ignore */ }
    }
  }

  abrirSelectorArchivo(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.click();
    }
  }

  onRecursoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const allowedImages = ['image/jpeg', 'image/png', 'image/webp'];
    const allowedVideos = ['video/mp4', 'video/webm', 'video/quicktime'];
    const esImagen = allowedImages.includes(file.type);
    const esVideo  = allowedVideos.includes(file.type);

    if (!esImagen && !esVideo) {
      this.toast.warning('Recurso', 'Formato no permitido. Use JPG, PNG, WEBP, MP4, WEBM o MOV.');
      input.value = '';
      return;
    }
    if (esImagen && file.size > 5 * 1024 * 1024) {
      this.toast.warning('Recurso', 'La imagen no puede exceder 5MB.');
      input.value = '';
      return;
    }
    if (esVideo && file.size > 150 * 1024 * 1024) {
      this.toast.warning('Recurso', 'El video no puede exceder 150MB.');
      input.value = '';
      return;
    }

    this.processSelectedFile(file);
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
  }

  onDragLeave(event: DragEvent): void {
    // Prevent default to avoid navigation and allow UI to remove drag state
    event.preventDefault();
  }

  onRecursoDropped(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (!file) return;
    this.processSelectedFile(file);
  }

  private processSelectedFile(file: File): void {
    const allowedImages = ['image/jpeg', 'image/png', 'image/webp'];
    const allowedVideos = ['video/mp4', 'video/webm', 'video/quicktime'];
    const esImagen = allowedImages.includes(file.type);
    const esVideo = allowedVideos.includes(file.type);

    if (!esImagen && !esVideo) {
      this.toast.warning('Recurso', 'Formato no permitido. Use JPG, PNG, WEBP, MP4, WEBM o MOV.');
      return;
    }
    if (esImagen && file.size > 5 * 1024 * 1024) {
      this.toast.warning('Recurso', 'La imagen no puede exceder 5MB.');
      return;
    }
    if (esVideo && file.size > 150 * 1024 * 1024) {
      this.toast.warning('Recurso', 'El video no puede exceder 150MB.');
      return;
    }

    try {
      if (this.recursoPreviewUrl && this.recursoPreviewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(this.recursoPreviewUrl);
      }
      this.recursoFile = file;
      this.form.TipoRecurso = esImagen ? 'IMAGEN' : 'VIDEO';
      this.recursoPreviewUrl = URL.createObjectURL(file);
    } catch (e) {
      this.toast.error('Recurso', 'Error preparando vista previa del recurso.');
      this.recursoFile = null;
      this.recursoPreviewUrl = '';
    }
  }

  removeSelectedRecurso(): void {
    if (this.recursoPreviewUrl && this.recursoPreviewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(this.recursoPreviewUrl);
    }
    this.recursoPreviewUrl = '';
    this.recursoFile = null;
    // also clear RutaRecurso so it won't be saved
    this.form.RutaRecurso = '';
  }

  verAuditoria(item: DtoModal): void {
    this.auditoriaTitulo = item.titulo;
    this.auditoria = [];
    this.loadingAuditoria = true;
    this.mostrarAuditoria = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) { /* ignore */ }

    this.modalService.getInteracciones(item.idModal).subscribe({
      next: (data) => {
        this.auditoria = data ?? [];
        this.loadingAuditoria = false;
      },
      error: () => {
        this.loadingAuditoria = false;
        this.toast.error('Auditoría', 'Error al cargar las interacciones');
      },
    });
  }

  cerrarAuditoria(): void {
    this.mostrarAuditoria = false;
    this.auditoria = [];
    try { document.body.classList.remove('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  iconoTipoAccion(tipo: string): string {
    const map: Record<string, string> = {
      VISTA: 'fa-eye',
      ACEPTAR: 'fa-check',
      CERRAR: 'fa-xmark',
    };
    return map[tipo] ?? 'fa-circle';
  }

  private formVacio(): DtoModalRequest {
    const inicio = new Date();
    const fin = new Date();
    fin.setDate(fin.getDate() + 5);
    fin.setHours(0, 0, 0, 0);
    return {
      Titulo: '',
      Descripcion: '',
      TipoRecurso: 'TEXTO',
      RutaRecurso: '',
      TipoAccion: 'INFO',
      FechaInicio: this.toDateTimeLocal(inicio),
      FechaFin: this.toDateTimeLocal(fin),
      Unidad: '',
      Orden: 1,
      Vigente: 1,
    };
  }

  private toDateTimeLocal(date: Date): string {
    const p = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${p(date.getMonth() + 1)}-${p(date.getDate())}T${p(date.getHours())}:${p(date.getMinutes())}`;
  }

  private normalizarFecha(valor: string): string {
    if (!valor) return valor;
    // datetime-local devuelve "YYYY-MM-DDTHH:mm" — .NET necesita segundos
    return valor.length === 16 ? `${valor}:00` : valor;
  }
}
