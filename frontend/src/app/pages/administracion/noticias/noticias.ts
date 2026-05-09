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

@Component({
  selector: 'app-noticias',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxEditorModule],
  templateUrl: './noticias.html',
  styleUrls: ['./noticias.scss']
})
export class NoticiasAdminComponent implements OnInit, OnDestroy {
  private readonly maxImages = 4;
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
  unidades: DtoUnidad[] = [];
  secciones: string[] = [];
  departamentos: DtoDepartamento[] = [];
  municipios: DtoMunicipio[] = [];
  selectedDepartamentoId: number | null = null;
  uploadedImages: string[] = [];

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
  ) {}

  ngOnInit(): void {
    this.editor = new Editor();
    this.cargarCatalogos();
    this.cargar();
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
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
          .sort((a, b) => (a?.text ?? '').localeCompare(b?.text ?? '', 'es', { sensitivity: 'base' }));
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

  cargar(): void {
    this.loading = true;
    this.service.getAll().subscribe({
      next: (data) => {
        this.noticias = data;
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
    this.cargarMunicipios(null);
    this.isEditing = true;
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
    this.uploadedImages = this.parseImageList(noticia.imagenNoticia);
    this.cargarMunicipiosParaEdicion((noticia.ciudad ?? '').trim());
    this.isEditing = true;
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
        this.noticiaActual = null;
        this.cargar();
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Noticias', err?.error?.message ?? 'Error guardando noticia.');
      }
    });
  }

  eliminarNoticia(noticia: DtoNoticia): void {
    if (!confirm(`¿Está seguro de eliminar la noticia "${noticia.titulo}"?`)) {
      return;
    }

    this.deleting = true;
    this.service.delete(noticia.idNoticia).subscribe({
      next: (resp) => {
        this.deleting = false;
        if (!resp.success) {
          this.toast.warning('Noticias', resp.message ?? 'No fue posible eliminar.');
          return;
        }
        this.toast.success('Noticias', resp.message ?? 'Noticia eliminada correctamente.');
        this.cargar();
      },
      error: (err) => {
        this.deleting = false;
        this.toast.error('Noticias', err?.error?.message ?? 'Error eliminando noticia.');
      }
    });
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
    if (files.length === 0) {
      return;
    }

    const availableSlots = this.maxImages - this.uploadedImages.length;
    if (availableSlots <= 0) {
      this.toast.warning('Imagen', `Solo se permiten ${this.maxImages} imágenes por noticia.`);
      input.value = '';
      return;
    }

    const filesToUpload = files.slice(0, availableSlots);
    if (files.length > availableSlots) {
      this.toast.warning('Imagen', `Se cargarán solo ${availableSlots} imágenes para completar el máximo de ${this.maxImages}.`);
    }

    this.uploading = true;
    this.uploadFilesSequential(filesToUpload, 0, () => {
      this.uploading = false;
      this.form.imagenNoticia = this.uploadedImages.join('|');
      input.value = '';
    });
  }

  removeUploadedImage(index: number): void {
    if (index < 0 || index >= this.uploadedImages.length) {
      return;
    }

    this.uploadedImages.splice(index, 1);
    this.form.imagenNoticia = this.uploadedImages.join('|');
  }

  getUploadedImageUrl(fileName: string): string {
    const raw = (fileName ?? '').trim();
    if (!raw) {
      return '';
    }

    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('/api/')) {
      return raw;
    }

    const justName = raw.split('/').filter(Boolean).pop() ?? '';
    return justName ? `/api/NoticiaUpload/Imagen/${encodeURIComponent(justName)}` : '';
  }

  private uploadFilesSequential(files: File[], index: number, onDone: () => void): void {
    if (index >= files.length) {
      onDone();
      return;
    }

    this.service.uploadImage(files[index]).subscribe({
      next: (resp) => {
        if (resp.success) {
          this.uploadedImages.push(resp.fileName);
        } else {
          this.toast.warning('Imagen', resp.message || 'Error al cargar imagen.');
        }
        this.uploadFilesSequential(files, index + 1, onDone);
      },
      error: (err) => {
        this.toast.error('Imagen', err?.error?.message || 'Error al cargar imagen.');
        this.uploadFilesSequential(files, index + 1, onDone);
      }
    });
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
}
