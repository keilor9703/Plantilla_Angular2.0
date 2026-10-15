import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  effect,
  input,
  model,
  output,
  viewChild,
  viewChildren,
} from '@angular/core';
import {
  UiSegmentedTabItem,
  UiSegmentedTabSelectionChange,
  UiSegmentedTabsSize,
} from './ui-segmented-tabs.types';

let nextSegmentedTabsId = 0;

@Component({
  selector: 'app-ui-segmented-tabs',
  standalone: true,
  templateUrl: './ui-segmented-tabs.component.html',
  styleUrl: './ui-segmented-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiSegmentedTabsComponent {
  readonly items = input.required<readonly UiSegmentedTabItem[]>();
  readonly activeId = model('');
  readonly ariaLabel = input('Secciones por estado');
  readonly tabsId = input('');
  readonly controlsId = input('');
  readonly size = input<UiSegmentedTabsSize>('md');
  readonly stretch = input(true, { transform: booleanAttribute });
  readonly showDescriptions = input(true, { transform: booleanAttribute });
  readonly hideDescriptionsOnMobile = input(true, { transform: booleanAttribute });
  readonly showBadges = input(true, { transform: booleanAttribute });

  readonly selectionChange = output<UiSegmentedTabSelectionChange>();

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');
  private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('tabButton');
  private readonly generatedId = `ui-segmented-tabs-${++nextSegmentedTabsId}`;
  private dragPointerId: number | null = null;
  private dragStartX = 0;
  private dragStartScrollLeft = 0;
  private draggedTabs = false;
  private suppressNextClick = false;

  readonly resolvedTabsId = computed(() => this.tabsId().trim() || this.generatedId);
  readonly enabledItems = computed(() => this.items().filter((item) => !item.disabled));
  readonly resolvedActiveId = computed(
    () =>
      this.enabledItems().find((item) => item.id === this.activeId())?.id ??
      this.enabledItems().at(0)?.id ??
      '',
  );
  readonly classes = computed(() =>
    [
      'ui-segmented-tabs',
      `ui-segmented-tabs--${this.size()}`,
      this.stretch() ? 'is-stretched' : '',
      this.hideDescriptionsOnMobile() ? 'hide-description-mobile' : '',
    ]
      .filter(Boolean)
      .join(' '),
  );

  /** Desplaza la barra cuando cambia el tab activo desde el modelo o el teclado. */
  private readonly revealActiveTab = effect(() => {
    const activeId = this.resolvedActiveId();
    this.tabList();
    this.buttons();

    if (activeId) {
      queueMicrotask(() => this.scrollItemIntoView(activeId));
    }
  });

  isActive(item: UiSegmentedTabItem): boolean {
    return item.id === this.resolvedActiveId();
  }

  buttonId(item: UiSegmentedTabItem): string {
    return `${this.resolvedTabsId()}-tab-${item.id}`;
  }

  itemClasses(item: UiSegmentedTabItem): string {
    return [
      'ui-segmented-tabs__item',
      `is-${item.tone ?? 'info'}`,
      this.isActive(item) ? 'is-active' : '',
    ]
      .filter(Boolean)
      .join(' ');
  }

  select(item: UiSegmentedTabItem, event?: MouseEvent): void {
    if (this.suppressNextClick) {
      event?.preventDefault();
      event?.stopPropagation();
      this.suppressNextClick = false;
      return;
    }

    if (item.disabled || this.isActive(item)) {
      return;
    }

    this.activate(item);
    this.scrollItemIntoView(item.id);
  }

  handlePointerDown(event: PointerEvent): void {
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

  handlePointerMove(event: PointerEvent): void {
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

  handlePointerUp(event: PointerEvent): void {
    this.finishDrag(event, false);
  }

  handlePointerCancel(event: PointerEvent): void {
    this.finishDrag(event, true);
  }

  handleKeydown(event: KeyboardEvent, item: UiSegmentedTabItem): void {
    const enabledItems = this.enabledItems();
    const currentIndex = enabledItems.indexOf(item);
    let target: UiSegmentedTabItem | undefined;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        target = enabledItems[(currentIndex + 1) % enabledItems.length];
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        target = enabledItems[(currentIndex - 1 + enabledItems.length) % enabledItems.length];
        break;
      case 'Home':
        target = enabledItems.at(0);
        break;
      case 'End':
        target = enabledItems.at(-1);
        break;
      default:
        return;
    }

    event.preventDefault();

    if (target) {
      this.activate(target);
      this.focus(target);
    }
  }

  private activate(item: UiSegmentedTabItem): void {
    const index = this.items().indexOf(item);

    this.activeId.set(item.id);
    this.selectionChange.emit({ id: item.id, index, item });
  }

  private focus(item: UiSegmentedTabItem): void {
    const index = this.items().indexOf(item);
    this.buttons().at(index)?.nativeElement.focus();
    this.scrollItemIntoView(item.id);
  }

  private finishDrag(event: PointerEvent, cancelled: boolean): void {
    const list = this.tabList()?.nativeElement;

    if (!list || this.dragPointerId !== event.pointerId) {
      return;
    }

    if (list.hasPointerCapture(event.pointerId)) {
      list.releasePointerCapture(event.pointerId);
    }

    list.classList.remove('is-dragging');

    if (this.draggedTabs && !cancelled) {
      this.suppressNextClick = true;
      setTimeout(() => {
        this.suppressNextClick = false;
      });
    }

    this.dragPointerId = null;
    this.draggedTabs = false;
  }

  private scrollItemIntoView(itemId: string): void {
    const list = this.tabList()?.nativeElement;
    const itemIndex = this.items().findIndex((item) => item.id === itemId);
    const button = this.buttons().at(itemIndex)?.nativeElement;

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
