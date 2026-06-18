import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { LoginVisualItem } from '../../core/services/administracion/login-visual.service';

@Component({
  selector: 'app-login-reorder-modal',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  templateUrl: './login-reorder-modal.html',
  styleUrls: ['./login-reorder-modal.scss']
})
export class LoginReorderModalComponent implements OnChanges {
  @Input() items: LoginVisualItem[] = [];
  @Input() isOpen = false;
  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<LoginVisualItem[]>();

  reorderItems: LoginVisualItem[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] && this.items) {
      this.reorderItems = [...this.items].sort((a, b) => a.order - b.order);
    }
    if (changes['isOpen'] && this.isOpen) {
      this.reorderItems = [...this.items].sort((a, b) => a.order - b.order);
    }
  }

  onDrop(event: CdkDragDrop<LoginVisualItem[]>): void {
    moveItemInArray(this.reorderItems, event.previousIndex, event.currentIndex);
  }

  onSave(): void {
    const updatedItems = this.reorderItems.map((item, index) => ({
      ...item,
      order: index + 1
    }));
    this.saved.emit(updatedItems);
    this.close();
  }

  onCancel(): void {
    this.close();
  }

  private close(): void {
    this.reorderItems = [];
    this.closed.emit();
  }

  getImageUrl(item: LoginVisualItem): string {
    return item.url || '/api/LoginVisual/image/' + item.file;
  }
}