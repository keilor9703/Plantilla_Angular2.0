import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { RadioPlayerService } from '../../core/services/radio-player.service';
import { RadioPlayerComponent } from '../footer/radio-player/radio-player.component';

@Component({
  selector: 'app-mobile-bottom-nav',
  standalone: true,
  imports: [RouterLink, RadioPlayerComponent],
  templateUrl: './mobile-bottom-nav.html',
  styleUrl: './mobile-bottom-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MobileBottomNavComponent {
  private readonly router = inject(Router);
  readonly player = inject(RadioPlayerService);

  readonly isRadioOpen = signal(false);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  isHomeActive(): boolean {
    const path = this.currentUrl().split(/[?#]/, 1)[0];
    return path === '/home' || path === '/';
  }

  isAdminActive(): boolean {
    const path = this.currentUrl().split(/[?#]/, 1)[0];
    return path === '/administracion' || path.startsWith('/administracion/');
  }

  toggleRadioSheet(): void {
    this.isRadioOpen.update((open) => !open);
  }

  closeRadioSheet(): void {
    this.isRadioOpen.set(false);
  }
}
