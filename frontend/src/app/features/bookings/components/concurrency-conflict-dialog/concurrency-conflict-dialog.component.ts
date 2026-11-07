import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, output, viewChild } from '@angular/core';
import { keepTabFocusInside, restoreFocusOnDestroy } from '../../../../core/utils/modal-focus';

@Component({
  selector: 'app-concurrency-conflict-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './concurrency-conflict-dialog.component.html',
  styleUrl: './concurrency-conflict-dialog.component.css'
})
export class ConcurrencyConflictDialogComponent implements AfterViewInit {
  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  readonly refresh = output<void>();

  constructor() {
    restoreFocusOnDestroy();
  }

  ngAfterViewInit(): void {
    this.dialog().nativeElement.focus();
  }

  onKeydown(event: KeyboardEvent): void {
    keepTabFocusInside(event, this.dialog().nativeElement);
  }
}
