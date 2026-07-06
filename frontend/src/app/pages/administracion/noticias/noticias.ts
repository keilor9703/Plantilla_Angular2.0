import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Editor, NgxEditorModule, Toolbar } from 'ngx-editor';
import {
  NoticiaService,
  DtoNoticia,
  DtoNoticiaRequest
} from '../../../core/services/administracion/noticia.service';
import { ToastService } from '../../../core/services/toast.service';
import { DominioService, DtoDominio, DtoUnidad } from '../../../core/services/administracion/dominio.service';
import { DtoDepartamento, DtoMunicipio, UbicacionService } from '../../../core/services/administracion/ubicacion.service';
import { AlertService } from '../../../core/services/alert.service';

interface ImageItem {
  file?: File;
  name?: string;
}

@Component({
  selector: 'app-noticias',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxEditorModule],
  templateUrl: './noticias.html',
  styleUrls: ['./noticias.scss']
})
export class NoticiasAdminComponent implements OnInit, OnDestroy {
  private readonly maxImages = 4;
  private nombresOriginales: string[] = [];
  loading = false;
  saving = false;
  deleting = false;
  uploading = false;
  unidadesLoading = false;
  seccionesLoading = false;
  departamentosLoading = false;
  municipiosLoading = false;

  noticias: DtoNoticia[] = [];
  noticiaActual: DtoNoticia | null = null;
  isEditing = false;

  searchNoticias = '';
  private searchNoticiasTimeout: ReturnType<typeof setTimeout> | null = null;
  readonly minSearchChars = 3;
  pageSize = 10;
  pageSizeOptions: number[] = [5, 10, 20, 30, 50, 100];
  currentPage = 1;
  unidades: DtoUnidad[] = [];
  secciones: string[] = [];
  departamentos: DtoDepartamento[] = [];
  municipios: DtoMunicipio[] = [];
  selectedDepartamentoId: number | null = null;
  uploadedImages: ImageItem[] = [];
  imageDragOver = false;
  dragSlotIndex: number | null = null;
  draggedIndex: number | null = null;

  get slotIndices(): number[] {
    return [0, 1, 2, 3];
  }

  form: DtoNoticiaRequest = {
    unidad: '',
    titulo: '',
    seccion: '',
    ciudad: '',
    imagenNoticia: '',
    subtitulo: '',
    contenido: ''
  };

  editor!: Editor;
  editorToolbar: Toolbar = [
    [{ heading: ['h1', 'h2', 'h3'] }],
    ['bold', 'italic', 'underline'],
    ['blockquote'],
    ['ordered_list', 'bullet_list'],
    ['align_left', 'align_center', 'align_right'],
    ['link'],
    ['undo', 'redo']
  ];

  constructor(
    private service: NoticiaService,
    private dominioService: DominioService,
    private ubicacionService: UbicacionService,
    private toast: ToastService
    ,private alert: AlertService
  ) {}

  ngOnInit(): void {
    this.editor = new Editor();
    this.cargarCatalogos();
    this.cargar();
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
    if (this.searchNoticiasTimeout) {
      clearTimeout(this.searchNoticiasTimeout);
    }
    try { document.body.classList.remove('ui-modal-open'); } catch (e) {}
  }

  private cargarCatalogos(): void {
    this.cargarUnidades();
    this.cargarSecciones();
    this.cargarDepartamentos();
    this.cargarMunicipios(null);
  }

  private cargarDepartamentos(): void {
    this.departamentosLoading = true;
    this.ubicacionService.getDepartamentos().subscribe({
      next: (items) => {
        this.departamentos = (items ?? [])
          .sort((a, b) => (a?.nombre ?? '').localeCompare(b?.nombre ?? '', 'es', { sensitivity: 'base' }));
        this.departamentosLoading = false;
      },
      error: () => {
        this.departamentos = [];
        this.departamentosLoading = false;
        this.toast.error('Noticias', 'No fue posible cargar los departamentos.');
      }
    });
  }

  private cargarMunicipios(idDepartamento: number | null): void {
    this.municipiosLoading = true;
    this.ubicacionService.getMunicipios(idDepartamento).subscribe({
      next: (items) => {
        this.municipios = (items ?? [])
          .sort((a, b) => (a?.nombre ?? '').localeCompare(b?.nombre ?? '', 'es', { sensitivity: 'base' }));
        this.municipiosLoading = false;
      },
      error: () => {
        this.municipios = [];
        this.municipiosLoading = false;
        this.toast.error('Noticias', 'No fue posible cargar los municipios.');
      }
    });
  }

  private cargarUnidades(): void {
    this.unidadesLoading = true;
    this.dominioService.getUnidades().subscribe({
      next: (items) => {
        this.unidades = (items ?? [])
          .filter((x) => Number(x?.value ?? 0) > 0)
          .sort((a, b) => (a?.descripcion ?? '').localeCompare(b?.descripcion ?? '', 'es', { sensitivity: 'base' }));
        this.unidadesLoading = false;
      },
      error: () => {
        this.unidades = [];
        this.unidadesLoading = false;
        this.toast.error('Noticias', 'No fue posible cargar las unidades.');
      }
    });
  }

  private cargarSecciones(): void {
    this.seccionesLoading = true;
    this.dominioService.getAll().subscribe({
      next: (items) => {
        this.secciones = (items ?? [])
          .filter((x: DtoDominio) => Number(x?.idPadre ?? 0) === 151 && Number(x?.vigente ?? 0) === 1)
          .map((x: DtoDominio) => (x?.descripcion ?? '').trim())
          .filter((x: string) => !!x)
          .sort((a: string, b: string) => a.localeCompare(b, 'es', { sensitivity: 'base' }));
        this.seccionesLoading = false;
      },
      error: () => {
        this.secciones = [];
        this.seccionesLoading = false;
        this.toast.error('Noticias', 'No fue posible cargar las secciones.');
      }
    });
  }

  get noticiasFiltradas(): DtoNoticia[] {
    const term = (this.searchNoticias ?? '').trim().toLowerCase();
    if (!term || term.length < this.minSearchChars) return this.noticias;
    return this.noticias.filter(n =>
      (n.titulo ?? '').toLowerCase().includes(term) ||
      (n.unidad ?? '').toLowerCase().includes(term) ||
      (n.seccion ?? '').toLowerCase().includes(term) ||
      (n.ciudad ?? '').toLowerCase().includes(term)
    );
  }

  get totalNoticias(): number {
    return this.noticiasFiltradas.length;
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.totalNoticias / this.pageSize));
  }

  get pageNumbers(): Array<number | string> {
    const total = this.totalPaginas;
    const current = this.currentPage;
    const windowSize = 5;
    if (total <= windowSize * 2 + 5) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: Array<number | string> = [];
    pages.push(1);
    let start = Math.max(2, current - windowSize);
    let end = Math.min(total - 1, current + windowSize);
    if (start <= 2) start = 2;
    if (end >= total - 1) end = total - 1;
    if (start > 2) pages.push('...');
    for (let i = start; i <= end; i++) pages.push(i);
    if (end < total - 1) pages.push('...');
    pages.push(total);
    return pages;
  }

  get noticiasPaginadas(): DtoNoticia[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.noticiasFiltradas.slice(start, start + this.pageSize);
  }

  onSearchNoticiasChange(): void {
    if (this.searchNoticiasTimeout) clearTimeout(this.searchNoticiasTimeout);
    this.searchNoticiasTimeout = setTimeout(() => {
      this.currentPage = 1;
    }, 350);
  }

  onPageSizeChange(size: number | string): void {
    const n = Number(size) || 10;
    const lastPage = Math.max(1, Math.ceil(this.totalNoticias / n));
    this.pageSize = n;
    this.currentPage = Math.min(this.currentPage, lastPage);
    if (this.noticiasPaginadas.length === 0 && this.totalNoticias > 0) {
      this.currentPage = lastPage;
    }
  }

  goToPage(page: number): void {
    if (!Number.isFinite(page) || page < 1 || page > this.totalPaginas) return;
    this.currentPage = page;
  }

  onPageClick(p: number | string): void {
    if (p === '...') return;
    const n = Number(p);
    if (Number.isFinite(n)) this.goToPage(n);
  }

  canGoPrev(): boolean { return this.currentPage > 1; }
  canGoNext(): boolean { return this.currentPage < this.totalPaginas; }
  prevPagina(): void { if (this.canGoPrev()) this.currentPage -= 1; }
  nextPagina(): void { if (this.canGoNext()) this.currentPage += 1; }

  cargar(): void {
    this.loading = true;
    this.service.getActivas().subscribe({
      next: (data) => {
        this.noticias = data;
        this.currentPage = 1;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Noticias', err?.error?.message ?? 'Error cargando noticias.');
      }
    });
  }

  nuevaNoticia(): void {
    this.noticiaActual = null;
    this.form = {
      unidad: '',
      titulo: '',
      seccion: '',
      ciudad: '',
      imagenNoticia: '',
      subtitulo: '',
      contenido: ''
    };
    this.selectedDepartamentoId = null;
    this.uploadedImages = [];
    this.nombresOriginales = [];
    this.cargarMunicipios(null);
    this.isEditing = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) {}
  }

  editarNoticia(noticia: DtoNoticia): void {
    this.noticiaActual = noticia;
    this.form = {
      unidad: noticia.unidad,
      titulo: noticia.titulo,
      seccion: noticia.seccion,
      ciudad: noticia.ciudad,
      imagenNoticia: noticia.imagenNoticia ?? '',
      subtitulo: noticia.subtitulo ?? '',
      contenido: noticia.contenido ?? ''
    };
    this.selectedDepartamentoId = null;
    this.nombresOriginales = this.parseImageList(noticia.imagenNoticia);
    this.uploadedImages = this.nombresOriginales.map(n => ({ name: n }));
    this.cargarMunicipiosParaEdicion((noticia.ciudad ?? '').trim());
    this.isEditing = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) {}
  }

  private cargarMunicipiosParaEdicion(ciudadActual: string): void {
    this.municipiosLoading = true;
    this.ubicacionService.getMunicipios(null).subscribe({
      next: (items) => {
        this.municipios = (items ?? [])
          .sort((a, b) => (a?.nombre ?? '').localeCompare(b?.nombre ?? '', 'es', { sensitivity: 'base' }));

        if (ciudadActual) {
          const found = this.municipios.find((m) => (m?.nombre ?? '').trim().toLowerCase() === ciudadActual.toLowerCase());
          if (found?.idDepartamento) {
            this.selectedDepartamentoId = found.idDepartamento;
            this.cargarMunicipios(this.selectedDepartamentoId);
          }
        }

        this.municipiosLoading = false;
      },
      error: () => {
        this.municipios = [];
        this.municipiosLoading = false;
        this.toast.error('Noticias', 'No fue posible cargar los municipios.');
      }
    });
  }

  onDepartamentoChange(): void {
    this.form.ciudad = '';
    this.cargarMunicipios(this.selectedDepartamentoId);
  }

  cancelar(): void {
    this.isEditing = false;
    this.noticiaActual = null;
    this.uploadedImages = [];
    this.nombresOriginales = [];
    try { document.body.classList.remove('ui-modal-open'); } catch (e) {}
  }

  guardar(): void {
    const titulo = (this.form.titulo ?? '').trim();
    const seccion = (this.form.seccion ?? '').trim();
    const ciudad = (this.form.ciudad ?? '').trim();
    const unidad = (this.form.unidad ?? '').trim();

    if (!titulo) {
      this.toast.warning('Noticias', 'El título es obligatorio.');
      return;
    }
    if (!seccion) {
      this.toast.warning('Noticias', 'La sección es obligatoria.');
      return;
    }
    if (!ciudad) {
      this.toast.warning('Noticias', 'La ciudad es obligatoria.');
      return;
    }
    if (!this.selectedDepartamentoId || this.selectedDepartamentoId <= 0) {
      this.toast.warning('Noticias', 'El departamento es obligatorio.');
      return;
    }
    if (!unidad) {
      this.toast.warning('Noticias', 'La unidad es obligatoria.');
      return;
    }

    const payload: DtoNoticiaRequest = {
      titulo,
      seccion,
      ciudad,
      unidad,
      imagenNoticia: this.uploadedImages.length > 0 ? this.uploadedImages.join('|') : null,
      subtitulo: (this.form.subtitulo ?? '').trim() || null,
      contenido: (this.form.contenido ?? '').trim() || null
    };

    this.saving = true;
    this.subirImagenesPendientes(() => {
      const nombres = this.uploadedImages.map(i => i.name).filter(Boolean) as string[];
      const payload: DtoNoticiaRequest = {
        titulo,
        seccion,
        ciudad,
        unidad,
        imagenNoticia: nombres.length > 0 ? nombres.join('|') : null,
        subtitulo: (this.form.subtitulo ?? '').trim() || null,
        contenido: (this.form.contenido ?? '').trim() || null
      };

      const request$ = this.noticiaActual
        ? this.service.update(this.noticiaActual.idNoticia, payload)
        : this.service.create(payload);

      request$.subscribe({
        next: (resp) => {
          this.saving = false;
          if (!resp.success) {
            this.toast.warning('Noticias', resp.message ?? 'No fue posible guardar.');
            return;
          }
          this.toast.success('Noticias', resp.message ?? 'Guardado correctamente.');
          this.isEditing = false;
          try { document.body.classList.remove('ui-modal-open'); } catch (e) {}
          this.noticiaActual = null;
          this.eliminarImagenesRemovidas();
          this.cargar();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error('Noticias', err?.error?.message ?? 'Error guardando noticia.');
        }
      });
    });
  }

  eliminarNoticia(noticia: DtoNoticia): void {
    (async () => {
      const confirmed = await this.alert.confirm(
        'Eliminar noticia',
        `¿Está seguro de eliminar la noticia "${noticia.titulo}"?`,
        'Eliminar',
        'Cancelar'
      );
      if (!confirmed) return;

      this.deleting = true;
      const imagenes = this.parseImageList(noticia.imagenNoticia);
      this.service.delete(noticia.idNoticia).subscribe({
        next: (resp) => {
          this.deleting = false;
          if (!resp.success) {
            this.toast.warning('Noticias', resp.message ?? 'No fue posible eliminar.');
            return;
          }
          this.toast.success('Noticias', 'Noticia eliminada correctamente.');
          this.eliminarImagenes(imagenes);
          this.cargar();
        },
        error: (err) => {
          this.deleting = false;
          this.toast.error('Noticias', err?.error?.message ?? 'Error eliminando noticia.');
        }
      });
    })();
  }

  getSeccionClass(seccion: string): string {
    switch (seccion.toLowerCase()) {
      case 'comunicado': return 'tag-comunicado';
      case 'servicio': return 'tag-servicio';
      case 'importante': return 'tag-importante';
      case 'evento': return 'tag-evento';
      case 'galería': return 'tag-galeria';
      default: return '';
    }
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    input.value = '';
    if (files.length === 0) {
      return;
    }
    this.agregarArchivos(files);
  }

  onImageDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.add('drag-over');
    this.imageDragOver = true;
  }

  onImageDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.imageDragOver = false;
  }

  onImageDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.imageDragOver = false;
    const files = event.dataTransfer ? Array.from(event.dataTransfer.files) : [];
    if (files.length > 0) {
      this.agregarArchivos(files);
    }
  }

  onSlotDragStart(event: DragEvent, index: number): void {
    this.draggedIndex = index;
    event.dataTransfer?.setData('text/plain', String(index));
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  onSlotDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.draggedIndex !== null && this.draggedIndex !== index) {
      this.dragSlotIndex = index;
    }
  }

  onSlotDragLeave(event: DragEvent, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.dragSlotIndex === index) {
      this.dragSlotIndex = null;
    }
  }

  onSlotDrop(event: DragEvent, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragSlotIndex = null;
    const files = event.dataTransfer ? Array.from(event.dataTransfer.files) : [];
    if (files.length > 0) {
      this.agregarArchivos(files);
    } else if (this.draggedIndex !== null && this.draggedIndex !== index) {
      this.reordenar(this.draggedIndex, index);
    }
    this.draggedIndex = null;
  }

  onSlotClick(event: MouseEvent, index: number): void {
    if (!this.uploadedImages[index]) {
      const input = document.getElementById('imagenNoticia') as HTMLInputElement;
      input?.click();
    }
  }

  removeUploadedImage(index: number): void {
    if (index < 0 || index >= this.uploadedImages.length) {
      return;
    }
    this.uploadedImages.splice(index, 1);
  }

  private reordenar(from: number, to: number): void {
    const item = this.uploadedImages.splice(from, 1)[0];
    this.uploadedImages.splice(to, 0, item);
  }

  private agregarArchivos(files: File[]): void {
    const availableSlots = this.maxImages - this.uploadedImages.length;
    if (availableSlots <= 0) {
      this.toast.warning('Imagen', `Solo se permiten ${this.maxImages} imágenes por noticia.`);
      return;
    }
    const filesToAdd = files.slice(0, availableSlots);
    if (files.length > availableSlots) {
      this.toast.warning('Imagen', `Se cargarán solo ${availableSlots} imágenes para completar el máximo de ${this.maxImages}.`);
    }
    for (const f of filesToAdd) {
      this.uploadedImages.push({ file: f });
    }
  }

  getUploadedImageUrl(item: ImageItem): string {
    if (item.file) {
      return URL.createObjectURL(item.file);
    }
    const raw = (item.name ?? '').trim();
    if (!raw) {
      return '';
    }
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('/api/')) {
      return raw;
    }
    const justName = raw.split('/').filter(Boolean).pop() ?? '';
    return justName ? `/api/NoticiaUpload/Imagen/${encodeURIComponent(justName)}` : '';
  }

  private subirImagenesPendientes(onComplete: () => void): void {
    const pendientes = this.uploadedImages.filter(i => i.file);
    if (pendientes.length === 0) {
      onComplete();
      return;
    }
    this.uploading = true;
    let completadas = 0;
    for (const item of pendientes) {
      this.service.uploadImage(item.file!).subscribe({
        next: (resp) => {
          if (resp.success) {
            item.name = resp.fileName;
            item.file = undefined;
          } else {
            this.toast.warning('Imagen', resp.message || 'Error al cargar imagen.');
          }
          completadas++;
          if (completadas >= pendientes.length) {
            this.uploading = false;
            onComplete();
          }
        },
        error: (err) => {
          this.toast.error('Imagen', err?.error?.message || 'Error al cargar imagen.');
          completadas++;
          if (completadas >= pendientes.length) {
            this.uploading = false;
            onComplete();
          }
        }
      });
    }
  }

  private parseImageList(rawValue?: string | null): string[] {
    const raw = (rawValue ?? '').trim();
    if (!raw) {
      return [];
    }
    return raw
      .split('|')
      .map((x) => x.trim())
      .filter((x) => !!x)
      .slice(0, this.maxImages);
  }

  private eliminarImagenes(nombres: string[]): void {
    for (const n of nombres) {
      const soloNombre = n.split('/').filter(Boolean).pop() ?? n;
      this.service.deleteImage(soloNombre).subscribe({
        error: (err) => console.error('Error eliminando imagen:', soloNombre, err)
      });
    }
  }

  private eliminarImagenesRemovidas(): void {
    if (this.nombresOriginales.length === 0) return;
    const nombresActuales = this.uploadedImages.map(i => i.name).filter(Boolean) as string[];
    const removidas = this.nombresOriginales.filter(n => !nombresActuales.includes(n));
    this.eliminarImagenes(removidas);
    this.nombresOriginales = [];
  }
}
