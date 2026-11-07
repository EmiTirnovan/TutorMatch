import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  SEARCH_DEBOUNCE_MS,
  SEARCH_PLACEHOLDER,
  SemanticSearchBarComponent
} from '../../../../../src/app/features/tutors/components/semantic-search-bar/semantic-search-bar.component';

describe('SemanticSearchBarComponent', () => {
  let fixture: ComponentFixture<SemanticSearchBarComponent>;
  let emitted: string[];

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const input = (): HTMLInputElement => element().querySelector('input') as HTMLInputElement;
  const button = (): HTMLButtonElement => element().querySelector('button') as HTMLButtonElement;

  const type = (text: string): void => {
    input().value = text;
    input().dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    (element().querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SemanticSearchBarComponent] }).compileComponents();
    vi.useFakeTimers();
    fixture = TestBed.createComponent(SemanticSearchBarComponent);
    emitted = [];
    fixture.componentInstance.searchRequested.subscribe((query) => emitted.push(query));
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('Given_EmptyBar_When_Rendering_Then_ShowsPlaceholderAndDisabledSearchButton', () => {
    expect(input().placeholder).toBe('Descrie problema la care ai nevoie de ajutor...');
    expect(SEARCH_PLACEHOLDER).toBe(input().placeholder);
    expect(button().disabled).toBe(true);
    expect(button().textContent).toContain('Caută');
  });

  it('Given_TypedQuery_When_Submitting_Then_EmitsTrimmedQuery', () => {
    type('  pointeri in C  ');

    submit();

    expect(emitted).toEqual(['pointeri in C']);
  });

  it('Given_TypedQuery_When_ClickingSearchButton_Then_EmitsQuery', () => {
    type('sortare');
    const submitEvents: Event[] = [];
    element().querySelector('form')?.addEventListener('submit', (event) => submitEvents.push(event));

    button().click();

    expect(submitEvents.length).toBe(1);
    expect(submitEvents[0].defaultPrevented).toBe(true);
    expect(emitted).toEqual(['sortare']);
  });

  it('Given_BlankQuery_When_Submitting_Then_DoesNotEmit', () => {
    type('    ');

    submit();

    expect(emitted).toEqual([]);
    expect(button().disabled).toBe(true);
  });

  it('Given_TypedQuery_When_PausingLongerThanDebounce_Then_EmitsOnce', () => {
    type('baze de date');

    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 1);
    expect(emitted).toEqual([]);

    vi.advanceTimersByTime(1);
    expect(emitted).toEqual(['baze de date']);
  });

  it('Given_RapidTyping_When_Debouncing_Then_EmitsOnlyLatestValue', () => {
    type('poi');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 100);
    type('pointeri');

    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    expect(emitted).toEqual(['pointeri']);
  });

  it('Given_TooShortQuery_When_Debouncing_Then_DoesNotEmit', () => {
    type('ab');

    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    expect(emitted).toEqual([]);
  });

  it('Given_SameQueryTwice_When_Debouncing_Then_EmitsOnlyOnce', () => {
    type('pointeri');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    type('pointeri ');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    expect(emitted).toEqual(['pointeri']);
  });

  it('Given_TypedText_When_Typing_Then_UpdatesTwoWayQueryModelImmediately', () => {
    type('grafuri');

    expect(fixture.componentInstance.query()).toBe('grafuri');
  });

  it('Given_ParentSetsQuery_When_Rendering_Then_InputShowsValueWithoutEmitting', () => {
    fixture.componentInstance.query.set('Pointeri în C');
    fixture.detectChanges();

    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    expect(input().value).toBe('Pointeri în C');
    expect(emitted).toEqual([]);
  });

  it('Given_LoadingTrue_When_Rendering_Then_DisablesInputAndButtonAndShowsSpinner', () => {
    type('pointeri');

    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(input().disabled).toBe(true);
    expect(button().disabled).toBe(true);
    expect(element().querySelector('.spinner')).not.toBeNull();
    expect(button().textContent).toContain('Se caută');
  });

  it('Given_Loading_When_Submitting_Then_DoesNotEmit', () => {
    type('pointeri');
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    fixture.componentInstance.submit();

    expect(emitted).toEqual([]);
  });

  it('Given_LoadingFinished_When_Rendering_Then_ReenablesInputAndButton', () => {
    type('pointeri');
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    expect(input().disabled).toBe(false);
    expect(button().disabled).toBe(false);
    expect(element().querySelector('.spinner')).toBeNull();
  });

  it('Given_PendingDebounce_When_ComponentIsDestroyed_Then_DoesNotEmit', () => {
    type('pointeri');

    fixture.destroy();
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);

    expect(emitted).toEqual([]);
  });
});
