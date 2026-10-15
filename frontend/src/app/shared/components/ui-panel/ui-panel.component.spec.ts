import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { UiPanelComponent } from './ui-panel.component';

describe('UiPanelComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [UiPanelComponent] });
  });

  it('minimiza y restaura el contenido cuando el control está habilitado', () => {
    const fixture = TestBed.createComponent(UiPanelComponent);
    fixture.componentRef.setInput('title', 'Panel de prueba');
    fixture.componentRef.setInput('minimizable', true);
    fixture.detectChanges();

    const minimizeButton: HTMLButtonElement = fixture.nativeElement.querySelector('.is-min');
    minimizeButton.click();
    fixture.detectChanges();

    const body: HTMLDivElement = fixture.nativeElement.querySelector('.ui-window-panel__body');
    expect(fixture.componentInstance.minimized()).toBe(true);
    expect(body.hidden).toBe(true);

    minimizeButton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.minimized()).toBe(false);
    expect(body.hidden).toBe(false);
  });

  it('actualiza visible y emite closed al cerrar', () => {
    const fixture = TestBed.createComponent(UiPanelComponent);
    const listener = vi.fn();
    fixture.componentInstance.closed.subscribe(listener);
    fixture.componentRef.setInput('title', 'Panel de prueba');
    fixture.componentRef.setInput('closable', true);
    fixture.detectChanges();

    const closeButton: HTMLButtonElement = fixture.nativeElement.querySelector('.is-close');
    closeButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.visible()).toBe(false);
    expect(listener).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.querySelector('.ui-window-panel')).toBeNull();
  });

  it('no muestra controles cuando no se solicitan', () => {
    const fixture = TestBed.createComponent(UiPanelComponent);
    fixture.componentRef.setInput('title', 'Panel sin controles');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.ui-window-controls')).toBeNull();
  });
});
