import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { SearchTutorsComponent, SEARCH_PAGE_SIZE } from './search-tutors.component';
import { TutorService } from '../../../../core/services/tutor.service';
import { TutorSearchFilters, TutorSearchItem, TutorSearchResponse } from '../../../../core/models/tutor-search.models';

describe('SearchTutorsComponent', () => {
  let component: SearchTutorsComponent;
  let fixture: ComponentFixture<SearchTutorsComponent>;
  let searchSpy: ReturnType<typeof vi.fn<(filters: TutorSearchFilters) => Observable<TutorSearchResponse>>>;

  const tutor = (id: string, name: string): TutorSearchItem => ({
    id,
    name,
    hourlyRate: 70,
    averageRating: 4.5,
    ratingCount: 2
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
    TestBed.configureTestingModule({
      imports: [SearchTutorsComponent],
      providers: [provideRouter([]), { provide: TutorService, useValue: { searchTutors: searchSpy } }]
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
});
