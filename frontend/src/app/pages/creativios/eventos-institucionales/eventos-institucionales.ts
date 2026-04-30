import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { DominioService, DtoUnidad } from '../../../core/services/administracion/dominio.service';
import { DtoDepartamento, DtoMunicipio, UbicacionService } from '../../../core/services/administracion/ubicacion.service';
import { UsuarioAdminService } from '../../../core/services/administracion/usuario-admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { DtoEvento, DtoEventoRequest, EventoService } from '../../../core/services/administracion/evento.service';

interface EventoForm {
  nombreEvento: string;
  fechaInicio: string;
  fechaFinal: string;
  unidadId: number | null;
  departamentoId: number | null;
  municipioId: number | null;
  imagenEvento: File | null;
  cedulaResponsable: string;
  nombreResponsable: string;
  correoResponsable: string;
  telefonoResponsable: string;
  direccionEvento: string;
}

@Component({
  selector: 'app-eventos-institucionales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './eventos-institucionales.html',
  styleUrls: ['./eventos-institucionales.scss'],
})
export class EventosInstitucionalesComponent implements OnInit {
  unidades: DtoUnidad[] = [];
  departamentos: DtoDepartamento[] = [];
  municipios: DtoMunicipio[] = [];
  municipiosCatalogo: DtoMunicipio[] = [];

  unidadesLoading = false;
  departamentosLoading = false;
  municipiosLoading = false;
  buscandoResponsable = false;
  guardando = false;
  subiendoImagen = false;
  eventosLoading = false;
  eventos: DtoEvento[] = [];
  eventoExpandidoId: number | null = null;
  editandoEventoId: number | null = null;
  vigenteEdicion = 1;
  private ultimaCedulaConsultada = '';

  imagenPreviewUrl = '';
  imagenPortadaUrl: string | null = null;

  form: EventoForm = {
    nombreEvento: '',
    fechaInicio: '',
    fechaFinal: '',
    unidadId: null,
    departamentoId: null,
    municipioId: null,
    imagenEvento: null,
    cedulaResponsable: '',
    nombreResponsable: '',
    correoResponsable: '',
    telefonoResponsable: '',
    direccionEvento: '',
  };

  constructor(
    private dominioService: DominioService,
    private ubicacionService: UbicacionService,
    private usuarioAdminService: UsuarioAdminService,
    private toast: ToastService,
    private eventoService: EventoService
  ) {}

  ngOnInit(): void {
    this.cargarUnidades();
    this.cargarDepartamentos();
    this.cargarMunicipiosCatalogo();
    this.cargarEventos();
  }

  cargarUnidades(): void {
    this.unidadesLoading = true;
    this.dominioService.getUnidades()
      .pipe(finalize(() => (this.unidadesLoading = false)))
      .subscribe({
        next: (items) => {
          this.unidades = (items ?? []).slice().sort((a, b) => (a.text ?? '').localeCompare(b.text ?? ''));
        },
        error: () => {
          this.unidades = [];
          this.toast.error('Eventos', 'No fue posible cargar las unidades.');
        },
      });
  }

  cargarDepartamentos(): void {
    this.departamentosLoading = true;
    this.ubicacionService.getDepartamentos()
      .pipe(finalize(() => (this.departamentosLoading = false)))
      .subscribe({
        next: (items) => {
          this.departamentos = (items ?? []).slice().sort((a, b) => (a.nombre ?? '').localeCompare(b.nombre ?? ''));
        },
        error: () => {
          this.departamentos = [];
          this.toast.error('Eventos', 'No fue posible cargar los departamentos.');
        },
      });
  }

  onChangeDepartamento(): void {
    this.form.municipioId = null;
    const idDepto = Number(this.form.departamentoId ?? 0);
    if (!idDepto) {
      this.municipios = [];
      return;
    }

    this.municipiosLoading = true;
    this.ubicacionService.getMunicipios(idDepto)
      .pipe(finalize(() => (this.municipiosLoading = false)))
      .subscribe({
        next: (items) => {
          this.municipios = (items ?? []).slice().sort((a, b) => (a.nombre ?? '').localeCompare(b.nombre ?? ''));
        },
        error: () => {
          this.municipios = [];
          this.toast.error('Eventos', 'No fue posible cargar las ciudades del departamento.');
        },
      });
  }

  onChangeImagen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    this.form.imagenEvento = file;
    this.imagenPreviewUrl = '';

    if (!file) {
      this.imagenPortadaUrl = null;
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.imagenPreviewUrl = String(reader.result ?? '');
    };
    reader.readAsDataURL(file);

    this.subiendoImagen = true;
    this.eventoService.uploadImage(file)
      .pipe(finalize(() => (this.subiendoImagen = false)))
      .subscribe({
        next: (resp) => {
          if (!resp?.success || !resp?.url) {
            this.imagenPortadaUrl = null;
            this.toast.warning('Eventos', resp?.message ?? 'No fue posible cargar la imagen.');
            return;
          }
          this.imagenPortadaUrl = resp.url;
          this.toast.success('Eventos', 'Imagen cargada correctamente.');
        },
        error: (err) => {
          this.imagenPortadaUrl = null;
          this.toast.error('Eventos', err?.error?.message ?? 'Error cargando imagen del evento.');
        },
      });
  }

  private cargarMunicipiosCatalogo(): void {
    this.ubicacionService.getMunicipios(null).subscribe({
      next: (items) => {
        this.municipiosCatalogo = (items ?? []).slice();
      },
      error: () => {
        this.municipiosCatalogo = [];
      },
    });
  }

  buscarResponsable(): void {
    const cedula = (this.form.cedulaResponsable ?? '').trim();
    if (!cedula) {
      this.toast.warning('Eventos', 'Ingrese la cédula del responsable.');
      return;
    }

    this.buscandoResponsable = true;
    this.usuarioAdminService.consultarUsuarioPorIdentificacion(cedula)
      .pipe(finalize(() => (this.buscandoResponsable = false)))
      .subscribe({
        next: (resp) => {
          const funcionario = resp?.funcionario ?? {};
          const nombres = (funcionario.nombres ?? '').trim();
          const apellidos = (funcionario.apellidos ?? '').trim();
          const fullName = `${nombres} ${apellidos}`.trim();
          const grado = (funcionario.nombreGrado ?? funcionario.gradAlfabetico ?? '').trim();

          this.form.nombreResponsable = `${grado} ${fullName}`.trim();
          this.form.correoResponsable = (funcionario.correo ?? '').trim();
          this.form.telefonoResponsable = (funcionario.celular ?? '').trim();

          if (!this.form.nombreResponsable) {
            this.toast.warning('Eventos', 'No se encontró funcionario con esa cédula.');
            return;
          }
          this.ultimaCedulaConsultada = cedula;
          this.toast.success('Eventos', 'Datos del responsable cargados.');
        },
        error: () => {
          this.form.nombreResponsable = '';
          this.form.correoResponsable = '';
          this.form.telefonoResponsable = '';
          this.ultimaCedulaConsultada = '';
          this.toast.error('Eventos', 'Error consultando el responsable.');
        },
      });
  }

  buscarResponsableEnBlur(): void {
    const cedula = (this.form.cedulaResponsable ?? '').trim();
    if (!cedula || this.buscandoResponsable || cedula === this.ultimaCedulaConsultada) {
      return;
    }
    this.buscarResponsable();
  }

  guardarEvento(): void {
    const nombre = this.form.nombreEvento.trim();
    const fechaInicio = this.form.fechaInicio.trim();
    const fechaFinal = this.form.fechaFinal.trim();
    const unidadId = Number(this.form.unidadId ?? 0);
    const departamentoId = Number(this.form.departamentoId ?? 0);
    const municipioId = Number(this.form.municipioId ?? 0);
    const cedula = this.form.cedulaResponsable.trim();
    const direccion = this.form.direccionEvento.trim();

    if (!nombre || !fechaInicio || !fechaFinal || !unidadId || !departamentoId || !municipioId || !cedula || !direccion) {
      this.toast.warning('Eventos', 'Complete todos los campos obligatorios.');
      return;
    }

    if (fechaFinal < fechaInicio) {
      this.toast.warning('Eventos', 'La fecha final no puede ser menor a la fecha de inicio.');
      return;
    }

    if (!this.form.nombreResponsable) {
      this.toast.warning('Eventos', 'Consulte y valide el responsable por cédula antes de guardar.');
      return;
    }

    if (this.subiendoImagen) {
      this.toast.info('Eventos', 'Espere a que termine la carga de la imagen.');
      return;
    }

    const unidadNombre = this.getUnidadNombre(unidadId);
    const ciudadNombre = this.getMunicipioNombre(municipioId);
    if (!unidadNombre) {
      this.toast.warning('Eventos', 'Seleccione una unidad válida.');
      return;
    }
    if (!ciudadNombre) {
      this.toast.warning('Eventos', 'Seleccione una ciudad válida.');
      return;
    }

    const departamentoNombre = this.getDepartamentoNombre(departamentoId);
    if (!departamentoNombre) {
      this.toast.warning('Eventos', 'Seleccione un departamento válido.');
      return;
    }

    const payload: DtoEventoRequest = {
      nombreEvento: nombre,
      unidad: unidadNombre,
      departamento: departamentoNombre,
      ciudad: ciudadNombre,
      direccionEvento: direccion,
      cedulaResponsable: this.form.cedulaResponsable.trim(),
      nombreResponsable: this.form.nombreResponsable.trim(),
      correoResponsable: this.form.correoResponsable.trim() || null,
      telefonoResponsable: this.form.telefonoResponsable.trim() || null,
      imagenEvento: this.imagenPortadaUrl,
      fechaInicio,
      fechaFin: fechaFinal,
      vigente: this.editandoEventoId ? this.vigenteEdicion : 1,
    };

    this.guardando = true;
    const op$ = this.editandoEventoId
      ? this.eventoService.update(this.editandoEventoId, payload)
      : this.eventoService.create(payload);

    op$
      .pipe(finalize(() => (this.guardando = false)))
      .subscribe({
        next: (resp) => {
          if (!resp?.success) {
            this.toast.warning('Eventos', resp?.message ?? 'No fue posible guardar el evento.');
            return;
          }
          this.toast.success('Eventos', resp.message ?? 'Evento guardado correctamente.');
          this.limpiarFormulario();
          this.cargarEventos();
        },
        error: (err) => {
          this.toast.error('Eventos', err?.error?.message ?? 'Error guardando el evento.');
        },
      });
  }

  cargarEventos(): void {
    this.eventosLoading = true;
    this.eventoService.getAll()
      .pipe(finalize(() => (this.eventosLoading = false)))
      .subscribe({
        next: (items) => {
          this.eventos = (items ?? []).slice();
        },
        error: () => {
          this.eventos = [];
          this.toast.error('Eventos', 'No fue posible cargar la tabla de eventos.');
        },
      });
  }

  toggleImagenEvento(evento: DtoEvento): void {
    this.eventoExpandidoId = this.eventoExpandidoId === evento.idEvento ? null : evento.idEvento;
  }

  editarEvento(evento: DtoEvento): void {
    this.editandoEventoId = evento.idEvento;
    this.vigenteEdicion = evento.vigente ?? 1;
    this.form.nombreEvento = evento.nombreEvento ?? '';
    this.form.fechaInicio = (evento.fechaInicio ?? '').toString().substring(0, 10);
    this.form.fechaFinal = (evento.fechaFin ?? '').toString().substring(0, 10);
    this.form.direccionEvento = evento.direccionEvento ?? '';
    this.form.nombreResponsable = evento.nombreResponsable ?? '';
    this.form.telefonoResponsable = evento.telefonoResponsable ?? '';
    this.form.correoResponsable = evento.correoResponsable ?? '';
    this.form.cedulaResponsable = evento.cedulaResponsable ?? '';
    this.imagenPortadaUrl = evento.imagenEvento ?? null;
    this.imagenPreviewUrl = this.getImagenEventoUrl(evento);

    const unidad = this.unidades.find((u) =>
      (u.descripcion || u.text || '').trim().toLowerCase() === (evento.unidad || '').trim().toLowerCase()
    );
    this.form.unidadId = unidad?.value ?? null;

    const municipio = this.municipiosCatalogo.find((m) =>
      (m.nombre || '').trim().toLowerCase() === (evento.ciudad || '').trim().toLowerCase()
    );
    if (municipio?.idDepartamento) {
      this.form.departamentoId = municipio.idDepartamento;
      this.onChangeDepartamento();
      this.form.municipioId = municipio.idMunicipio ?? null;
    } else {
      this.form.municipioId = null;
    }

    this.toast.info('Eventos', `Editando: ${evento.nombreEvento}`);
  }

  eliminarEvento(evento: DtoEvento): void {
    if (!evento?.idEvento) return;
    if (!confirm(`¿Eliminar el evento "${evento.nombreEvento}"?`)) return;

    this.guardando = true;
    this.eventoService.delete(evento.idEvento)
      .pipe(finalize(() => (this.guardando = false)))
      .subscribe({
        next: (resp) => {
          if (!resp?.success) {
            this.toast.warning('Eventos', resp?.message ?? 'No fue posible eliminar el evento.');
            return;
          }
          this.toast.success('Eventos', resp.message ?? 'Evento eliminado correctamente.');
          if (this.editandoEventoId === evento.idEvento) {
            this.limpiarFormulario();
          }
          this.cargarEventos();
        },
        error: (err) => {
          this.toast.error('Eventos', err?.error?.message ?? 'Error eliminando el evento.');
        },
      });
  }

  getTelefonoResponsable(evento: DtoEvento): string {
    return (evento.telefonoResponsable ?? '').trim();
  }

  getDepartamentoEvento(evento: DtoEvento): string {
    return (evento.departamento ?? '').trim();
  }

  getNombreResponsable(evento: DtoEvento): string {
    return (evento.nombreResponsable ?? '').trim();
  }

  getImagenEventoUrl(evento: DtoEvento): string {
    const raw = (evento.imagenEvento ?? '').trim();
    if (!raw) return '';
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('/')) return raw;
    return `/uploads/eventos/${raw}`;
  }

  private getUnidadNombre(id: number): string {
    const unidad = this.unidades.find((u) => Number(u.value) === id);
    return (unidad?.descripcion || unidad?.text || '').trim();
  }

  private getMunicipioNombre(id: number): string {
    const municipio = this.municipios.find((m) => Number(m.idMunicipio) === id);
    return (municipio?.nombre || '').trim();
  }

  private getDepartamentoNombre(id: number): string {
    const departamento = this.departamentos.find((d) => Number(d.idDepartamento) === id);
    return (departamento?.nombre || '').trim();
  }

  limpiarFormulario(): void {
    this.editandoEventoId = null;
    this.vigenteEdicion = 1;
    this.form = {
      nombreEvento: '',
      fechaInicio: '',
      fechaFinal: '',
      unidadId: null,
      departamentoId: null,
      municipioId: null,
      imagenEvento: null,
      cedulaResponsable: '',
      nombreResponsable: '',
      correoResponsable: '',
      telefonoResponsable: '',
      direccionEvento: '',
    };
    this.municipios = [];
    this.imagenPreviewUrl = '';
    this.imagenPortadaUrl = null;
    this.ultimaCedulaConsultada = '';
  }
}

