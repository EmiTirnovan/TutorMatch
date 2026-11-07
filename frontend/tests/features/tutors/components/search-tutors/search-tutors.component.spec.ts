import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NEVER, Observable, Subject, of, throwError } from 'rxjs';
import { SearchTutorsComponent, SEARCH_PAGE_SIZE } from '../../../../../src/app/features/tutors/components/search-tutors/search-tutors.component';
import { TutorService } from '../../../../../src/app/core/services/tutor.service';
import { SemanticSearchService } from '../../../../../src/app/core/services/semantic-search.service';
import { SemanticSearchResult } from '../../../../../src/app/core/models/semantic-search.models';
import { TutorSearchFilters, TutorSearchItem, TutorSearchResponse } from '../../../../../src/app/core/models/tutor-search.models';

describe('SearchTutorsComponent', () => {
  let component: SearchTutorsComponent;
  let fixture: ComponentFixture<SearchTutorsComponent>;
  let searchSpy: ReturnType<typeof vi.fn<(filters: TutorSearchFilters) => Observable<TutorSearchResponse>>>;
  let semanticSpy: ReturnType<typeof vi.fn<(query: string) => Observable<SemanticSearchResult>>>;

  const tutor = (id: string, name: string, subjects: string[] = []): TutorSearchItem => ({
    id,
    name,
    hourlyRate: 70,
    averageRating: 4.5,
    ratingCount: 2,
    subjects
  });

  const response = (tutors: TutorSearchItem[], totalCount = tutors.length): TutorSearchResponse => ({
    tutors,
    totalCount,
    page: 1,
    pageSize: SEARCH_PAGE_SIZE,
    mode: 'keyword'
  });

  const createPage = (result: Observable<TutorSearchResponse>): void => {
    searchSpy = vi.fn<(filters: TutorSearchFilters) => Observable<TutorSearchResponse>>().mockReturnValue(result);
    semanticSpy = vi.fn<(query: string) => Observable<SemanticSearchResult>>();
    TestBed.configureTestingModule({
      imports: [SearchTutorsComponent],
      providers: [
        provideRouter([]),
        { provide: TutorService, useValue: { searchTutors: searchSpy } },
        { provide: SemanticSearchService, useValue: { search: semanticSpy } }
      ]
    });
    fixture = TestBed.createComponent(SearchTutorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;

  it('Given_Page_When_Initialized_Then_LoadsFirstPageWithDefaultFilters', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir'), tutor('2', 'Radu Ionescu')])));

    expect(searchSpy).toHaveBeenCalledWith({
      subject: '',
      maxPrice: null,
      date: '',
      sortBy: 'priceAsc',
      page: 1,
      pageSize: SEARCH_PAGE_SIZE
    });
    expect(element().querySelectorAll('app-tutor-card').length).toBe(2);
    expect(element().querySelector('.result-count')?.textContent).toContain('2 tutors found');
  });

  it('Given_TutorsWithSubjects_When_Rendering_Then_EachCardShowsItsOwnSubjectTags', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir', ['Algoritmi', 'SQL']), tutor('2', 'Radu Ionescu')])));

    const cards = Array.from(element().querySelectorAll('app-tutor-card'));
    const tagsOf = (card: Element): (string | undefined)[] =>
      Array.from(card.querySelectorAll('.subjects .badge')).map((tag) => tag.textContent?.trim());
    expect(tagsOf(cards[0])).toEqual(['Algoritmi', 'SQL']);
    expect(cards[1].querySelector('.subjects')).toBeNull();
  });

  it('Given_NewFilters_When_Applied_Then_ResetsToFirstPageAndSearchesWithFilters', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 20)));
    component.goToPage(3);

    component.onCriteriaChange({ subject: 'SQL', maxPrice: 80, date: '2026-11-10', sortBy: 'priceDesc' });

    expect(component.page()).toBe(1);
    expect(searchSpy).toHaveBeenLastCalledWith({
      subject: 'SQL',
      maxPrice: 80,
      date: '2026-11-10',
      sortBy: 'priceDesc',
      page: 1,
      pageSize: SEARCH_PAGE_SIZE
    });
  });

  it('Given_MultiplePages_When_GoingToPage_Then_LoadsRequestedPageWithinBounds', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));

    expect(component.totalPages()).toBe(3);
    component.goToPage(2);
    expect(searchSpy).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));

    searchSpy.mockClear();
    component.goToPage(4);
    component.goToPage(0);
    component.goToPage(2);
    expect(searchSpy).not.toHaveBeenCalled();
  });

  it('Given_NoResults_When_Rendering_Then_ShowsEmptyState', () => {
    createPage(of(response([])));

    expect(element().querySelector('.empty-state')).not.toBeNull();
    expect(element().querySelector('app-tutor-card')).toBeNull();
  });

  it('Given_ApiFailure_When_Loading_Then_ShowsErrorAndRetryReloads', () => {
    createPage(throwError(() => new Error('network')));

    expect(element().querySelector('.alert-danger')).not.toBeNull();

    searchSpy.mockReturnValue(of(response([tutor('1', 'Maria Dragomir')])));
    component.loadTutors();
    fixture.detectChanges();

    expect(component.errorMessage()).toBeNull();
    expect(element().querySelectorAll('app-tutor-card').length).toBe(1);
  });

  const paginationButtons = (): HTMLButtonElement[] =>
    Array.from(element().querySelectorAll<HTMLButtonElement>('nav.pagination button'));

  it('Given_Page_When_Rendering_Then_ShowsHeadingAndFilterBar', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')])));

    expect(element().querySelector('h1')?.textContent).toContain('Discover tutors');
    expect(element().querySelector('app-tutor-filter-bar form')).not.toBeNull();
  });

  it('Given_PendingRequest_When_Loading_Then_ShowsSkeletonCardsAndHidesResultCount', () => {
    createPage(NEVER);

    expect(component.isLoading()).toBe(true);
    expect(element().querySelector('.tutor-grid')?.getAttribute('aria-busy')).toBe('true');
    expect(element().querySelectorAll('.skeleton-card').length).toBe(SEARCH_PAGE_SIZE);
    expect(element().querySelector('app-tutor-card')).toBeNull();
    expect(element().querySelector('.result-count')?.textContent?.trim()).toBe('');
  });

  it('Given_LoadedResults_When_Rendering_Then_HidesSkeletonCards', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')])));

    expect(component.isLoading()).toBe(false);
    expect(element().querySelector('.skeleton-card')).toBeNull();
    expect(element().querySelector('[aria-busy="true"]')).toBeNull();
  });

  it('Given_PendingRequest_When_ComponentIsDestroyed_Then_UnsubscribesFromSearch', () => {
    const pending = new Subject<TutorSearchResponse>();
    createPage(pending);
    expect(pending.observed).toBe(true);

    fixture.destroy();

    expect(pending.observed).toBe(false);
  });

  it('Given_SingleResult_When_Rendering_Then_ShowsSingularCount', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')])));

    const text = element().querySelector('.result-count')?.textContent ?? '';
    expect(text).toContain('1 tutor found');
    expect(text).not.toContain('tutors');
  });

  it('Given_NoResults_When_Rendering_Then_ShowsZeroCountEmptyMessageAndNoPagination', () => {
    createPage(of(response([])));

    expect(element().querySelector('.result-count')?.textContent).toContain('0 tutors found');
    expect(element().querySelector('.empty-state h3')?.textContent).toContain('No tutors match your filters');
    expect(element().querySelector('nav.pagination')).toBeNull();
  });

  it('Given_ApiFailure_When_Loading_Then_ShowsMessageHidesCountAndKeepsFilterBar', () => {
    createPage(throwError(() => new Error('network')));

    expect(element().querySelector('.alert-danger')?.textContent).toContain('We could not load tutors. Please try again.');
    expect(element().querySelector('.result-count')?.textContent?.trim()).toBe('');
    expect(element().querySelector('app-tutor-card')).toBeNull();
    expect(element().querySelector('app-tutor-filter-bar form')).not.toBeNull();
  });

  it('Given_ApiFailure_When_ClickingRetryButton_Then_ReloadsAndShowsResults', () => {
    createPage(throwError(() => new Error('network')));
    searchSpy.mockReturnValue(of(response([tutor('1', 'Maria Dragomir')])));
    const retry = element().querySelector('.alert-danger button') as HTMLButtonElement;

    retry.click();
    fixture.detectChanges();

    expect(retry.textContent).toContain('Retry');
    expect(searchSpy).toHaveBeenCalledTimes(2);
    expect(element().querySelector('.alert-danger')).toBeNull();
    expect(element().querySelectorAll('app-tutor-card').length).toBe(1);
  });

  it('Given_ApiFailureOnSecondPage_When_Retrying_Then_RequestsSamePage', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    searchSpy.mockReturnValue(throwError(() => new Error('network')));
    component.goToPage(2);
    fixture.detectChanges();
    searchSpy.mockClear();
    searchSpy.mockReturnValue(of(response([tutor('2', 'Radu Ionescu')], 14)));

    (element().querySelector('.alert-danger button') as HTMLButtonElement).click();

    expect(searchSpy).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }));
  });

  it('Given_SingleFullPage_When_Rendering_Then_HidesPagination', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], SEARCH_PAGE_SIZE)));

    expect(component.totalPages()).toBe(1);
    expect(element().querySelector('nav.pagination')).toBeNull();
  });

  it('Given_ResultsExceedingOnePage_When_Rendering_Then_ShowsPaginationOnFirstPage', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], SEARCH_PAGE_SIZE + 1)));

    const [previous, next] = paginationButtons();
    expect(element().querySelector('nav.pagination')?.getAttribute('aria-label')).toBe('Tutor results pages');
    expect(element().querySelector('nav.pagination')?.textContent).toContain('Page 1 of 2');
    expect(previous.disabled).toBe(true);
    expect(next.disabled).toBe(false);
  });

  it('Given_FirstPage_When_ClickingNext_Then_LoadsSecondPageAndUpdatesIndicator', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    const [, next] = paginationButtons();

    next.click();
    fixture.detectChanges();

    const [previous] = paginationButtons();
    expect(searchSpy).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
    expect(element().querySelector('nav.pagination')?.textContent).toContain('Page 2 of 3');
    expect(previous.disabled).toBe(false);
  });

  it('Given_SecondPage_When_ClickingPrevious_Then_ReturnsToFirstPage', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    component.goToPage(2);
    fixture.detectChanges();
    const [previous] = paginationButtons();

    previous.click();
    fixture.detectChanges();

    expect(searchSpy).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    expect(element().querySelector('nav.pagination')?.textContent).toContain('Page 1 of 3');
  });

  it('Given_LastPage_When_Rendering_Then_DisablesNextButton', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    component.goToPage(3);
    fixture.detectChanges();

    const [previous, next] = paginationButtons();
    expect(element().querySelector('nav.pagination')?.textContent).toContain('Page 3 of 3');
    expect(previous.disabled).toBe(false);
    expect(next.disabled).toBe(true);
  });

  it('Given_FirstPage_When_ClickingDisabledPrevious_Then_DoesNotSearchAgain', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    searchSpy.mockClear();
    const [previous] = paginationButtons();

    previous.click();

    expect(searchSpy).not.toHaveBeenCalled();
  });

  it('Given_CurrentPage_When_GoingToSamePage_Then_DoesNotSearchAgain', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    searchSpy.mockClear();

    component.goToPage(1);

    expect(searchSpy).not.toHaveBeenCalled();
  });

  it('Given_TypedSubjectOnSecondPage_When_SubmittingFilterBar_Then_SearchesFirstPageWithSubject', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')], 14)));
    component.goToPage(2);
    const subject = element().querySelector('#filter-subject') as HTMLInputElement;
    subject.value = 'SQL';
    subject.dispatchEvent(new Event('input'));

    (element().querySelector('app-tutor-filter-bar form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    expect(searchSpy).toHaveBeenLastCalledWith(expect.objectContaining({ subject: 'SQL', page: 1 }));
    expect(component.page()).toBe(1);
  });

  it('Given_AppliedFilters_When_ClickingReset_Then_SearchesWithDefaultCriteria', () => {
    createPage(of(response([tutor('1', 'Maria Dragomir')])));
    component.onCriteriaChange({ subject: 'SQL', maxPrice: 80, date: '2026-11-10', sortBy: 'priceDesc' });
    const reset = element().querySelector('app-tutor-filter-bar .btn-secondary') as HTMLButtonElement;

    reset.click();

    expect(searchSpy).toHaveBeenLastCalledWith({
      subject: '',
      maxPrice: null,
      date: '',
      sortBy: 'priceAsc',
      page: 1,
      pageSize: SEARCH_PAGE_SIZE
    });
  });

  it('Given_NewSearchWhilePreviousIsPending_When_Loading_Then_CancelsPreviousRequest', () => {
    const first = new Subject<TutorSearchResponse>();
    createPage(first);
    searchSpy.mockReturnValue(of(response([tutor('1', 'Maria Dragomir')])));

    component.loadTutors();
    fixture.detectChanges();

    expect(first.observed).toBe(false);
    expect(element().querySelectorAll('app-tutor-card').length).toBe(1);
  });

  describe('semantic search', () => {
    const semanticResult = (
      mode: SemanticSearchResult['mode'],
      scores: (number | null)[] = [0.91, 0.74]
    ): SemanticSearchResult => ({
      mode,
      message: null,
      tutors: scores.map((score, index) => ({ ...tutor(`s${index}`, `Tutor ${index}`), similarityScore: score }))
    });

    const typeAndSubmit = (text: string): void => {
      const input = element().querySelector('#semantic-search-input') as HTMLInputElement;
      input.value = text;
      input.dispatchEvent(new Event('input'));
      (element().querySelector('app-semantic-search-bar form') as HTMLFormElement).dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };

    it('Given_Query_When_SubmittingSearchBar_Then_ShowsSemanticResultsWithBadgeAndBestMatch', () => {
      createPage(of(response([tutor('1', 'Maria Dragomir')])));
      semanticSpy.mockReturnValue(of(semanticResult('semantic')));

      typeAndSubmit('pointeri in C');

      expect(semanticSpy).toHaveBeenCalledWith('pointeri in C');
      const cards = element().querySelectorAll('app-tutor-card');
      expect(cards.length).toBe(2);
      expect(cards[0].querySelector('.best-match-label')).not.toBeNull();
      expect(cards[1].querySelector('.best-match-label')).toBeNull();
      expect(element().querySelector('app-search-mode-badge .mode-semantic')?.textContent).toContain('91% match');
      expect(component.topScore()).toBe(0.91);
    });

    it('Given_SemanticFallback_When_Searching_Then_ShowsKeywordFallbackBadgeWithoutBestMatch', () => {
      createPage(of(response([])));
      semanticSpy.mockReturnValue(of(semanticResult('keyword_fallback', [null, null])));

      component.onSemanticSearch('pointeri');
      fixture.detectChanges();

      expect(element().querySelector('app-search-mode-badge .mode-fallback')).not.toBeNull();
      expect(element().querySelector('.best-match-label')).toBeNull();
      expect(element().querySelector('.match-badge')).toBeNull();
      expect(component.topScore()).toBeNull();
    });

    it('Given_PromptChip_When_Clicked_Then_PopulatesSearchBarAndSearches', () => {
      createPage(of(response([])));
      semanticSpy.mockReturnValue(of(semanticResult('semantic')));
      const chip = element().querySelector('app-prompt-chips .chip') as HTMLButtonElement;

      chip.click();
      fixture.detectChanges();

      const input = element().querySelector('#semantic-search-input') as HTMLInputElement;
      expect(semanticSpy).toHaveBeenCalledWith(chip.textContent?.trim());
      expect(component.semanticQuery()).toBe(chip.textContent?.trim());
      expect(input.value).toBe(chip.textContent?.trim());
    });

    it('Given_BlankQuery_When_SearchingSemantically_Then_DoesNothing', () => {
      createPage(of(response([])));

      component.onSemanticSearch('   ');

      expect(semanticSpy).not.toHaveBeenCalled();
    });

    it('Given_PendingSemanticSearch_When_Rendering_Then_ShowsSkeletonsAndDisablesChips', () => {
      createPage(of(response([])));
      semanticSpy.mockReturnValue(NEVER);

      component.onSemanticSearch('pointeri');
      fixture.detectChanges();

      expect(element().querySelectorAll('.skeleton-card').length).toBe(SEARCH_PAGE_SIZE);
      expect(element().querySelector('app-search-mode-badge')).toBeNull();
      expect((element().querySelector('app-prompt-chips .chip') as HTMLButtonElement).disabled).toBe(true);
    });

    it('Given_SemanticFailure_When_Searching_Then_ShowsErrorAndRetryRepeatsSemanticQuery', () => {
      createPage(of(response([])));
      semanticSpy.mockReturnValue(throwError(() => new Error('bad request')));
      component.onSemanticSearch('pointeri');
      fixture.detectChanges();
      expect(element().querySelector('.alert-danger')).not.toBeNull();

      semanticSpy.mockReturnValue(of(semanticResult('semantic')));
      (element().querySelector('.alert-danger button') as HTMLButtonElement).click();
      fixture.detectChanges();

      expect(semanticSpy).toHaveBeenCalledTimes(2);
      expect(semanticSpy).toHaveBeenLastCalledWith('pointeri');
      expect(element().querySelector('.alert-danger')).toBeNull();
      expect(element().querySelectorAll('app-tutor-card').length).toBe(2);
    });

    it('Given_NoSemanticMatches_When_Rendering_Then_ShowsDescriptionEmptyState', () => {
      createPage(of(response([tutor('1', 'Maria Dragomir')])));
      semanticSpy.mockReturnValue(of(semanticResult('semantic', [])));

      component.onSemanticSearch('ceva obscur');
      fixture.detectChanges();

      expect(element().querySelector('.empty-state h3')?.textContent).toContain('No tutors match your description');
    });

    it('Given_SemanticResults_When_ClickingBackToAllTutors_Then_ReloadsClassicListAndClearsMode', () => {
      createPage(of(response([tutor('1', 'Maria Dragomir')])));
      semanticSpy.mockReturnValue(of(semanticResult('semantic')));
      component.onSemanticSearch('pointeri');
      fixture.detectChanges();
      searchSpy.mockClear();

      (element().querySelector('.mode-row button') as HTMLButtonElement).click();
      fixture.detectChanges();

      expect(searchSpy).toHaveBeenCalledTimes(1);
      expect(component.searchMode()).toBeNull();
      expect(component.semanticQuery()).toBe('');
      expect(element().querySelector('app-search-mode-badge')).toBeNull();
    });

    it('Given_SemanticResults_When_ApplyingFilters_Then_LeavesSemanticMode', () => {
      createPage(of(response([tutor('1', 'Maria Dragomir')])));
      semanticSpy.mockReturnValue(of(semanticResult('semantic')));
      component.onSemanticSearch('pointeri');

      component.onCriteriaChange({ subject: 'SQL', maxPrice: null, date: '', sortBy: 'priceAsc' });

      expect(component.searchMode()).toBeNull();
      expect(component.activeSemanticQuery()).toBeNull();
    });

    it('Given_NewSemanticSearchWhilePreviousIsPending_When_Searching_Then_CancelsPreviousRequest', () => {
      createPage(of(response([])));
      const first = new Subject<SemanticSearchResult>();
      semanticSpy.mockReturnValue(first);
      component.onSemanticSearch('pointeri');

      semanticSpy.mockReturnValue(of(semanticResult('semantic')));
      component.onSemanticSearch('sortare');

      expect(first.observed).toBe(false);
    });
  });

  describe('evaluation dashboard toggle', () => {
    it('Given_Page_When_TogglingEvaluation_Then_ShowsAndHidesDashboard', () => {
      createPage(of(response([])));
      expect(element().querySelector('app-evaluation-dashboard')).toBeNull();
      const toggle = element().querySelector('.header-row button') as HTMLButtonElement;

      toggle.click();
      fixture.detectChanges();
      expect(element().querySelector('app-evaluation-dashboard')).not.toBeNull();
      expect(toggle.getAttribute('aria-pressed')).toBe('true');

      toggle.click();
      fixture.detectChanges();
      expect(element().querySelector('app-evaluation-dashboard')).toBeNull();
    });
  });
});
