import { ChangeDetectionStrategy, Component, effect, input, model, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, map } from 'rxjs';

export const SEARCH_PLACEHOLDER = 'Descrie problema la care ai nevoie de ajutor...';
export const SEARCH_DEBOUNCE_MS = 400;
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

  /** Emitted on Enter / button click, and after the user pauses typing a query of at least 3 characters. */
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

    this.control.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        map((value) => value.trim()),
        filter((value) => value.length >= MIN_QUERY_LENGTH),
        distinctUntilChanged(),
        takeUntilDestroyed()
      )
      .subscribe((value) => this.searchRequested.emit(value));
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
