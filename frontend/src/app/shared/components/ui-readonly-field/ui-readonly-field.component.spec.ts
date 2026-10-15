import { TestBed } from '@angular/core/testing';

import { UiReadonlyFieldComponent } from './ui-readonly-field.component';

describe('UiReadonlyFieldComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [UiReadonlyFieldComponent] });
  });

  it('asocia la etiqueta con el valor de solo lectura', () => {
    const fixture = TestBed.createComponent(UiReadonlyFieldComponent);
    fixture.componentRef.setInput('label', 'Unidad Pertenece');
    fixture.componentRef.setInput('value', 'OFITE');
    fixture.detectChanges();

    const label: HTMLLabelElement = fixture.nativeElement.querySelector('label');
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');

    expect(input.readOnly).toBe(true);
    expect(input.value).toBe('OFITE');
    expect(label.htmlFor).toBe(input.id);
  });

  it('muestra el placeholder cuando no existe información', () => {
    const fixture = TestBed.createComponent(UiReadonlyFieldComponent);
    fixture.componentRef.setInput('label', 'Teléfono Fijo');
    fixture.componentRef.setInput('value', null);
    fixture.detectChanges();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    expect(input.value).toBe('');
    expect(input.placeholder).toBe('Sin información');
  });

  it('aplica la densidad compacta', () => {
    const fixture = TestBed.createComponent(UiReadonlyFieldComponent);
    fixture.componentRef.setInput('label', 'Estado Civil');
    fixture.componentRef.setInput('size', 'sm');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.ui-readonly-field--sm')).not.toBeNull();
  });
});
