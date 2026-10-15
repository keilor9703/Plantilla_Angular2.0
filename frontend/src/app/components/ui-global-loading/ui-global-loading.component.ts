import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { GlobalLoadingService } from '../../core/loading/global-loading.service';
import { RouterLoadingService } from '../../core/loading/router-loading.service';
import { BrandIdentityService } from '../../core/services/brand-identity.service';
import { UiSpinnerComponent } from '../ui-spinner/ui-spinner.component';

/**
 * Overlay raíz que comunica las cargas globales sin exigir lógica adicional en
 * páginas, módulos o submódulos. Reutiliza el spinner institucional compartido.
 */
@Component({
  selector: 'app-ui-global-loading',
  standalone: true,
  imports: [UiSpinnerComponent],
  templateUrl: './ui-global-loading.component.html',
  styleUrl: './ui-global-loading.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiGlobalLoadingComponent {
  protected readonly loading = inject(GlobalLoadingService);
  private readonly routerLoading = inject(RouterLoadingService);
  private readonly brandIdentity = inject(BrandIdentityService);

  protected readonly systemAcronym = computed(
    () => this.brandIdentity.identity().shortName || 'SICOE',
  );

  constructor() {
    this.routerLoading.start();
  }
}
