import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { forkJoin } from 'rxjs';
import {
  DtoSaveSliderRequest,
  DtoSliders,
  SliderService
} from '../../../core/services/administracion/slider.service';
import { ToastService } from '../../../core/services/toast.service';

type SliderForm = DtoSaveSliderRequest;

@Component({
  selector: 'app-sliders',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './sliders.html',
  styleUrls: ['./sliders.scss']
})
export class SlidersComponent implements OnInit {
  minimized = false;
  visible = true;
  isEditing = false;
  loading = false;
  saving = false;
  uploadingImage = false;

  allSliders: DtoSliders[] = [];
  activeSliders: DtoSliders[] = [];
  originalActiveOrder: DtoSliders[] = [];
  editingId: number | null = null;
  replacementSourceId: number | null = null;
  replacementSourceUrl = '';
  imagePreviewUrl = '';
  selectedImageFile: File | null = null;
  originalImageUrl = '';
  reorderSaving = false;

  searchSliders = '';
  readonly minSearchChars = 3;
  pageSize = 10;
  pageSizeOptions: number[] = [5, 10, 20, 30, 50, 100];
  currentPage = 1;

  form: SliderForm = this.getDefaultForm();
  openActionsId: number | null = null;

  constructor(
    private sliderService: SliderService,
    private toast: ToastService
  ) {}

  probarUrlDestino(): void {
    const url = (this.form.urlDestino ?? '').trim();
    if (!url) {
      return;
    }

    let targetUrl = url;
    if (!/^https?:\/\//i.test(targetUrl)) {
      targetUrl = `https://${targetUrl}`;
    }

    window.open(targetUrl, '_blank');
  }

  ngOnInit(): void {
    this.loadSliders();
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  closePanel(): void {
    this.visible = false;
  }

  loadSliders(): void {
    this.loading = true;
    this.sliderService.getAdmin().subscribe({
      next: (data) => {
        this.allSliders = (data ?? []).slice().sort((a, b) => a.orden - b.orden);
        const active = this.allSliders
          .filter((item) => this.isActiveSlider(item))
          .sort((a, b) => a.orden - b.orden);
        this.activeSliders = active.slice(0, 7);
        this.originalActiveOrder = [...this.activeSliders];
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error(
          'Sliders',
          err?.error?.message ?? 'No fue posible cargar los sliders.'
        );
      }
    });
  }

  nuevo(): void {
    this.replacementSourceId = null;
    this.replacementSourceUrl = '';
    const activeCount = this.allSliders.filter((item) => item.vigente === 1).length;
    if (activeCount >= 7) {
      this.toast.warning('Sliders', 'Ya hay 7 sliders activos. Primero desactive un slider para crear uno nuevo.');
      return;
    }

    this.editingId = null;
    this.originalImageUrl = '';
    this.form = this.getDefaultForm();
    this.cleanupSelectedImage();
    this.imagePreviewUrl = '';
    this.isEditing = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) {}
  }

  reemplazar(item: DtoSliders): void {
    this.replacementSourceId = item.idSlider;
    this.replacementSourceUrl = (item.urlImagen ?? '').trim();
    this.editingId = null;
    this.originalImageUrl = (item.urlImagen ?? '').trim();
    this.form = this.getDefaultForm();
    this.form.orden = item.orden || 1;
    this.cleanupSelectedImage();
    this.imagePreviewUrl = '';
    this.isEditing = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) {}
  }

  editar(item: DtoSliders): void {
    this.openActionsId = null;
    this.replacementSourceId = null;
    this.replacementSourceUrl = '';
    this.editingId = item.idSlider;
    this.originalImageUrl = (item.urlImagen ?? '').trim();
    this.form = {
      idSlider: item.idSlider,
      titulo: (item.titulo ?? '').trim(),
      subtitulo: (item.subtitulo ?? '').trim(),
      urlImagen: this.originalImageUrl,
      urlDestino: (item.urlDestino ?? '').trim(),
      orden: item.orden || 1,
      vigente: item.vigente || 0,
      fechaInicio: this.normalizeDateTime(item.fechaInicio),
      fechaFin: this.normalizeDateTime(item.fechaFin)
    };
    this.cleanupSelectedImage();
    this.imagePreviewUrl = this.form.urlImagen || '';
    this.isEditing = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) {}
  }

  guardar(): void {
    if (!this.form.titulo.trim()) {
      this.toast.warning('Sliders', 'El título es obligatorio.');
      return;
    }

    if (this.selectedImageFile === null && !this.form.urlImagen.trim()) {
      this.toast.warning('Sliders', 'La imagen es obligatoria.');
      return;
    }

    if (!this.form.orden || this.form.orden < 1) {
      this.toast.warning('Sliders', 'El orden debe ser mayor a 0.');
      return;
    }

    if (!this.form.fechaInicio || !this.form.fechaFin) {
      this.toast.warning('Sliders', 'La fecha/hora de inicio y fin son obligatorias.');
      return;
    }

    if (new Date(this.form.fechaFin).getTime() < new Date(this.form.fechaInicio).getTime()) {
      this.toast.warning('Sliders', 'La vigencia fin no puede ser menor a la vigencia inicio.');
      return;
    }

    const activeCountExcludingCurrent = this.allSliders.filter((item) => item.vigente === 1 && item.idSlider !== this.form.idSlider).length;
    if (this.form.vigente === 1 && !this.replacementSourceId && activeCountExcludingCurrent >= 7) {
      this.toast.warning('Sliders', 'Solo se permiten 7 sliders activos. Desactive uno antes de activar otro o edite un slider existente.');
      return;
    }

    const oldImageUrl = this.selectedImageFile ? this.originalImageUrl : '';

    const createPayload = (imageUrl: string): DtoSaveSliderRequest => ({
      idSlider: this.form.idSlider ?? undefined,
      titulo: this.form.titulo.trim(),
      subtitulo: (this.form.subtitulo ?? '').trim() || null,
      urlImagen: imageUrl.trim(),
      urlDestino: (this.form.urlDestino ?? '').trim() || null,
      orden: this.form.orden,
      vigente: this.form.vigente,
      fechaInicio: (this.form.fechaInicio ?? '').trim() || null,
      fechaFin: (this.form.fechaFin ?? '').trim() || null
    });

    const finishSave = () => {
      this.saving = false;
      this.isEditing = false;
      this.editingId = null;
      this.replacementSourceId = null;
      this.cleanupSelectedImage();
      try { document.body.classList.remove('ui-modal-open'); } catch (e) {}
      this.form = this.getDefaultForm();
      this.imagePreviewUrl = '';
      this.loadSliders();
    };

    const handleSaveError = (err: any, uploadedFileName?: string) => {
      if (uploadedFileName) {
        this.sliderService.deleteImage(uploadedFileName).subscribe();
      }
      this.saving = false;
      this.uploadingImage = false;
      this.toast.error(
        'Sliders',
        err?.error?.detail ?? err?.error?.message ?? 'Error guardando slider.'
      );
    };

    const performSave = (imageUrl: string, uploadedFileName?: string) => {
      const payload = createPayload(imageUrl);
      const replacementSourceUrl = this.replacementSourceUrl;

      const saveAction = () => {
        this.sliderService.save(payload).subscribe({
          next: (resp) => {
            if (!resp?.success) {
              handleSaveError({ error: resp }, uploadedFileName);
              return;
            }

            if (uploadedFileName && oldImageUrl && oldImageUrl !== imageUrl) {
              this.deleteLocalImageByUrl(oldImageUrl);
            }

            if (replacementSourceUrl) {
              this.deleteLocalImageByUrl(replacementSourceUrl);
            }

            this.toast.success('Sliders', resp.message || 'Slider guardado correctamente.');
            finishSave();
          },
          error: (err) => handleSaveError(err, uploadedFileName)
        });
      };

      if (this.replacementSourceId) {
        const deactivation$ = this.sliderService.setEstado({ idSlider: this.replacementSourceId, vigente: 0 });
        deactivation$.subscribe({
          next: () => saveAction(),
          error: (err) => handleSaveError(err, uploadedFileName)
        });
      } else {
        saveAction();
      }
    };

    this.saving = true;
    if (this.selectedImageFile) {
      this.uploadingImage = true;
      this.sliderService.upload(this.selectedImageFile).subscribe({
        next: (resp) => {
          this.uploadingImage = false;
          if (!resp?.success || !resp?.url) {
            this.saving = false;
            this.toast.warning('Sliders', resp?.message || 'No fue posible cargar la imagen.');
            return;
          }
          this.form.urlImagen = resp.url;
          performSave(resp.url, resp.fileName);
        },
        error: (err) => {
          this.uploadingImage = false;
          this.saving = false;
          this.toast.error(
            'Sliders',
            err?.error?.detail ?? err?.error?.message ?? 'Error cargando imagen.'
          );
        }
      });
    } else {
      performSave(this.form.urlImagen);
    }
  }

  cambiarEstado(item: DtoSliders): void {
    this.openActionsId = null;
    const nuevoEstado = item.vigente === 1 ? 0 : 1;
    this.sliderService.setEstado({ idSlider: item.idSlider, vigente: nuevoEstado }).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Sliders', resp?.message || 'No fue posible actualizar estado.');
          return;
        }
        if (nuevoEstado === 0 && item.urlImagen) {
          this.deleteLocalImageByUrl(item.urlImagen);
        }
        this.toast.success('Sliders', resp.message || 'Estado actualizado.');
        this.loadSliders();
      },
      error: (err) => {
        this.toast.error(
          'Sliders',
          err?.error?.detail ?? err?.error?.message ?? 'Error actualizando estado.'
        );
      }
    });
  }

  toggleActions(id: number, event: Event): void {
    event.stopPropagation();
    this.openActionsId = this.openActionsId === id ? null : id;
  }

  isActionsOpen(id: number): boolean {
    return this.openActionsId === id;
  }

  onDropActive(event: CdkDragDrop<DtoSliders[]>): void {
    moveItemInArray(this.activeSliders, event.previousIndex, event.currentIndex);
  }

  hasActiveOrderChanged(): boolean {
    return this.activeSliders.some((item, index) => item.idSlider !== this.originalActiveOrder[index]?.idSlider);
  }

  saveActiveOrder(): void {
    if (this.activeSliders.length === 0) {
      return;
    }

    const updated = this.activeSliders.map((item, index) => ({
      ...item,
      orden: index + 1
    }));

    const requests = updated.map((item) => this.sliderService.save({
      idSlider: item.idSlider,
      titulo: (item.titulo ?? '').trim(),
      subtitulo: (item.subtitulo ?? '').trim() || null,
      urlImagen: (item.urlImagen ?? '').trim(),
      urlDestino: (item.urlDestino ?? '').trim() || null,
      orden: item.orden,
      vigente: item.vigente,
      fechaInicio: item.fechaInicio ?? null,
      fechaFin: item.fechaFin ?? null
    }));

    this.reorderSaving = true;
    forkJoin(requests).subscribe({
      next: (responses) => {
        this.reorderSaving = false;
        const success = responses.every((resp) => resp?.success);
        if (!success) {
          this.toast.warning('Sliders', 'No fue posible guardar el nuevo orden de todos los sliders.');
          return;
        }
        this.toast.success('Sliders', 'Orden de sliders guardado correctamente.');
        this.loadSliders();
      },
      error: (err) => {
        this.reorderSaving = false;
        this.toast.error('Sliders', err?.error?.detail ?? err?.error?.message ?? 'Error guardando orden.');
      }
    });
  }

  resetActiveOrder(): void {
    this.activeSliders = [...this.originalActiveOrder];
  }

  get filteredSliders(): DtoSliders[] {
    const inactiveSliders = this.allSliders.filter((item) => item.vigente !== 1);
    const term = (this.searchSliders ?? '').trim().toLowerCase();
    if (!term || term.length < this.minSearchChars) {
      return inactiveSliders;
    }
    return inactiveSliders.filter((item) =>
      (item.titulo ?? '').toLowerCase().includes(term) ||
      (item.subtitulo ?? '').toLowerCase().includes(term) ||
      (item.urlDestino ?? '').toLowerCase().includes(term) ||
      (item.fechaInicio ?? '').toLowerCase().includes(term) ||
      (item.fechaFin ?? '').toLowerCase().includes(term)
    );
  }

  get totalSliders(): number {
    return this.filteredSliders.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalSliders / this.pageSize));
  }

  get pageNumbers(): Array<number | string> {
    const total = this.totalPages;
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

  get slidersPaged(): DtoSliders[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredSliders.slice(start, start + this.pageSize);
  }

  onSearchSlidersChange(): void {
    this.currentPage = 1;
  }

  onPageSizeChange(size: number | string): void {
    const n = Number(size) || 10;
    const lastPage = Math.max(1, Math.ceil(this.totalSliders / n));
    this.pageSize = n;
    this.currentPage = Math.min(this.currentPage, lastPage);
    if (this.slidersPaged.length === 0 && this.totalSliders > 0) {
      this.currentPage = lastPage;
    }
  }

  goToPage(page: number): void {
    if (!Number.isFinite(page) || page < 1 || page > this.totalPages) return;
    this.currentPage = page;
  }

  onPageClick(p: number | string): void {
    if (p === '...') return;
    const n = Number(p);
    if (Number.isFinite(n)) this.goToPage(n);
  }

  prevPage(): void {
    if (this.canGoPrev()) {
      this.currentPage -= 1;
    }
  }

  nextPage(): void {
    if (this.canGoNext()) {
      this.currentPage += 1;
    }
  }

  canGoPrev(): boolean { return this.currentPage > 1; }
  canGoNext(): boolean { return this.currentPage < this.totalPages; }

  get modalTitle(): string {
    if (this.replacementSourceId) {
      return 'Reemplazar slider';
    }
    return this.editingId ? 'Editar slider' : 'Nuevo slider';
  }

  limpiarForm(): void {
    this.form = this.getDefaultForm();
    this.cleanupSelectedImage();
    this.imagePreviewUrl = this.form.urlImagen || '';
  }

  private isActiveSlider(item: DtoSliders): boolean {
    if (item.vigente !== 1) {
      return false;
    }
    if (!item.fechaInicio || !item.fechaFin) {
      return false;
    }
    const inicio = new Date(item.fechaInicio).getTime();
    const fin = new Date(item.fechaFin).getTime();
    const now = new Date().getTime();
    return inicio <= now && fin >= now;
  }

  getTiempoRestante(item: DtoSliders): string {
    if (!item.fechaFin) {
      return '0d 0h 0m';
    }

    const fin = new Date(item.fechaFin).getTime();
    const now = new Date().getTime();
    let diff = fin - now;
    if (diff <= 0) {
      return '0d 0h 0m';
    }

    const msPorMinuto = 1000 * 60;
    const msPorHora = msPorMinuto * 60;
    const msPorDia = msPorHora * 24;
    const dias = Math.floor(diff / msPorDia);
    diff -= dias * msPorDia;
    const horas = Math.floor(diff / msPorHora);
    diff -= horas * msPorHora;
    const minutos = Math.floor(diff / msPorMinuto);

    return `${dias}d ${horas}h ${minutos}m`;
  }

  cancelar(): void {
    this.isEditing = false;
    this.editingId = null;
    this.replacementSourceId = null;
    this.replacementSourceUrl = '';
    this.originalImageUrl = '';
    this.cleanupSelectedImage();
    try { document.body.classList.remove('ui-modal-open'); } catch (e) {}
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Sliders', 'Formato inválido. Use JPG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.toast.warning('Sliders', 'La imagen excede 5MB.');
      input.value = '';
      return;
    }

    if (this.selectedImageFile && this.imagePreviewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(this.imagePreviewUrl);
    }
    this.selectedImageFile = file;
    this.imagePreviewUrl = URL.createObjectURL(file);
    input.value = '';
  }

  private cleanupSelectedImage(): void {
    if (this.imagePreviewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(this.imagePreviewUrl);
    }
    this.selectedImageFile = null;
    this.imagePreviewUrl = '';
  }

  private extractFileName(url: string): string {
    if (!url) {
      return '';
    }

    try {
      const parsed = new URL(url, window.location.origin);
      return parsed.pathname.split('/').pop() ?? '';
    } catch {
      return url.split(/[?#]/)[0].split('/').pop() ?? '';
    }
  }

  private deleteLocalImageByUrl(url: string): void {
    const fileName = this.extractFileName(url);
    if (!fileName) {
      return;
    }

    this.sliderService.deleteImage(fileName).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Sliders', `No se eliminó la imagen ${fileName}: ${resp?.message || 'sin detalle'}`);
        }
      },
      error: (err) => {
        this.toast.warning('Sliders', `Error eliminando imagen ${fileName}.`);
        console.error('Error eliminando imagen slider:', fileName, err);
      }
    });
  }

  private getDefaultForm(): SliderForm {
    const today = new Date();
    const plusSeven = new Date(today);
    plusSeven.setDate(today.getDate() + 7);

    return {
      idSlider: null,
      titulo: '',
      subtitulo: '',
      urlImagen: '',
      urlDestino: '',
      orden: 1,
      vigente: 1,
      fechaInicio: this.toInputDateTime(today),
      fechaFin: this.toInputDateTime(plusSeven)
    };
  }

  private normalizeDateTime(value?: string | null): string {
    if (!value) {
      return '';
    }
    const str = value.toString().trim();
    if (!str) {
      return '';
    }

    // Caso común: "YYYY-MM-DDTHH:mm:ss..." -> datetime-local "YYYY-MM-DDTHH:mm"
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(str)) {
      return str.slice(0, 16);
    }

    const parsed = new Date(str);
    if (Number.isNaN(parsed.getTime())) {
      return '';
    }
    return this.toInputDateTime(parsed);
  }

  private toInputDateTime(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }
}


