import { ChangeDetectionStrategy, Component, effect, input, model, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

export const SEARCH_PLACEHOLDER = 'Descrie problema la care ai nevoie de ajutor...';
export const MIN_QUERY_LENGTH = 3;

@Component({
  selector: 'app-semantic-search-bar',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './semantic-search-bar.component.html',
  styleUrl: './semantic-search-bar.component.css'
})
export class SemanticSearchBarComponent {
  /** Current text. Two-way bindable so a parent (e.g. prompt chips) can populate the input. */
  readonly query = model('');
  readonly loading = input(false);

  /** Emitted exclusively on Enter / button click (form submission). */
  readonly searchRequested = output<string>();

  readonly placeholder = SEARCH_PLACEHOLDER;
  readonly control = new FormControl('', { nonNullable: true });

  constructor() {
    effect(() => {
      const value = this.query();
      if (this.control.value !== value) {
        this.control.setValue(value, { emitEvent: false });
      }
    });

    effect(() => {
      if (this.loading()) {
        this.control.disable({ emitEvent: false });
      } else {
        this.control.enable({ emitEvent: false });
      }
    });

    this.control.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => this.query.set(value));
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  submit(): void {
    const value = this.control.value.trim();
    if (!value || this.loading()) {
      return;
    }
    this.searchRequested.emit(value);
  }
}
