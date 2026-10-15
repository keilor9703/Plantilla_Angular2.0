# Guía de Implementación de Estilos y Componentes de Plantilla Institucional (SICOE)

Esta guía documenta los estándares visuales, patrones de arquitectura de interfaz y componentes reutilizables implementados en los módulos de administración (**Menús**, **Roles** y **Usuarios**), basados en la directriz visual de **`update-plantilla`**.

---

## Índice

1. [Regla Crítica: Gotcha de Pestañas y styles.scss (¡No repetir el error!)](#1-regla-crítica-gotcha-de-pestañas-y-stylesscss)
2. [Estructura Base del Módulo y Miga de Pan con Logo](#2-estructura-base-del-módulo-y-miga-de-pan-con-logo)
3. [Estado de Carga / Card de Espera](#3-estado-de-carga--card-de-espera)
4. [Modales Centradas en Pantalla](#4-modales-centradas-en-pantalla)
5. [Sistema de Pestañas (Tabs)](#5-sistema-de-pestañas-tabs)
6. [Formularios, Campos Flotantes e Iconografía](#6-formularios-campos-flotantes-e-iconografía)
7. [Toggles Institucionales (Switch)](#7-toggles-institucionales-switch)
8. [Tablas Institucionales, Paginador y Dropdown de Roles](#8-tablas-institucionales-paginador-y-dropdown-de-roles)
9. [Workspace de Dos Columnas (Catálogo + Detalle)](#9-workspace-de-dos-columnas-catálogo--detalle)
10. [Soporte para Modo Oscuro](#10-soporte-para-modo-oscuro)
11. [Checklist de Validación para Nuevas Pantallas](#11-checklist-de-validación-para-nuevas-pantallas)

---

## 1. Regla Crítica: Gotcha de Pestañas y `styles.scss`

> [!CAUTION]
> **ERROR CONOCIDO Y CÓMO EVITARLO:**
> En el archivo global `src/styles.scss` (aprox. línea 1529) existe una regla CSS heredada:
> ```scss
> .ui-tabs__panel { display: none; }
> ```
> Esta regla fue escrita para un sistema antiguo con *inputs radio* (`#tab-1:checked ~ .ui-tabs__panels .ui-tabs__panel[data-for="tab-1"] { display: block; }`).
>
> **Consecuencia:** Si creas pestañas en Angular usando la clase `.ui-tabs__panel` combinada con `*ngIf="activeTab === '...'"` y **NO** especificas `display: block !important;` en tu SCSS local, la regla global ocultará el panel por completo y **los campos del formulario no se verán en pantalla**.

### Regla obligatoria en el SCSS de cualquier componente que use pestañas:

```scss
.ui-tabs {
  display: block !important;
  width: 100%;
  max-width: 100%;
  min-width: 0;

  &__panel {
    display: block !important; /* OBLIGATORIO: anula display:none de styles.scss */
    box-sizing: border-box;
    width: 100%;
    min-width: 0;
    max-width: 100%;
    margin-top: 7px;
    padding: 18px;
    border: 1px solid rgba(21, 27, 59, 0.16);
    border-radius: 5px;
    background: var(--ui-surface, #ffffff);
    color: var(--ui-text, #0f172a);

    &.is-panel-flush {
      display: block !important; /* OBLIGATORIO */
      padding: 0;
      border: 0;
      background: transparent;
    }

    &[hidden] {
      display: none !important;
    }
  }
}

/* En modo oscuro también forzar display */
:host-context(html.dark-mode) {
  .ui-tabs__panel {
    display: block !important;
    background: #151e2e;
    border-color: rgba(255, 255, 255, 0.12);
  }
}
```

---

## 2. Estructura Base del Módulo y Miga de Pan con Logo

La cabecera del módulo unifica la navegación con la identidad visual institucional de la Policía Nacional:

### HTML (`modulo.html`):

```html
<section class="module-admin-page" *ngIf="visible">
  <!-- Miga de pan / Header con isotipo institucional -->
  <header class="ui-breadcrumb-bar">
    <div class="breadcrumb-brand">
      <img
        src="assets/images/logo-policia.png"
        alt="Escudo Institucional"
        class="breadcrumb-logo"
      />
      <div class="breadcrumb-copy">
        <nav class="breadcrumb-nav" aria-label="Navegación">
          <span class="crumb-parent">Administración</span>
          <i class="fa-solid fa-chevron-right crumb-sep"></i>
          <span class="crumb-current">Nombre del Módulo</span>
        </nav>
        <h1 class="module-heading-title">Título Principal del Módulo</h1>
      </div>
    </div>

    <div class="breadcrumb-actions">
      <!-- Botones de acción global (p.ej. Nuevo registro, Volver) -->
      <button class="ui-btn primary btn-sm" type="button" (click)="nuevoRegistro()">
        <i class="fa-solid fa-plus"></i>
        <span>Nuevo registro</span>
      </button>
    </div>
  </header>

  <!-- Panel Principal Contenedor -->
  <div class="ui-panel">
    <div class="ui-panel__body">
      <!-- Contenido del módulo -->
    </div>
  </div>
</section>
```

### SCSS (`modulo.scss`):

```scss
.ui-breadcrumb-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
  padding: 14px 20px;
  background: var(--ui-surface, #ffffff);
  border: 1px solid var(--ui-border, #e2e8f0);
  border-radius: 10px;
  margin-bottom: 16px;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.04);

  .breadcrumb-brand {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  .breadcrumb-logo {
    width: 36px;
    height: 36px;
    object-fit: contain;
    filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.12));
  }

  .breadcrumb-copy {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .breadcrumb-nav {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: calc(11px * var(--font-size-scale, 1));
    font-weight: 700;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.04em;

    .crumb-sep {
      font-size: 9px;
      color: #94a3b8;
    }

    .crumb-current {
      color: var(--policia-oscuro, #151b3b);
    }
  }

  .module-heading-title {
    margin: 0;
    font-size: calc(17px * var(--font-size-scale, 1));
    font-weight: 900;
    color: var(--policia-oscuro, #151b3b);
    line-height: 1.2;
  }
}
```

---

## 3. Estado de Carga / Card de Espera

Para evitar saltos visuales o pantallas en blanco mientras se cargan catálogos o llamadas HTTP:

### HTML:

```html
<article class="module-loading-card" *ngIf="loading">
  <div class="loading-spinner-ring">
    <i class="fa-solid fa-circle-notch fa-spin"></i>
  </div>
  <div class="loading-copy">
    <h3>Cargando información del módulo...</h3>
    <p>Por favor espere mientras se obtienen los datos del servidor institucional.</p>
  </div>
</article>
```

### SCSS:

```scss
.module-loading-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  padding: 48px 24px;
  text-align: center;
  background: var(--ui-surface, #ffffff);
  border: 1px dashed rgba(21, 27, 59, 0.2);
  border-radius: 12px;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.03);

  .loading-spinner-ring {
    font-size: 32px;
    color: var(--policia-cielo, #08a6cb);
  }

  .loading-copy {
    h3 {
      margin: 0 0 6px;
      font-size: calc(15px * var(--font-size-scale, 1));
      font-weight: 800;
      color: var(--policia-oscuro, #151b3b);
    }

    p {
      margin: 0;
      font-size: calc(12px * var(--font-size-scale, 1));
      color: #64748b;
    }
  }
}
```

---

## 4. Modales Centradas en Pantalla

Las modales deben mostrarse siempre en el **centro geométrico** de la pantalla con desenfoque de fondo (`backdrop-filter: blur(6px)`), cabecera degradada institucional, cuerpo desplazable y pie con botones alineados a la derecha.

### HTML:

```html
<div class="ui-modal" *ngIf="showModal" (click)="cerrarModal()">
  <div class="ui-modal__dialog ui-modal__dialog--lg" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
    <header class="ui-modal__head">
      <div class="ui-modal__heading">
        <span class="ui-modal__icon">
          <i class="fa-solid fa-shield-halved"></i>
        </span>
        <div class="ui-modal__heading-copy">
          <h3 class="ui-modal__title">{{ esEdicion ? 'Editar Registro' : 'Nuevo Registro' }}</h3>
          <p class="ui-modal__subtitle">Complete los campos requeridos para continuar.</p>
        </div>
      </div>
      <button class="ui-modal__close" type="button" (click)="cerrarModal()" title="Cerrar ventana">
        <i class="fa-solid fa-xmark"></i>
      </button>
    </header>

    <div class="ui-modal__body">
      <!-- Grid de campos de formulario -->
      <div class="ui-grid">
        <div class="ui-col-6">
          <label class="form-field">
            <span class="form-field__label">Nombre del campo</span>
            <span class="form-field__control">
              <input class="form-field__native" type="text" [(ngModel)]="model.nombre" />
              <i class="form-field__icon fa-solid fa-pencil"></i>
            </span>
          </label>
        </div>
      </div>
    </div>

    <footer class="ui-modal__foot">
      <button class="ui-btn secondary" type="button" (click)="cerrarModal()" [disabled]="guardando">
        <i class="fa-solid fa-ban"></i>
        <span>Cancelar</span>
      </button>
      <button class="ui-btn primary" type="button" (click)="guardar()" [disabled]="guardando">
        <i class="fa-solid fa-floppy-disk" *ngIf="!guardando"></i>
        <i class="fa-solid fa-circle-notch fa-spin" *ngIf="guardando"></i>
        <span>{{ guardando ? 'Guardando...' : 'Guardar cambios' }}</span>
      </button>
    </footer>
  </div>
</div>
```

### SCSS:

```scss
.ui-modal {
  position: fixed;
  inset: 0;
  z-index: var(--ui-z-modal, 11000);
  display: grid;
  place-items: center; /* Centrado 100% en mitad de la pantalla */
  padding: 24px;
  background: rgba(2, 6, 23, 0.58);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);

  &__dialog {
    position: relative;
    display: grid;
    grid-template-rows: auto minmax(0, 1fr) auto;
    width: min(100%, 680px);
    max-height: min(88vh, 860px);
    overflow: hidden;
    border-radius: 16px;
    background: var(--ui-surface, #ffffff);
    box-shadow:
      0 0 0 1px rgba(15, 23, 42, 0.13),
      0 34px 90px rgba(2, 6, 23, 0.34);

    &--lg {
      width: min(100%, 880px);
    }
  }

  &__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    padding: 18px 20px;
    background: linear-gradient(135deg, var(--policia-oscuro, #151b3b), var(--policia-medio, #002a66));
    color: #ffffff;
  }

  &__heading {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  &__icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.12);
    font-size: 16px;
  }

  &__title {
    margin: 0;
    font-size: calc(16px * var(--font-size-scale, 1));
    font-weight: 800;
  }

  &__subtitle {
    margin: 2px 0 0;
    font-size: calc(11.5px * var(--font-size-scale, 1));
    color: rgba(255, 255, 255, 0.78);
  }

  &__close {
    background: transparent;
    border: 0;
    color: #ffffff;
    font-size: 18px;
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;
    transition: background 0.15s ease;

    &:hover {
      background: rgba(255, 255, 255, 0.15);
    }
  }

  &__body {
    padding: 20px;
    overflow-y: auto;
  }

  &__foot {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 10px;
    padding: 14px 20px;
    border-top: 1px solid var(--ui-border, #e2e8f0);
    background: #f8fafc;
  }
}
```

---

## 5. Sistema de Pestañas (Tabs)

Pestañas institucionales con acento inferior de color cielo (`--policia-cielo`), contador en badge y panel conectado:

### HTML:

```html
<div class="ui-tabs ui-tabs--navigation is-stretched" [class.has-form-footer]="activeTab === 'datos'">
  <!-- Lista de pestañas -->
  <div class="ui-tabs__list" role="tablist">
    <button
      type="button"
      class="ui-tabs__tab"
      [class.is-active]="activeTab === 'datos'"
      (click)="activeTab = 'datos'"
    >
      <i class="fa-solid fa-address-card"></i>
      <span>Datos Principales</span>
    </button>

    <button
      type="button"
      class="ui-tabs__tab"
      [class.is-active]="activeTab === 'roles'"
      (click)="activeTab = 'roles'"
    >
      <i class="fa-solid fa-key"></i>
      <span>Permisos Asignados</span>
      <small class="ui-tabs__badge" *ngIf="rolesCount > 0">{{ rolesCount }}</small>
    </button>
  </div>

  <!-- Panel 1 (con footer integrado) -->
  <div class="ui-tabs__panel" *ngIf="activeTab === 'datos'">
    <div class="tab-content">
      <!-- Grid de campos -->
    </div>
  </div>

  <!-- Footer pegado al panel -->
  <footer class="form-footer" *ngIf="activeTab === 'datos'">
    <button class="ui-btn primary" type="button" (click)="guardar()">
      <i class="fa-solid fa-floppy-disk"></i>
      <span>Guardar</span>
    </button>
  </footer>

  <!-- Panel 2 (al ras de tabla) -->
  <div class="ui-tabs__panel is-panel-flush" *ngIf="activeTab === 'roles'">
    <!-- Tabla de permisos -->
  </div>
</div>
```

---

## 6. Formularios, Campos Flotantes e Iconografía

Campos de alta legibilidad con etiqueta superpuesta que no se solapa (`padding: 0 9px` con gradiente simulador de recorte de borde), caja de control con foco cian y soporte de icono a la derecha:

### HTML:

```html
<label class="form-field">
  <span class="form-field__label">Identificación</span>
  <span class="form-field__control">
    <input class="form-field__native" type="text" [(ngModel)]="model.identificacion" placeholder=" " />
    <i class="form-field__icon fa-solid fa-id-card"></i>
  </span>
</label>

<!-- Campo tipo Select -->
<label class="form-field">
  <span class="form-field__label">Rol asignado</span>
  <span class="form-field__control">
    <select class="form-field__native" [(ngModel)]="model.rolId">
      <option [ngValue]="null">Seleccione una opción...</option>
      <option *ngFor="let r of roles" [ngValue]="r.id">{{ r.nombre }}</option>
    </select>
  </span>
</label>

<!-- Buscador de campo integrado -->
<div class="search-field">
  <span class="search-field__label">Buscar usuario</span>
  <div class="search-field__control search-field__control--detached">
    <div class="search-field__shell">
      <input
        class="search-field__input"
        type="text"
        [(ngModel)]="searchQuery"
        placeholder="Identificación o nombre"
        (keyup.enter)="consultar()"
      />
      <button
        *ngIf="searchQuery"
        type="button"
        class="search-field__clear"
        (click)="searchQuery = ''"
        title="Limpiar"
      >×</button>
    </div>
    <button class="ui-btn primary search-field__button" type="button" (click)="consultar()">
      <i class="fa-solid fa-magnifying-glass"></i>
      <span>Buscar</span>
    </button>
  </div>
</div>
```

### SCSS:

```scss
.form-field {
  position: relative;
  display: block;
  width: 100%;
  box-sizing: border-box;

  &__label {
    position: absolute;
    top: -8px;
    left: 11px;
    z-index: 2;
    padding: 0 9px;
    color: var(--form-label-color, #334155);
    font-size: calc(11px * var(--font-size-scale, 1));
    font-weight: 700;
    line-height: 1.2;
    background: linear-gradient(
      90deg,
      transparent 0,
      var(--ui-surface, #ffffff) 6px,
      var(--ui-surface, #ffffff) calc(100% - 6px),
      transparent 100%
    );
    pointer-events: none;
  }

  &__control {
    position: relative;
    display: flex;
    align-items: center;
    width: 100%;
    height: 42px;
    min-height: 42px;
    box-sizing: border-box;
    border: 1px solid var(--form-border-color, #cbd5e1);
    border-radius: 7px;
    background: var(--form-bg, #fff);
    box-shadow: inset 0 1px 2px rgba(15, 23, 42, 0.05);
    transition: border-color 0.15s ease, box-shadow 0.15s ease;

    &:focus-within {
      border-color: var(--policia-cielo, #08a6cb);
      box-shadow:
        inset 0 1px 2px rgba(15, 23, 42, 0.05),
        0 0 0 3px rgba(8, 166, 203, 0.18);
    }
  }

  &__native {
    width: 100%;
    height: 100%;
    box-sizing: border-box;
    padding: 9px 38px 9px 12px;
    border: 0;
    outline: 0;
    background: transparent;
    color: var(--ui-text, #0f172a);
    font-size: calc(13px * var(--font-size-scale, 1));
    font-weight: 600;
    font-family: inherit;

    &[readonly] {
      cursor: default;
    }
  }

  &__icon {
    position: absolute;
    right: 12px;
    color: #53565a;
    font-size: calc(14px * var(--font-size-scale, 1));
    pointer-events: none;
  }
}
```

---

## 7. Toggles Institucionales (Switch)

Switch de activación/inactivación institucional con pista y thumb animado con icono:

### HTML:

```html
<button
  class="ui-toggle ui-toggle--sm ui-toggle--institutional"
  type="button"
  [class.is-checked]="item.activo"
  (click)="toggleEstado(item)"
  title="Cambiar estado"
  aria-label="Cambiar estado"
>
  <span class="ui-toggle__track">
    <span class="ui-toggle__thumb">
      <i [class]="item.activo ? 'fa-solid fa-check' : 'fa-solid fa-xmark'"></i>
    </span>
  </span>
</button>
```

### SCSS:

```scss
.ui-toggle {
  background: transparent;
  border: 0;
  cursor: pointer;
  padding: 0;

  &__track {
    position: relative;
    display: inline-block;
    width: 44px;
    height: 24px;
    border-radius: 999px;
    background: #cbd5e1;
    transition: background 0.2s ease;
  }

  &__thumb {
    position: absolute;
    top: 2px;
    left: 2px;
    display: grid;
    place-items: center;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: #ffffff;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
    transition: transform 0.2s ease;

    i {
      font-size: 10px;
      color: #64748b;
    }
  }

  &.is-checked {
    .ui-toggle__track {
      background: var(--policia-verde, #0a8754);
    }

    .ui-toggle__thumb {
      transform: translateX(20px);

      i {
        color: var(--policia-verde, #0a8754);
      }
    }
  }
}
```

---

## 8. Tablas Institucionales, Paginador y Dropdown de Roles

Para tablas de listados con paginación y manejo de múltiples roles:

### Componente reutilizable para roles:
En el frontend existe el componente standalone:
```typescript
import { UsuarioRolesDropdownComponent } from '../../../components/usuario-roles-dropdown/usuario-roles-dropdown';
```

### Uso en HTML:
```html
<td class="col-roles align-center">
  <app-usuario-roles-dropdown
    [roles]="item.rol"
    [vencimientos]="item.fechaFinRol || ''"
    [contextLabel]="item.identificacion || item.username"
  ></app-usuario-roles-dropdown>
</td>
```
- Si tiene **1 rol**: Muestra una insignia (`badge`) compacta.
- Si tiene **múltiples roles**: Muestra el primer rol y un badge interactivo `+N roles` que despliega un dropdown flotante con vigencias e iconos de estado.

### Paginador institucional:
```html
<footer class="users-table-pagination" *ngIf="totalItems > 0 && !loading">
  <div class="pagination-counter">
    Página <strong>{{ currentPage }}</strong> de <strong>{{ totalPaginas }}</strong>
  </div>

  <nav class="pagination-nav-group" aria-label="Navegación de páginas">
    <button class="pagination-btn pagination-arrow" (click)="prevPagina()" [disabled]="currentPage === 1">
      <i class="fa-solid fa-chevron-left"></i>
    </button>

    <ng-container *ngFor="let p of pageNumbers">
      <button
        *ngIf="p !== '...'"
        class="pagination-btn pagination-number"
        [class.is-active]="p === currentPage"
        (click)="setPage(p)"
      >
        {{ p }}
      </button>
      <span *ngIf="p === '...'" class="pagination-dots">...</span>
    </ng-container>

    <button class="pagination-btn pagination-arrow" (click)="nextPagina()" [disabled]="currentPage === totalPaginas">
      <i class="fa-solid fa-chevron-right"></i>
    </button>
  </nav>
</footer>
```

---

## 9. Workspace de Dos Columnas (Catálogo + Detalle)

Patrón aplicado en `roles-admin` y `usuarios`:

```
┌─────────────────────────────────┬──────────────────────────────────────────────┐
│  COLUMNA 1: Catálogo / Perfil   │  COLUMNA 2: Pestañas / Formularios / Permisos │
│  - Búsqueda en vivo             │  - Barra de pestañas (.ui-tabs__list)        │
│  - Lista con selector activo    │  - Panel activo (.ui-tabs__panel)            │
│  - Badges de conteo             │  - Pie de formulario (.form-footer)          │
└─────────────────────────────────┴──────────────────────────────────────────────┘
```

```scss
.roles-admin-workspace,
.admin-grid {
  display: grid;
  grid-template-columns: 360px minmax(0, 1fr);
  gap: 20px;
  align-items: start;

  @media (max-width: 1024px) {
    grid-template-columns: 1fr;
  }
}
```

---

## 10. Soporte para Modo Oscuro

El sistema utiliza la clase `html.dark-mode`. Todas las vistas deben incluir al final de su SCSS:

```scss
:host-context(html.dark-mode) {
  .ui-breadcrumb-bar,
  .ui-panel,
  .access-section {
    background: #151e2e;
    border-color: rgba(255, 255, 255, 0.1);
  }

  .ui-tabs__panel {
    display: block !important;
    background: #151e2e;
    border-color: rgba(255, 255, 255, 0.12);
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);

    &.is-panel-flush {
      display: block !important;
      background: transparent;
      border: 0;
    }
  }

  .form-field__label,
  .search-field__label {
    background: linear-gradient(
      90deg,
      transparent 0,
      #151e2e 6px,
      #151e2e calc(100% - 6px),
      transparent 100%
    );
    color: #cbd5e1;
  }

  .form-field__control,
  .search-field__shell {
    background: #1e2840;
    border-color: rgba(255, 255, 255, 0.14);
  }

  .form-footer {
    background: #151e2e;
    border-color: rgba(255, 255, 255, 0.1);
  }
}
```

---

## 11. Checklist de Validación para Nuevas Pantallas

Antes de dar por completado un módulo de administración, verificar:

- [ ] **Visibilidad de Tabs:** ¿Se agregó `display: block !important;` a `.ui-tabs__panel` tanto en modo normal como en `:host-context(html.dark-mode)`?
- [ ] **Centrado de Modales:** ¿La modal usa `position: fixed; inset: 0; display: grid; place-items: center;` con backdrop blur?
- [ ] **Miga de pan:** ¿Tiene el logo institucional de la Policía (`assets/images/logo-policia.png`), ruta de navegación y título del módulo?
- [ ] **Estado de carga:** ¿Muestra el spinner institucional dentro de un card estilizado mientras cargan los datos?
- [ ] **Sizing de Campos:** ¿Todos los inputs tienen `box-sizing: border-box; width: 100%;`?
- [ ] **Múltiples Roles:** Si lista usuarios, ¿usa `<app-usuario-roles-dropdown>` para evitar que la tabla se rompa con usuarios que tienen varios roles?
- [ ] **Paginación:** ¿Se incluye el paginador institucional con flechas e indicador de página activa?
- [ ] **Compilación:** ¿Ejecutó `ng build` sin errores ni advertencias de selector?
