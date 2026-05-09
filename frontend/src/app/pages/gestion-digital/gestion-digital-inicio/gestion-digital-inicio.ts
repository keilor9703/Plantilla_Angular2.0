import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MenuService } from '../../../core/services/administracion/menu.service';
import { ModuleCard, buildModuleCards } from '../../../shared/utils/menu-module-cards';

@Component({
  selector: 'app-gestion-digital-inicio',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './gestion-digital-inicio.html',
  styleUrls: ['./gestion-digital-inicio.scss'],
})
export class GestionDigitalInicioComponent implements OnInit {
  sites: ModuleCard[] = [];
  loading = false;

  constructor(private menuService: MenuService) {}

  ngOnInit(): void {
    this.loadSites();
  }

  private loadSites(): void {
    this.loading = true;
    this.menuService.getMyMenu().subscribe({
      next: (items) => {
        this.sites = buildModuleCards(items ?? [], {
          prefix: '/gestion-digital',
          excludeRoutes: ['/gestion-digital/inicio', '/gestion-digital'],
        });
        this.loading = false;
      },
      error: () => {
        this.sites = [];
        this.loading = false;
      },
    });
  }
}
