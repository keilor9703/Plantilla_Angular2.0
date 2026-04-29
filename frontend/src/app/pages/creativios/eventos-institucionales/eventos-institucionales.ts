import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { DominioService, DtoUnidad } from '../../../core/services/administracion/dominio.service';
import { DtoDepartamento, DtoMunicipio, UbicacionService } from '../../../core/services/administracion/ubicacion.service';
import { UsuarioAdminService } from '../../../core/services/administracion/usuario-admin.service';
import { ToastService } from '../../../core/services/toast.service';

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

  unidadesLoading = false;
  departamentosLoading = false;
  municipiosLoading = false;
  buscandoResponsable = false;
  guardando = false;
  private ultimaCedulaConsultada = '';

  imagenPreviewUrl = '';

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
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.cargarUnidades();
    this.cargarDepartamentos();
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
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.imagenPreviewUrl = String(reader.result ?? '');
    };
    reader.readAsDataURL(file);
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

    this.guardando = true;
    setTimeout(() => {
      this.guardando = false;
      this.toast.info('Eventos', 'Formulario validado. Pendiente conectar endpoint de guardado.');
    }, 300);
  }

  limpiarFormulario(): void {
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
    this.ultimaCedulaConsultada = '';
  }
}
