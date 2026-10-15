import { ChangeDetectionStrategy, Component, forwardRef, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MenuItem } from '../../core/interfaces/menu-item.interface';

@Component({
  selector: 'app-sidebar-menu-node',
  standalone: true,
  imports: [RouterLink, forwardRef(() => SidebarMenuNodeComponent)],
  templateUrl: './sidebar-menu-node.html',
  styleUrl: './sidebar-menu-node.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarMenuNodeComponent {
  readonly item = input.required<MenuItem>();
  readonly depth = input(0);
  readonly sidebarOpen = input(false);
  readonly expandedIds = input.required<ReadonlySet<number>>();
  readonly currentUrl = input('/');

  readonly toggleRequested = output<number>();
  readonly navigateRequested = output<void>();

  isExpanded(): boolean {
    return this.expandedIds().has(this.item().id);
  }

  isActive(): boolean {
    return this.containsActiveRoute(this.item());
  }

  isCurrentRoute(): boolean {
    const route = this.item().route;
    return Boolean(route && this.routeMatches(route));
  }

  isLinkTarget(): boolean {
    const target = this.item().target;
    return target === 'external' || target === 'document';
  }

  toggle(): void {
    this.toggleRequested.emit(this.item().id);
  }

  private containsActiveRoute(item: MenuItem): boolean {
    return (
      Boolean(item.route && this.routeMatches(item.route)) ||
      (item.children?.some((child) => this.containsActiveRoute(child)) ?? false)
    );
  }

  private routeMatches(route: string): boolean {
    if (!route || /^(https?:\/\/|www\.|mailto:|tel:)/i.test(route)) {
      return false;
    }

    const currentPath = this.currentUrl().split(/[?#]/, 1)[0].replace(/\/+$/, '') || '/';
    const candidatePath = route.split(/[?#]/, 1)[0].replace(/\/+$/, '') || '/';
    return (
      currentPath === candidatePath ||
      (candidatePath !== '/' && currentPath.startsWith(`${candidatePath}/`))
    );
  }
}
