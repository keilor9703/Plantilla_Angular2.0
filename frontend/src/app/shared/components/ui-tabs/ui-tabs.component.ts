import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  contentChildren,
  effect,
  input,
  model,
  output,
  viewChild,
  viewChildren,
} from '@angular/core';
import { UiTabComponent } from './ui-tab.component';
import { UiTabSelectionChange, UiTabsSize, UiTabsVariant } from './ui-tabs.types';

let nextTabsId = 0;

@Component({
  selector: 'app-ui-tabs',
  standalone: true,
  templateUrl: './ui-tabs.component.html',
  styleUrl: './ui-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiTabsComponent {
  readonly variant = input<UiTabsVariant>('contained');
  readonly size = input<UiTabsSize>('md');
  readonly ariaLabel = input('Secciones');
  readonly tabsId = input('');
  readonly stretch = input(false, { transform: booleanAttribute });
  readonly showPanel = input(true, { transform: booleanAttribute });
  /** Distribuye cuatro secciones en una grilla 2×2 en pantallas móviles. */
  readonly mobileGrid = input(false, { transform: booleanAttribute });
  /** Hace que el panel activo ocupe el alto entregado por su contenedor. */
  readonly fillHeight = input(false, { transform: booleanAttribute });
  /**
   * Retira la tarjeta exterior del contenido sin alterar la navegación.
   * Resulta útil cuando cada página ya administra sus propios paneles.
   */
  readonly flushPanel = input(false, { transform: booleanAttribute });

  /**
   * Controla la línea institucional del panel.
   * true: muestra la línea azul en el lateral izquierdo.
   * false: conserva el panel sin línea de acento.
   */
  readonly showPanelAccent = input(true, { transform: booleanAttribute });

  readonly activeTabId = model('');
  readonly selectionChange = output<UiTabSelectionChange>();

  readonly tabs = contentChildren(UiTabComponent);
  private readonly tabList = viewChild<ElementRef<HTMLDivElement>>('tabList');
  private readonly tabButtons = viewChildren<ElementRef<HTMLButtonElement>>('tabButton');
  private readonly generatedTabsId = `ui-tabs-${++nextTabsId}`;
  private dragPointerId: number | null = null;
  private dragStartX = 0;
  private dragStartScrollLeft = 0;
  private draggedTabs = false;
  private suppressNextTabClick = false;

  readonly resolvedTabsId = computed(() => this.tabsId().trim() || this.generatedTabsId);
  readonly activeTab = computed(() => {
    const enabledTabs = this.tabs().filter((tab) => !tab.disabled());

    return enabledTabs.find((tab) => tab.id() === this.activeTabId()) ?? enabledTabs.at(0) ?? null;
  });
  readonly classes = computed(() => {
    const classNames = ['ui-tabs', `ui-tabs--${this.variant()}`, `ui-tabs--${this.size()}`];

    if (this.stretch()) {
      classNames.push('is-stretched');
    }

    if (!this.showPanel()) {
      classNames.push('without-panel');
    }

    if (this.mobileGrid()) {
      classNames.push('is-mobile-grid');
    }

    if (this.fillHeight()) {
      classNames.push('is-fill-height');
    }

    if (this.flushPanel()) {
      classNames.push('is-panel-flush');
    }

    if (this.showPanelAccent()) {
      classNames.push('with-panel-accent');
    }

    return classNames.join(' ');
  });

  /**
   * Mantiene sincronizado cada panel proyectado con el tab activo.
   * Los formularios permanecen instanciados y conservan su estado.
   */
  private readonly syncTabPresentation = effect(() => {
    const activeTab = this.activeTab();
    const showPanel = this.showPanel();

    for (const tab of this.tabs()) {
      tab.setPresentation(
        showPanel && tab === activeTab,
        this.tabPanelId(tab),
        this.tabButtonId(tab),
      );
    }
  });

  /** Mantiene visible la pestaña activa cuando la barra tiene desplazamiento. */
  private readonly revealActiveTab = effect(() => {
    const activeTab = this.activeTab();
    this.tabList();
    this.tabButtons();

    if (activeTab) {
      queueMicrotask(() => this.scrollTabIntoView(activeTab));
    }
  });

  selectTab(tab: UiTabComponent, event?: MouseEvent): void {
    if (this.suppressNextTabClick) {
      event?.preventDefault();
      event?.stopPropagation();
      this.suppressNextTabClick = false;
      return;
    }

    if (tab.disabled() || this.isActive(tab)) {
      return;
    }

    this.activateTab(tab);
    this.scrollTabIntoView(tab);
  }

  handleTabListPointerDown(event: PointerEvent): void {
    const list = this.tabList()?.nativeElement;

    if (
      !list ||
      event.pointerType !== 'mouse' ||
      event.button !== 0 ||
      list.scrollWidth <= list.clientWidth
    ) {
      return;
    }

    this.dragPointerId = event.pointerId;
    this.dragStartX = event.clientX;
    this.dragStartScrollLeft = list.scrollLeft;
    this.draggedTabs = false;
    list.setPointerCapture(event.pointerId);
    list.classList.add('is-dragging');
  }

  handleTabListPointerMove(event: PointerEvent): void {
    const list = this.tabList()?.nativeElement;

    if (!list || this.dragPointerId !== event.pointerId) {
      return;
    }

    const displacement = event.clientX - this.dragStartX;

    if (Math.abs(displacement) >= 4) {
      this.draggedTabs = true;
    }

    if (!this.draggedTabs) {
      return;
    }

    event.preventDefault();
    list.scrollLeft = this.dragStartScrollLeft - displacement;
  }

  handleTabListPointerUp(event: PointerEvent): void {
    this.finishTabListDrag(event, false);
  }

  handleTabListPointerCancel(event: PointerEvent): void {
    this.finishTabListDrag(event, true);
  }

  handleTabKeydown(event: KeyboardEvent, currentTab: UiTabComponent): void {
    const enabledTabs = this.tabs().filter((tab) => !tab.disabled());
    const currentIndex = enabledTabs.indexOf(currentTab);
    let targetTab: UiTabComponent | undefined;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        targetTab = enabledTabs[(currentIndex + 1) % enabledTabs.length];
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        targetTab = enabledTabs[(currentIndex - 1 + enabledTabs.length) % enabledTabs.length];
        break;
      case 'Home':
        targetTab = enabledTabs.at(0);
        break;
      case 'End':
        targetTab = enabledTabs.at(-1);
        break;
      default:
        return;
    }

    event.preventDefault();

    if (targetTab) {
      this.activateTab(targetTab);
      this.focusTab(targetTab);
    }
  }

  isActive(tab: UiTabComponent): boolean {
    return this.activeTab() === tab;
  }

  tabButtonId(tab: UiTabComponent): string {
    return `${this.resolvedTabsId()}-tab-${tab.id()}`;
  }

  tabPanelId(tab: UiTabComponent): string {
    return `${this.resolvedTabsId()}-panel-${tab.id()}`;
  }

  private activateTab(tab: UiTabComponent): void {
    const index = this.tabs().indexOf(tab);

    this.activeTabId.set(tab.id());
    this.selectionChange.emit({
      id: tab.id(),
      label: tab.label(),
      index,
    });
  }

  private focusTab(tab: UiTabComponent): void {
    const tabIndex = this.tabs().indexOf(tab);
    const button = this.tabButtons().at(tabIndex);

    button?.nativeElement.focus();
    this.scrollTabIntoView(tab);
  }

  private finishTabListDrag(event: PointerEvent, cancelled: boolean): void {
    const list = this.tabList()?.nativeElement;

    if (!list || this.dragPointerId !== event.pointerId) {
      return;
    }

    if (list.hasPointerCapture(event.pointerId)) {
      list.releasePointerCapture(event.pointerId);
    }

    list.classList.remove('is-dragging');

    if (this.draggedTabs && !cancelled) {
      this.suppressNextTabClick = true;
      setTimeout(() => {
        this.suppressNextTabClick = false;
      });
    }

    this.dragPointerId = null;
    this.draggedTabs = false;
  }

  private scrollTabIntoView(tab: UiTabComponent): void {
    const list = this.tabList()?.nativeElement;
    const tabIndex = this.tabs().indexOf(tab);
    const button = this.tabButtons().at(tabIndex)?.nativeElement;

    if (!list || !button || list.scrollWidth <= list.clientWidth) {
      return;
    }

    const listBounds = list.getBoundingClientRect();
    const buttonBounds = button.getBoundingClientRect();
    const edgeGap = 7;

    if (buttonBounds.left < listBounds.left + edgeGap) {
      list.scrollLeft -= listBounds.left + edgeGap - buttonBounds.left;
    } else if (buttonBounds.right > listBounds.right - edgeGap) {
      list.scrollLeft += buttonBounds.right - (listBounds.right - edgeGap);
    }
  }
}
