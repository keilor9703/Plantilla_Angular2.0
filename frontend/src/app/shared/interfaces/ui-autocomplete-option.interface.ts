export interface UiAutocompleteOption<T = string | number> {
  readonly value: T;
  readonly label: string;
  readonly description?: string;
  /** Texto adicional que participa en el filtro sin mostrarse en la lista. */
  readonly searchText?: string;
}
