import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { UiButtonComponent } from './ui-button.component';

describe('UiButtonComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [UiButtonComponent] });
  });

  it('presenta la acción extra pequeña con apariencia de enlace', () => {
    const fixture = TestBed.createComponent(UiButtonComponent);
    fixture.componentRef.setInput('size', 'xs');
    fixture.componentRef.setInput('appearance', 'link');
    fixture.componentRef.setInput('icon', 'fa-solid fa-check');
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.classList).toContain('ui-btn--xs');
    expect(button.classList).toContain('ui-btn--link');
    expect(button.querySelector('i.fa-check')).not.toBeNull();
  });

  it('conserva nombre accesible en una acción compacta de solo icono', () => {
    const fixture = TestBed.createComponent(UiButtonComponent);
    fixture.componentRef.setInput('size', 'xs');
    fixture.componentRef.setInput('iconOnly', true);
    fixture.componentRef.setInput('icon', 'fa-solid fa-xmark');
    fixture.componentRef.setInput('ariaLabel', 'Cerrar');
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.classList).toContain('ui-btn--icon-only');
    expect(button.getAttribute('aria-label')).toBe('Cerrar');
  });

  it('no emite la acción mientras está deshabilitado', () => {
    const fixture = TestBed.createComponent(UiButtonComponent);
    const listener = vi.fn();
    fixture.componentInstance.buttonClick.subscribe(listener);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();

    expect(button.disabled).toBe(true);
    expect(listener).not.toHaveBeenCalled();
  });
});
