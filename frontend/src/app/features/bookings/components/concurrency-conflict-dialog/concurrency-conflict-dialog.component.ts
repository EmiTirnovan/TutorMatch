import { AfterViewInit, Component, ElementRef, output, viewChild } from '@angular/core';

@Component({
  selector: 'app-concurrency-conflict-dialog',
  standalone: true,
  templateUrl: './concurrency-conflict-dialog.component.html'
})
export class ConcurrencyConflictDialogComponent implements AfterViewInit {
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly refresh = output<void>();

  ngAfterViewInit(): void {
    this.dialog().nativeElement.showModal();
  }
}
