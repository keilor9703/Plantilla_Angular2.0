import {
  ConnectedOverlayPositionChange,
  ConnectedPosition,
  OverlayModule,
} from '@angular/cdk/overlay';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  OnChanges,
  SimpleChanges,
  booleanAttribute,
  forwardRef,
  inject,
  input,
  numberAttribute,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { UiFormControlSizeDirective } from '../../directives/ui-form-control-size.directive';
import { UiAutocompleteOption } from '../../interfaces/ui-autocomplete-option.interface';

type UiAutocompletePanelPlacement = 'above' | 'below';

let siguienteAutocompleteId = 0;

@Component({
  selector: 'app-ui-autocomplete',
  standalone: true,
  imports: [OverlayModule],
  hostDirectives: [
    {
      directive: UiFormControlSizeDirective,
      inputs: ['controlSize'],
    },
  ],
  templateUrl: './ui-autocomplete.component.html',
  styleUrl: './ui-autocomplete.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiAutocompleteComponent),
      multi: true,
    },
  ],
})
export class UiAutocompleteComponent<T = string | number>
  implements ControlValueAccessor, OnChanges
{
  private readonly documentRef = inject(DOCUMENT);
  private readonly windowRef = this.documentRef.defaultView;
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);

  @Input() label = '';
  @Input() placeholder = 'Escriba para buscar';
  @Input() hint = '';
  @Input() error = '';
  @Input() inputId = `ui-autocomplete-${siguienteAutocompleteId++}`;
  @Input() options: readonly UiAutocompleteOption<T>[] = [];
  @Input({ transform: booleanAttribute }) disabled = false;
  @Input({ transform: booleanAttribute }) required = false;
  @Input({ transform: booleanAttribute }) clearable = true;
  /**
   * Cuando es true, el origen remoto ya decidió qué opciones coinciden y el
   * componente no vuelve a descartarlas mediante una comparación de texto.
   */
  @Input({ transform: booleanAttribute }) serverSearch = false;
  @Input({ transform: numberAttribute }) minSearchLength = 2;
  @Input({ transform: numberAttribute }) maxResults = 8;

  readonly showRequiredMarker = input(true, { transform: booleanAttribute });
  readonly optionSelected = output<UiAutocompleteOption<T>>();
  /** Permite que formularios con catálogos grandes hagan búsqueda reactiva en servidor. */
  readonly queryChange = output<string>();
  readonly panelPlacement = signal<UiAutocompletePanelPlacement>('below');
  readonly panelPositioned = signal(false);
  readonly panelWidth = signal(240);

  value: T | null = null;
  query = '';
  opened = false;
  activeIndex = -1;
  controlDisabled = false;
  touched = false;

  readonly panelPositions: ConnectedPosition[] = [
    {
      originX: 'start',
      originY: 'bottom',
      overlayX: 'start',
      overlayY: 'top',
      offsetY: 5,
    },
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -5,
    },
  ];

  private onChange: (value: T | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  ngOnChanges(changes: SimpleChanges): void {
    // La respuesta remota llega después de onInput. Se actualiza la opción
    // activa para que el resultado nuevo también funcione con teclado.
    if (!changes['options'] || !this.opened) return;
    this.activeIndex = this.filteredOptions.length ? 0 : -1;
  }

  writeValue(value: T | null | undefined): void {
    this.value = value ?? null;
    if (this.value === null) this.query = '';
  }

  registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.controlDisabled = disabled;
    if (disabled) this.close();
  }

  onInput(event: Event): void {
    const query = (event.target as HTMLInputElement).value;
    if (this.value !== null) {
      this.value = null;
      this.onChange(null);
    }

    this.query = query;
    this.queryChange.emit(query);
    const debeAbrir = this.normalizedQuery.length >= this.resolvedMinSearchLength;
    const estabaAbierto = this.opened;
    this.opened = debeAbrir;
    this.activeIndex = this.filteredOptions.length ? 0 : -1;

    if (!debeAbrir) {
      this.panelPositioned.set(false);
      return;
    }

    // El overlay solo emite `attach` al pasar de cerrado a abierto. Si se
    // oculta el panel en cada pulsación, las respuestas remotas sí llegan,
    // pero la lista queda invisible desde el siguiente carácter escrito.
    if (!estabaAbierto) this.panelPositioned.set(false);
    this.syncPanelWidth();
  }

  onFocus(event: FocusEvent): void {
    if (this.selectedOption) (event.target as HTMLInputElement).select();
    if (this.normalizedQuery.length >= this.resolvedMinSearchLength) this.open();
  }

  handleKeydown(event: KeyboardEvent): void {
    const options = this.filteredOptions;

    if (event.key === 'Escape') {
      this.close();
      return;
    }

    if (event.key === 'Tab') {
      this.close();
      return;
    }

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      if (!options.length) return;
      event.preventDefault();
      this.open();
      const increment = event.key === 'ArrowDown' ? 1 : -1;
      this.activeIndex = (this.activeIndex + increment + options.length) % options.length;
      return;
    }

    if (event.key === 'Enter' && this.opened && this.activeIndex >= 0) {
      event.preventDefault();
      const option = options[this.activeIndex];
      if (option) this.selectOption(option);
    }
  }

  open(): void {
    if (this.isDisabled || this.normalizedQuery.length < this.resolvedMinSearchLength) return;

    // Conserva visible un overlay ya adjunto al recuperar el foco o navegar
    // con las flechas. `attach` no vuelve a dispararse mientras siga abierto.
    if (!this.opened) {
      this.opened = true;
      this.panelPositioned.set(false);
    }
    this.syncPanelWidth();
  }

  close(): void {
    if (!this.opened) return;
    this.opened = false;
    this.activeIndex = -1;
    this.panelPositioned.set(false);
    this.markAsTouched();
  }

  clear(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.value = null;
    this.query = '';
    this.queryChange.emit('');
    this.onChange(null);
    this.markAsTouched();
    this.close();
  }

  selectOption(option: UiAutocompleteOption<T>): void {
    this.value = option.value;
    this.query = '';
    this.onChange(option.value);
    this.optionSelected.emit(option);
    this.close();
    this.markAsTouched();
  }

  activate(index: number): void {
    this.activeIndex = index;
  }

  onPanelAttached(): void {
    this.panelPositioned.set(true);
  }

  onPanelPositionChange(event: ConnectedOverlayPositionChange): void {
    this.panelPlacement.set(event.connectionPair.overlayY === 'bottom' ? 'above' : 'below');
  }

  markAsTouched(): void {
    if (this.touched) return;
    this.touched = true;
    this.onTouched();
  }

  trackOption(index: number, _option: UiAutocompleteOption<T>): number {
    return index;
  }

  get selectedOption(): UiAutocompleteOption<T> | null {
    return this.options.find((option) => Object.is(option.value, this.value)) ?? null;
  }

  get visibleText(): string {
    return this.selectedOption?.label ?? this.query;
  }

  get filteredOptions(): readonly UiAutocompleteOption<T>[] {
    if (this.normalizedQuery.length < this.resolvedMinSearchLength || this.selectedOption)
      return [];

    const opciones = this.serverSearch
      ? this.options
      : this.options.filter((option) =>
          this.normalize(
            `${option.label} ${option.description ?? ''} ${option.searchText ?? ''} ${String(option.value)}`,
          ).includes(this.normalizedQuery),
        );

    return opciones.slice(0, this.resolvedMaxResults);
  }

  get isDisabled(): boolean {
    return this.disabled || this.controlDisabled;
  }

  get describedBy(): string | null {
    if (this.error) return `${this.inputId}-error`;
    if (this.hint) return `${this.inputId}-hint`;
    return null;
  }

  get showMinimumCharactersMessage(): boolean {
    return (
      !this.error &&
      !this.hint &&
      this.query.length > 0 &&
      this.query.length < this.resolvedMinSearchLength
    );
  }

  private get normalizedQuery(): string {
    return this.normalize(this.query.trim());
  }

  private get resolvedMinSearchLength(): number {
    return Number.isFinite(this.minSearchLength) ? Math.max(1, this.minSearchLength) : 2;
  }

  private get resolvedMaxResults(): number {
    return Number.isFinite(this.maxResults) ? Math.max(1, this.maxResults) : 8;
  }

  private normalize(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLocaleLowerCase('es');
  }

  private syncPanelWidth(): void {
    const control = this.elemento.nativeElement.querySelector('.ui-autocomplete__control');
    if (!control) return;

    const viewportWidth = this.windowRef?.innerWidth ?? control.clientWidth;
    const viewportMargin = viewportWidth <= 760 ? 8 : 12;
    const availableWidth = Math.max(0, viewportWidth - viewportMargin * 2);
    this.panelWidth.set(Math.min(control.getBoundingClientRect().width, availableWidth));
  }
}
