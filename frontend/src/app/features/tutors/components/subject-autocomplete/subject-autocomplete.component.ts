import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  computed,
  forwardRef,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, finalize, of, switchMap } from 'rxjs';
import { SubjectService } from '../../../../core/services/subject.service';

@Component({
  selector: 'app-subject-autocomplete',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './subject-autocomplete.component.html',
  styleUrl: './subject-autocomplete.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SubjectAutocompleteComponent),
      multi: true
    }
  ]
})
export class SubjectAutocompleteComponent implements OnInit, ControlValueAccessor {
  private readonly subjectService = inject(SubjectService, { optional: true });
  private readonly destroyRef = inject(DestroyRef);
  private readonly inputRef = viewChild<ElementRef<HTMLInputElement>>('subjectInput');

  // Configurable inputs
  readonly placeholder = input<string>('Type a subject (e.g. Java, Algorithms)...');
  readonly selectedSubjects = input<string[]>([]);
  readonly allowCustom = input<boolean>(true);
  readonly showAddButton = input<boolean>(true);
  readonly clearOnSelect = input<boolean | null>(null);
  readonly inputId = input<string>('subject-autocomplete-input');
  readonly disabled = input<boolean>(false);
  readonly ariaLabel = input<string>('Subject autocomplete input');

  // Outputs
  readonly subjectSelected = output<string>();

  // State
  readonly queryControl = new FormControl<string>('');
  readonly suggestions = signal<string[]>([]);
  readonly isOpen = signal<boolean>(false);
  readonly isLoading = signal<boolean>(false);
  readonly hasError = signal<boolean>(false);
  readonly activeIndex = signal<number>(-1);

  readonly hasQuery = computed(() => !!this.queryControl.value?.trim());

  readonly shouldClearOnSelect = computed(() => {
    const explicit = this.clearOnSelect();
    if (explicit !== null) return explicit;
    // Default to clearing if acting as a tag picker
    return this.selectedSubjects().length > 0;
  });

  readonly activeDescendantId = computed(() => {
    const idx = this.activeIndex();
    return idx >= 0 ? `${this.inputId()}-option-${idx}` : undefined;
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    const val = target ? target.value : (this.queryControl.value || '');
    this.onChange(val);
  }

  ngOnInit(): void {
    this.queryControl.valueChanges
      .pipe(
        debounceTime(200),
        distinctUntilChanged(),
        switchMap(query => {
          const trimmed = (query || '').trim();
          if (!trimmed) {
            this.suggestions.set([]);
            this.isOpen.set(false);
            this.hasError.set(false);
            this.isLoading.set(false);
            return of([]);
          }

          if (!this.subjectService) {
            return of([]);
          }

          this.isLoading.set(true);
          this.hasError.set(false);

          return this.subjectService.suggestSubjects(trimmed).pipe(
            catchError(() => {
              this.hasError.set(true);
              return of([]);
            }),
            finalize(() => this.isLoading.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(results => {
        const topThree = (results || []).slice(0, 3);
        this.suggestions.set(topThree);
        this.activeIndex.set(-1);

        const currentQuery = (this.queryControl.value || '').trim();
        this.isOpen.set(topThree.length > 0 || (this.allowCustom() && !!currentQuery));
      });
  }

  // ControlValueAccessor implementation
  writeValue(obj: any): void {
    const stringVal = obj !== null && obj !== undefined ? String(obj) : '';
    this.queryControl.setValue(stringVal, { emitEvent: false });
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) {
      this.queryControl.disable({ emitEvent: false });
    } else {
      this.queryControl.enable({ emitEvent: false });
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (this.disabled() || this.queryControl.disabled) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (!this.isOpen() && this.suggestions().length > 0) {
        this.isOpen.set(true);
      }
      const count = this.suggestions().length;
      if (count > 0) {
        const next = this.activeIndex() + 1;
        this.activeIndex.set(next < count ? next : 0);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const count = this.suggestions().length;
      if (count > 0) {
        const prev = this.activeIndex() - 1;
        this.activeIndex.set(prev >= 0 ? prev : count - 1);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const currentIdx = this.activeIndex();
      if (currentIdx >= 0 && currentIdx < this.suggestions().length) {
        this.selectSubject(this.suggestions()[currentIdx]);
      } else {
        const query = (this.queryControl.value || '').trim();
        if (query && this.allowCustom()) {
          this.selectSubject(query);
        }
      }
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.closeDropdown();
    }
  }

  selectSubject(subject: string): void {
    const trimmed = subject.trim();
    if (!trimmed) return;

    // Prevent duplicate selection case-insensitively
    const isDuplicate = this.selectedSubjects().some(
      s => s.toLowerCase() === trimmed.toLowerCase()
    );

    if (!isDuplicate) {
      this.subjectSelected.emit(trimmed);
    }

    if (this.shouldClearOnSelect()) {
      this.queryControl.setValue('', { emitEvent: true });
    } else {
      this.queryControl.setValue(trimmed, { emitEvent: true });
    }

    this.suggestions.set([]);
    this.isOpen.set(false);
    this.activeIndex.set(-1);
    this.hasError.set(false);
  }

  addCustomFromInput(): void {
    const query = (this.queryControl.value || '').trim();
    if (query && this.allowCustom()) {
      this.selectSubject(query);
    }
  }

  closeDropdown(): void {
    this.isOpen.set(false);
    this.activeIndex.set(-1);
    this.onTouched();
  }

  onFocus(): void {
    const query = (this.queryControl.value || '').trim();
    if (query && (this.suggestions().length > 0 || this.allowCustom())) {
      this.isOpen.set(true);
    }
  }

  onBlur(): void {
    this.onTouched();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const hostEl = this.inputRef()?.nativeElement;
    if (hostEl && !hostEl.closest('.autocomplete-container')?.contains(event.target as Node)) {
      this.closeDropdown();
    }
  }
}
