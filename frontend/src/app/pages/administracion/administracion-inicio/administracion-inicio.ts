import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MenuService } from '../../../core/services/administracion/menu.service';
import { ModuleCard, buildModuleCards } from '../../../shared/utils/menu-module-cards';

@Component({
  selector: 'app-administracion-inicio',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './administracion-inicio.html',
  styleUrls: ['./administracion-inicio.scss']
})
export class AdministracionInicioComponent implements OnInit {
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
          prefix: '/administracion',
          excludeRoutes: ['/administracion/inicio', '/administracion'],
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
