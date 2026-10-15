import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiAvatarComponent } from '../../../../../shared/components/ui-avatar/ui-avatar.component';

@Component({
  selector: 'app-avatar-demo',
  standalone: true,
  imports: [UiAvatarComponent],
  templateUrl: './avatar-demo.component.html',
  styleUrl: './avatar-demo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvatarDemoComponent {}
