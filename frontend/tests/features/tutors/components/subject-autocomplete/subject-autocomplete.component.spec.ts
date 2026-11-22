import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SubjectAutocompleteComponent } from '../../../../../src/app/features/tutors/components/subject-autocomplete/subject-autocomplete.component';
import { SubjectService } from '../../../../../src/app/core/services/subject.service';

describe('SubjectAutocompleteComponent', () => {
  let component: SubjectAutocompleteComponent;
  let fixture: ComponentFixture<SubjectAutocompleteComponent>;
  let subjectService: SubjectService;

  beforeEach(async () => {
    vi.useFakeTimers();
    const subjectServiceMock = {
      suggestSubjects: vi.fn().mockReturnValue(of(['Algoritmi', 'Arhitectura', 'Algebra']))
    };

    await TestBed.configureTestingModule({
      imports: [SubjectAutocompleteComponent],
      providers: [
        { provide: SubjectService, useValue: subjectServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SubjectAutocompleteComponent);
    component = fixture.componentInstance;
    subjectService = TestBed.inject(SubjectService);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('Given_Component_When_Initialized_Then_HasComboboxRoleAndCollapsedState', () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;

    expect(input.getAttribute('role')).toBe('combobox');
    expect(input.getAttribute('aria-autocomplete')).toBe('list');
    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(component.isOpen()).toBe(false);
  });

  it('Given_UserTypesQuery_When_Debounced_Then_CallsServiceAndDisplaysSuggestions', () => {
    component.queryControl.setValue('alg');
    vi.advanceTimersByTime(199); // Before 200ms debounce
    expect(subjectService.suggestSubjects).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1); // 200ms reached
    fixture.detectChanges();

    expect(subjectService.suggestSubjects).toHaveBeenCalledWith('alg');
    expect(component.suggestions()).toEqual(['Algoritmi', 'Arhitectura', 'Algebra']);
    expect(component.isOpen()).toBe(true);

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.getAttribute('aria-expanded')).toBe('true');

    const options = fixture.nativeElement.querySelectorAll('li[role="option"]');
    expect(options.length).toBe(3);
  });

  it('Given_DistinctUntilChanged_When_TypingSameValue_Then_DoesNotRequery', () => {
    component.queryControl.setValue('algo');
    vi.advanceTimersByTime(200);
    expect(subjectService.suggestSubjects).toHaveBeenCalledTimes(1);

    component.queryControl.setValue('algo');
    vi.advanceTimersByTime(200);
    expect(subjectService.suggestSubjects).toHaveBeenCalledTimes(1);
  });

  it('Given_ServiceReturnsMoreThanThree_When_Displaying_Then_LimitsToMaxThree', () => {
    vi.spyOn(subjectService, 'suggestSubjects').mockReturnValue(
      of(['Subj1', 'Subj2', 'Subj3', 'Subj4', 'Subj5'])
    );

    component.queryControl.setValue('test');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    expect(component.suggestions().length).toBe(3);
    expect(component.suggestions()).toEqual(['Subj1', 'Subj2', 'Subj3']);
  });

  it('Given_SuggestionsOpen_When_NavigatingWithArrows_Then_UpdatesActiveIndexAndAriaDescendant', () => {
    component.queryControl.setValue('alg');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;

    // Arrow Down -> Index 0
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    fixture.detectChanges();
    expect(component.activeIndex()).toBe(0);
    expect(input.getAttribute('aria-activedescendant')).toBe('subject-autocomplete-input-option-0');

    // Arrow Down -> Index 1
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    fixture.detectChanges();
    expect(component.activeIndex()).toBe(1);

    // Arrow Up -> Index 0
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    fixture.detectChanges();
    expect(component.activeIndex()).toBe(0);
  });

  it('Given_ActiveOption_When_PressingEnter_Then_EmitsSelectionAndCloses', () => {
    let selected: string | undefined;
    component.subjectSelected.subscribe(val => selected = val);

    component.queryControl.setValue('alg');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' })); // activeIndex = 0 ("Algoritmi")
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(selected).toBe('Algoritmi');
    expect(component.isOpen()).toBe(false);
  });

  it('Given_SuggestionsOpen_When_ClickingOption_Then_EmitsSelection', () => {
    let selected: string | undefined;
    component.subjectSelected.subscribe(val => selected = val);

    component.queryControl.setValue('alg');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll('li[role="option"]') as NodeListOf<HTMLElement>;
    options[1].dispatchEvent(new MouseEvent('mousedown'));
    fixture.detectChanges();

    expect(selected).toBe('Arhitectura');
    expect(component.isOpen()).toBe(false);
  });

  it('Given_SuggestionsOpen_When_PressingEscape_Then_ClosesDropdown', () => {
    component.queryControl.setValue('alg');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    expect(component.isOpen()).toBe(true);

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();

    expect(component.isOpen()).toBe(false);
    expect(input.getAttribute('aria-expanded')).toBe('false');
  });

  it('Given_CustomSubject_When_PressingEnterWithoutActiveSelection_Then_EmitsCustomSubject', () => {
    let selected: string | undefined;
    component.subjectSelected.subscribe(val => selected = val);

    component.queryControl.setValue('Quantum Computing');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(selected).toBe('Quantum Computing');
  });

  it('Given_DuplicateSubject_When_Selected_Then_DoesNotEmitDuplicate', () => {
    fixture.componentRef.setInput('selectedSubjects', ['Algoritmi', 'Java']);
    fixture.detectChanges();

    let emitted = false;
    component.subjectSelected.subscribe(() => emitted = true);

    component.selectSubject('algoritmi'); // Same subject, different casing

    expect(emitted).toBe(false);
  });

  it('Given_ServiceError_When_Fetching_Then_SetsHasErrorAndLeavesDropdownOpenForCustomEntry', () => {
    vi.spyOn(subjectService, 'suggestSubjects').mockReturnValue(throwError(() => new Error('Network error')));

    component.queryControl.setValue('error test');
    vi.advanceTimersByTime(200);
    fixture.detectChanges();

    expect(component.hasError()).toBe(true);
    expect(component.isLoading()).toBe(false);

    const errorNotice = fixture.nativeElement.querySelector('.error-notice');
    expect(errorNotice).toBeTruthy();
  });
});
