import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface CardInfo {
  icon: string;
  title: string;
  description: string;
  route: string;
}

@Component({
  selector: 'app-centro-analisis-inicio',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './centro-analisis-inicio.html',
  styleUrls: ['./centro-analisis-inicio.scss']
})
export class CentroAnalisisInicioComponent {
  cards: CardInfo[] = [
    {
      icon: 'fa-solid fa-magnifying-glass-chart',
      title: 'Recolección de Información',
      description: 'Gestión y análisis de hechos noticiosos y cobertura de vocerías oficiales.',
      route: '/centro-analisis/recoleccion'
    },
    {
      icon: 'fa-solid fa-chart-line',
      title: 'Reportes',
      description: 'Indicadores, consolidados e informes del impacto informativo en medios de comunicación.',
      route: '/centro-analisis/reportes'
    }
  ];
}
