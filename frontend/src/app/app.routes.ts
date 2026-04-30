import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login';
import { HomeComponent } from './pages/home/home';
import { Formularios } from './pages/administracion/formularios/formularios';
import { UsuariosComponent } from './pages/administracion/usuarios/usuarios';
import { SlidersComponent } from './pages/administracion/sliders/sliders';
import { MenuAdminComponent } from './pages/administracion/menu-admin/menu-admin';
import { ConfiguracionSistemaComponent } from './pages/administracion/configuracion-sistema/configuracion-sistema';
import { RolesAdminComponent } from './pages/administracion/roles-admin/roles-admin';
import { LayoutComponent } from './layout/layout';
import { NoticiasWeb } from './pages/noticias/noticias';
import { LineaMandoAdminComponent } from './pages/administracion/linea-mando/linea-mando';
import { AdministracionInicioComponent } from './pages/administracion/administracion-inicio/administracion-inicio';
import { RadioAdminComponent } from './pages/administracion/radio/radio';
import { DominioAdminComponent } from './pages/administracion/dominio/dominio';
import { VideoInstitucionalComponent } from './pages/administracion/video-institucional/video-institucional';
import { NoticiasAdminComponent } from './pages/administracion/noticias/noticias';
import { ModalAdminComponent } from './pages/administracion/modal/modal-admin';
import { AuditoriaAdminComponent } from './pages/administracion/auditoria/auditoria';
import { RedesSocialesAdminComponent } from './pages/gestion-digital/redes-sociales/redes-sociales-admin';
import { GestionDigitalInicioComponent } from './pages/gestion-digital/gestion-digital-inicio/gestion-digital-inicio';
import { EventosInstitucionalesComponent } from './pages/creativios/eventos-institucionales/eventos-institucionales';
import { RecoleccionAnalisisComponent } from './pages/centro-analisis/recoleccion-analisis/recoleccion-analisis';
import { EmprendimientosComponent } from './pages/Grupo-diseno/emprendimientos/emprendimientos';
import { GestionCorreosElectronicosComponent } from './pages/gestion-documental/gestion-correos-electronicos/gestion-correos-electronicos';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'noticias', component: NoticiasWeb },
      {
        path: 'gestion-digital',
        children: [
          { path: '', component: GestionDigitalInicioComponent },
          { path: 'inicio', component: GestionDigitalInicioComponent },
          { path: 'redes-sociales', component: RedesSocialesAdminComponent },
        ],
      },
      { path: 'eventos-institucionales', component: EventosInstitucionalesComponent },
      {
        path: 'creativios',
        children: [
          { path: '', redirectTo: 'emprendimientos', pathMatch: 'full' },
          { path: 'emprendimientos', component: EmprendimientosComponent },
        ],
      },
      {
        path: 'gestion-documental',
        children: [
          { path: '', redirectTo: 'gestion-correos-electronicos', pathMatch: 'full' },
          { path: 'gestion-correos-electronicos', component: GestionCorreosElectronicosComponent },
        ],
      },
      {
        path: 'centro-analisis',
        children: [
          { path: '', redirectTo: 'recoleccion-analisis', pathMatch: 'full' },
          { path: 'recoleccion-analisis', component: RecoleccionAnalisisComponent },
        ],
      },
      {
        path: 'administracion',
        children: [
          { path: '', component: AdministracionInicioComponent },
          { path: 'inicio', component: AdministracionInicioComponent },
          { path: 'formularios', component: Formularios },
          { path: 'usuarios', component: UsuariosComponent },
          { path: 'roles', component: RolesAdminComponent },
           { path: 'sliders', component: SlidersComponent },
           { path: 'personaje-mes', loadComponent: () => import('./pages/administracion/personaje-mes/personaje-mes').then(m => m.PersonajeMesAdminComponent) },
           { path: 'menu', component: MenuAdminComponent },
          { path: 'configuracion-sistema', component: ConfiguracionSistemaComponent },
          { path: 'admin-multimedia', redirectTo: 'configuracion-sistema', pathMatch: 'full' },
          { path: 'video-unidad', redirectTo: 'configuracion-sistema', pathMatch: 'full' },
          { path: 'configuracion-imagen-sitio', redirectTo: 'configuracion-sistema', pathMatch: 'full' },
          { path: 'linea-mando', component: LineaMandoAdminComponent },
          { path: 'radio', component: RadioAdminComponent },
          { path: 'dominio', component: DominioAdminComponent },
          { path: 'video-institucional', component: VideoInstitucionalComponent },
          { path: 'noticias', component: NoticiasAdminComponent },
          { path: 'modal', component: ModalAdminComponent },
          { path: 'auditoria', component: AuditoriaAdminComponent },
          { path: 'redes-sociales', redirectTo: '/gestion-digital/redes-sociales', pathMatch: 'full' },
          { path: 'gestion-digital', redirectTo: '/gestion-digital', pathMatch: 'full' },
        ],
      },
      { path: 'formularios', redirectTo: 'administracion/formularios', pathMatch: 'full' },
      { path: 'usuarios', redirectTo: 'administracion/usuarios', pathMatch: 'full' },
      { path: 'roles', redirectTo: 'administracion/roles', pathMatch: 'full' },
      { path: 'sliders', redirectTo: 'administracion/sliders', pathMatch: 'full' },
      { path: 'menu', redirectTo: 'administracion/menu', pathMatch: 'full' },
      { path: 'configuracion-sistema', redirectTo: 'administracion/configuracion-sistema', pathMatch: 'full' },
      { path: 'admin-multimedia', redirectTo: 'administracion/configuracion-sistema', pathMatch: 'full' },
      { path: 'video-unidad', redirectTo: 'administracion/configuracion-sistema', pathMatch: 'full' },
      { path: 'configuracion-imagen-sitio', redirectTo: 'administracion/configuracion-sistema', pathMatch: 'full' },
      { path: 'linea-mando', redirectTo: 'administracion/linea-mando', pathMatch: 'full' },
      { path: 'radio', redirectTo: 'administracion/radio', pathMatch: 'full' },
      { path: 'dominio', redirectTo: 'administracion/dominio', pathMatch: 'full' },
      { path: 'video-institucional', redirectTo: 'administracion/video-institucional', pathMatch: 'full' },
      { path: 'redes-sociales', redirectTo: 'gestion-digital/redes-sociales', pathMatch: 'full' },
      { path: 'eventos', redirectTo: 'eventos-institucionales', pathMatch: 'full' },
      { path: 'emprendimientos', redirectTo: 'creativios/emprendimientos', pathMatch: 'full' },
      { path: 'correos-electronicos', redirectTo: 'gestion-documental/gestion-correos-electronicos', pathMatch: 'full' },
      { path: 'analisis', redirectTo: 'centro-analisis/recoleccion-analisis', pathMatch: 'full' },
      { path: 'recoleccion-analisis', redirectTo: 'centro-analisis/recoleccion-analisis', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];

