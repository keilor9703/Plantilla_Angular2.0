import { CommonModule } from '@angular/common';
import { Component, HostListener, Input } from '@angular/core';

export interface SocialDockAccount {
  id: number;
  networkName: string;
  accountName: string;
  icon: string;
  url: string;
  color: string;
  handle?: string;
  avatarUrl?: string;
  notifications?: number;
}

export interface SocialDockGroup {
  networkName: string;
  icon: string;
  color: string;
  accounts: SocialDockAccount[];
}

@Component({
  selector: 'app-social-dock',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './social-dock.html',
  styleUrl: './social-dock.scss'
})
export class SocialDockComponent {
  @Input() groups: SocialDockGroup[] = [];

  collapsed = true;
  panelOpen = false;
  selectedGroup: SocialDockGroup | null = null;

  toggleCollapsed(): void {
    this.collapsed = !this.collapsed;
  }

  openGroup(group: SocialDockGroup): void {
    this.selectedGroup = group;
    this.panelOpen = true;
    document.body.classList.add('ui-modal-open');
  }

  closePanel(): void {
    this.panelOpen = false;
    this.selectedGroup = null;
    document.body.classList.remove('ui-modal-open');
  }

  getHandle(account: SocialDockAccount): string {
    const raw = (account.handle ?? account.accountName ?? '').trim();
    if (!raw) {
      return '@cuenta';
    }

    if (raw.startsWith('@')) {
      return raw;
    }

    return `@${raw.replace(/\s+/g, '').toLowerCase()}`;
  }

  getAvatar(account: SocialDockAccount): string {
    const raw = (account.avatarUrl ?? '').trim();
    if (raw) {
      return raw;
    }

    return 'assets/avatar-default.svg';
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.panelOpen) {
      this.closePanel();
    }
  }
}
