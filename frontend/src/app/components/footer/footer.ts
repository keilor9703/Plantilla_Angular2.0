import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BrandingService } from '../../core/services/administracion/branding.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
})
export class FooterComponent implements OnInit, OnDestroy {
  systemName = 'OFTIC';
  systemDisplayName = 'Sistema de gestion de Policia';

  supportOpen = false;
  policyOpen = false;

  supportForm = {
    tipo: 'Incidente (error)',
    prioridad: 'Media',
    asunto: '',
    descripcion: '',
    incluirDatosTecnicos: true,
    adjuntos: [] as File[]
  };

  constructor(
    private brandingService: BrandingService
  ) {
    this.loadBranding();
  }

  ngOnInit(): void {}

  private loadBranding(): void {
    this.brandingService.getPublicConfig().subscribe({
      next: (cfg) => {
        const sigla = (cfg?.sistema ?? cfg?.systemName ?? '').trim();
        const nombre = (cfg?.nombreSistema ?? '').trim();
        this.systemName = sigla || 'OFTIC';
        this.systemDisplayName = nombre || 'Sistema de gestion de Policia';
      },
      error: () => {
        this.systemName = 'OFTIC';
        this.systemDisplayName = 'Sistema de gestion de Policia';
      }
    });
  }

  openSupport(): void {
    this.supportOpen = true;
    document.body.classList.add('ui-modal-open');
  }

  closeSupport(): void {
    this.supportOpen = false;
    if (!this.policyOpen) {
      document.body.classList.remove('ui-modal-open');
    }
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    this.supportForm.adjuntos = files;
  }

  submitSupport(): void {
    if (!this.supportForm.asunto.trim() || !this.supportForm.descripcion.trim()) {
      alert('Completa Asunto y Descripcion.');
      return;
    }

    this.resetSupportForm();
    this.closeSupport();
  }

  resetSupportForm(): void {
    this.supportForm = {
      tipo: 'Incidente (error)',
      prioridad: 'Media',
      asunto: '',
      descripcion: '',
      incluirDatosTecnicos: true,
      adjuntos: []
    };
  }

  openPolicy(): void {
    this.policyOpen = true;
    document.body.classList.add('ui-modal-open');
  }

  closePolicy(): void {
    this.policyOpen = false;

    if (!this.supportOpen) {
      document.body.classList.remove('ui-modal-open');
    }
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.supportOpen) {
      this.closeSupport();
      return;
    }

    if (this.policyOpen) {
      this.closePolicy();
    }
  }

  ngOnDestroy(): void {
    document.body.classList.remove('ui-modal-open');
  }
}


