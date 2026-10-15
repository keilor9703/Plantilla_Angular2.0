import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  input,
  model,
  output,
} from '@angular/core';

let nextPanelId = 0;

/** Panel institucional con contenido proyectable y controles de ventana opcionales. */
@Component({
  selector: 'app-ui-panel',
  standalone: true,
  templateUrl: './ui-panel.component.html',
  styleUrl: './ui-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiPanelComponent {
  readonly title = input.required<string>();
  readonly icon = input('fa-solid fa-clipboard-list');
  readonly showIcon = input(true, { transform: booleanAttribute });
  readonly minimizable = input(false, { transform: booleanAttribute });
  readonly closable = input(false, { transform: booleanAttribute });
  readonly elevated = input(true, { transform: booleanAttribute });
  readonly bodyPadding = input('16px');
  readonly panelId = input(`ui-panel-${nextPanelId++}`);

  /** Admite uso autónomo o enlace bidireccional con [(minimized)]. */
  readonly minimized = model(false);
  /** Admite uso autónomo o enlace bidireccional con [(visible)]. */
  readonly visible = model(true);
  readonly closed = output<void>();

  toggleMinimized(): void {
    if (this.minimizable()) {
      this.minimized.update((value) => !value);
    }
  }

  close(): void {
    if (!this.closable()) return;

    this.visible.set(false);
    this.closed.emit();
  }
}
