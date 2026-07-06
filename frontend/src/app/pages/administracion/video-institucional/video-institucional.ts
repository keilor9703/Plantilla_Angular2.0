import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SafeUrlPipe } from '../../../shared/pipes/safe-url.pipe';
import {
  VideoInstitucionalService,
  DtoVideoInstitucional,
  DtoVideoInstitucionalRequest
} from '../../../core/services/administracion/video-institucional.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-video-institucional',
  standalone: true,
  imports: [CommonModule, FormsModule, SafeUrlPipe],
  templateUrl: './video-institucional.html',
  styleUrls: ['./video-institucional.scss']
})
export class VideoInstitucionalComponent implements OnInit {
  minimized = false;
  loading = false;
  saving = false;
  deleting = false;
  isEditing = false;
  isCreatingNew = false;
  loadingListado = false;

  current: DtoVideoInstitucional | null = null;
  videosListado: DtoVideoInstitucional[] = [];
  // Pagination / search / grouping
  searchQuery = '';
  minSearchChars = 2;
  pageSizeOptions = [5, 10, 20, 30, 50, 100, 0]; // 0 === Todos
  pageSize = 10;
  currentPage = 1;

  form: DtoVideoInstitucionalRequest = {
    titulo: '',
    descripcion: '',
    urlYoutube: ''
  };

  constructor(
    private service: VideoInstitucionalService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.cargar();
    this.cargarListado();
  }

  cargar(): void {
    this.loading = true;
    this.service.getCurrent().subscribe({
      next: (resp) => {
        this.current = resp.hasVideo ? (resp.data ?? null) : null;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Video institucional', err?.error?.message ?? 'Error consultando el video institucional.');
      }
    });
  }

  activarEdicion(): void {
    if (this.current) {
      this.form = {
        titulo: this.current.titulo,
        descripcion: this.current.descripcion ?? '',
        urlYoutube: this.current.urlYoutube
      };
      this.isCreatingNew = false;
    } else {
      this.form = { titulo: '', descripcion: '', urlYoutube: '' };
      this.isCreatingNew = true;
    }
    this.isEditing = true;
  }
  
  cargarListado(): void {
    this.loadingListado = true;
    this.service.getListado().subscribe({
      next: (items) => {
        this.videosListado = items ?? [];
        this.currentPage = 1;
        this.loadingListado = false;
      },
      error: (err) => {
        this.loadingListado = false;
        this.videosListado = [];
        this.toast.error('Video institucional', err?.error?.message ?? 'Error consultando listado de videos.');
      }
    });
  }

  // Search / Pagination helpers
  get filteredVideos(): DtoVideoInstitucional[] {
    const q = (this.searchQuery || '').trim().toLowerCase();
    if (!q || q.length < this.minSearchChars) return this.videosListado.slice();
    return this.videosListado.filter(v =>
      (v.titulo ?? '').toLowerCase().includes(q)
      || (v.urlYoutube ?? '').toLowerCase().includes(q)
      || (v.usuarioCreacion ?? '').toLowerCase().includes(q)
    );
  }

  get totalFiltered(): number {
    return this.filteredVideos.length;
  }

  get pageNumbers(): Array<number|string> {
    if (this.pageSize === 0) return [1];
    const total = Math.max(1, Math.ceil(this.totalFiltered / this.pageSize));
    const pages: Array<number|string> = [];
    for (let i = 1; i <= total; i++) pages.push(i);
    return pages;
  }

  get canGoPrev(): boolean { return this.pageSize !== 0 && this.currentPage > 1; }
  get canGoNext(): boolean { return this.pageSize !== 0 && this.currentPage < Math.ceil(this.totalFiltered / this.pageSize); }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  onPageSizeChange(size: number): void {
    this.pageSize = Number(size);
    this.currentPage = 1;
  }

  prevPage(): void { if (this.canGoPrev) this.currentPage--; }
  nextPage(): void { if (this.canGoNext) this.currentPage++; }
  goToPage(p: any): void {
    const n = Number(p);
    if (!Number.isInteger(n) || n < 1) return;
    this.currentPage = n;
  }

  get pagedVideos(): DtoVideoInstitucional[] {
    if (this.pageSize === 0) return this.filteredVideos.slice();
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredVideos.slice(start, start + this.pageSize);
  }

  nuevoVideo(): void {
    this.form = { titulo: '', descripcion: '', urlYoutube: '' };
    this.isCreatingNew = true;
    this.isEditing = true;
  }

  cancelar(): void {
    this.isEditing = false;
    this.isCreatingNew = false;
    this.form = { titulo: '', descripcion: '', urlYoutube: '' };
  }

  guardar(): void {
    const titulo = (this.form.titulo ?? '').trim();
    const urlYoutube = (this.form.urlYoutube ?? '').trim();

    if (!titulo) {
      this.toast.warning('Video institucional', 'El título es obligatorio.');
      return;
    }
    if (!urlYoutube) {
      this.toast.warning('Video institucional', 'La URL de YouTube es obligatoria.');
      return;
    }

    const payload: DtoVideoInstitucionalRequest = {
      titulo,
      descripcion: (this.form.descripcion ?? '').trim() || null,
      urlYoutube
    };

    this.saving = true;

    const request$ = this.current && !this.isCreatingNew
      ? this.service.update(this.current.idVideoInst, payload)
      : this.service.create(payload);

    request$.subscribe({
      next: (resp) => {
        this.saving = false;
        if (!resp.success) {
          this.toast.warning('Video institucional', resp.message ?? 'No fue posible guardar.');
          return;
        }
        this.toast.success('Video institucional', resp.message ?? 'Guardado correctamente.');
        this.isEditing = false;
        this.isCreatingNew = false;
        this.cargar();
        this.cargarListado();
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Video institucional', err?.error?.message ?? 'Error guardando el video institucional.');
      }
    });
  }

  eliminar(): void {
    if (!this.current) return;

    this.deleting = true;
    this.service.delete(this.current.idVideoInst).subscribe({
      next: (resp) => {
        this.deleting = false;
        if (!resp.success) {
          this.toast.warning('Video institucional', resp.message ?? 'No fue posible eliminar.');
          return;
        }
        this.toast.success('Video institucional', resp.message ?? 'Video eliminado correctamente.');
        this.current = null;
        this.isEditing = false;
        this.cargarListado();
      },
      error: (err) => {
        this.deleting = false;
        this.toast.error('Video institucional', err?.error?.message ?? 'Error eliminando el video institucional.');
      }
    });
  }
}
