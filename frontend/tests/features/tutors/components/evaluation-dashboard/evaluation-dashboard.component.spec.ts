import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { EVALUATION_CASES } from '../../../../../src/app/core/data/evaluation-cases';
import { EvaluationCase, SemanticSearchResponseDto } from '../../../../../src/app/core/models/semantic-search.models';
import { TutorSearchResponse } from '../../../../../src/app/core/models/tutor-search.models';
import { EvaluationDashboardComponent } from '../../../../../src/app/features/tutors/components/evaluation-dashboard/evaluation-dashboard.component';

describe('EvaluationDashboardComponent', () => {
  let fixture: ComponentFixture<EvaluationDashboardComponent>;
  let component: EvaluationDashboardComponent;
  let httpMock: HttpTestingController;

  const cases: EvaluationCase[] = [
    { query: 'pointeri in C', expectedTutorId: 'tutor-a', expectedTutorName: 'Alexandru Radu', category: 'Informatica' },
    { query: 'optimizare SQL', expectedTutorId: 'tutor-b', expectedTutorName: 'Mihai Enache', category: 'Baze de Date' }
  ];

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const text = (testId: string): string =>
    element().querySelector(`[data-testid="${testId}"]`)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
  const runButton = (): HTMLButtonElement => element().querySelector('.run-button') as HTMLButtonElement;
  const rows = (): HTMLTableRowElement[] => Array.from(element().querySelectorAll<HTMLTableRowElement>('tbody tr'));

  const semanticRequest = (): TestRequest =>
    httpMock.expectOne((request) => request.url === '/api/tutors/semantic-search');
  const classicRequests = (): TestRequest[] => httpMock.match((request) => request.url === '/api/tutors/search');

  const semanticDto = (...tutorIds: string[]): SemanticSearchResponseDto => ({
    mode: 'semantic',
    results: tutorIds.map((tutorId, index) => ({
      tutorId,
      name: tutorId,
      bio: '',
      hourlyRate: 80,
      similarityScore: 0.9 - index * 0.25,
      subjects: []
    }))
  });

  const classicDto = (...tutorIds: string[]): TutorSearchResponse => ({
    tutors: tutorIds.map((id) => ({ id, name: id, hourlyRate: 80, averageRating: 0, ratingCount: 0, subjects: [] })),
    totalCount: tutorIds.length,
    page: 1,
    pageSize: 3,
    mode: 'keyword'
  });

  const flushClassic = (dto: TutorSearchResponse): void => classicRequests().forEach((request) => request.flush(dto));

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EvaluationDashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    fixture = TestBed.createComponent(EvaluationDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('cases', cases);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('Given_NewDashboard_When_Rendering_Then_IsIdleWithRunButtonAndNoMetrics', () => {
    expect(component.state()).toBe('idle');
    expect(runButton().textContent).toContain('Run evaluation');
    expect(runButton().disabled).toBe(false);
    expect(element().querySelector('.metrics')).toBeNull();
    expect(element().textContent).toContain('2 test queries');
  });

  it('Given_DefaultInput_When_Created_Then_UsesBuiltInEvaluationSet', () => {
    const defaultFixture = TestBed.createComponent(EvaluationDashboardComponent);

    expect(defaultFixture.componentInstance.cases()).toBe(EVALUATION_CASES);
  });

  it('Given_Idle_When_ClickingRun_Then_TransitionsToLoadingAndDisablesButton', () => {
    runButton().click();
    fixture.detectChanges();

    expect(component.state()).toBe('loading');
    expect(runButton().disabled).toBe(true);
    expect(element().textContent).toContain('Evaluating... 0 / 2');
    semanticRequest().flush(semanticDto());
    classicRequests().forEach((request) => request.flush(classicDto()));
    semanticRequest().flush(semanticDto());
    flushClassic(classicDto());
  });

  it('Given_Loading_When_RunIsCalledAgain_Then_DoesNotStartSecondEvaluation', () => {
    component.run();
    component.run();

    const requests = httpMock.match((request) => request.url === '/api/tutors/semantic-search');
    expect(requests.length).toBe(1);
    requests[0].flush(semanticDto());
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto());
    flushClassic(classicDto());
  });

  it('Given_AllQueriesAnswered_When_Finished_Then_ShowsSuccessWithTopThreeAccuracyForBothModes', () => {
    runButton().click();
    semanticRequest().flush(semanticDto('tutor-x', 'tutor-y', 'tutor-a'));
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto('tutor-x', 'tutor-y'));
    flushClassic(classicDto('tutor-b'));
    fixture.detectChanges();

    expect(component.state()).toBe('success');
    expect(text('semantic-accuracy')).toBe('50%');
    expect(text('keyword-accuracy')).toBe('50%');
    expect(text('improvement')).toBe('0 pts');
    expect(rows().length).toBe(2);
    expect(runButton().textContent).toContain('Run again');
    expect(runButton().disabled).toBe(false);
  });

  it('Given_SemanticOutperformsBaseline_When_Finished_Then_ShowsPositiveDifference', () => {
    runButton().click();
    semanticRequest().flush(semanticDto('tutor-a'));
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto('tutor-b'));
    flushClassic(classicDto());
    fixture.detectChanges();

    expect(text('semantic-accuracy')).toBe('100%');
    expect(text('keyword-accuracy')).toBe('0%');
    expect(text('improvement')).toBe('+100 pts');
  });

  it('Given_EvaluatedQueries_When_Rendering_Then_ListsQueryExpectedTutorScoreAndStatus', () => {
    runButton().click();
    semanticRequest().flush(semanticDto('tutor-a'));
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto('tutor-x'));
    flushClassic(classicDto());
    fixture.detectChanges();

    const [hitRow, missRow] = rows();
    expect(hitRow.textContent).toContain('pointeri in C');
    expect(hitRow.textContent).toContain('Alexandru Radu');
    expect(hitRow.textContent).toContain('90%');
    expect(hitRow.querySelector('.badge-success')?.textContent).toContain('Top-3 hit');
    expect(missRow.textContent).toContain('—');
    expect(missRow.querySelector('.badge-neutral')?.textContent).toContain('Miss');
  });

  it('Given_ServerErrorsOnSemanticEndpoint_When_Finished_Then_ShowsFallbackStatusAndWarning', () => {
    runButton().click();
    semanticRequest().flush('', { status: 500, statusText: 'Server Error' });
    flushClassic(classicDto('tutor-a'));
    semanticRequest().flush(semanticDto('tutor-b'));
    flushClassic(classicDto());
    fixture.detectChanges();

    expect(component.state()).toBe('success');
    expect(rows()[0].querySelector('.badge-warning')?.textContent).toContain('Keyword fallback');
    expect(element().querySelector('.alert-warning')?.textContent).toContain('unavailable for 1 of 2 queries');
    expect(text('semantic-accuracy')).toBe('50%');
    expect(text('keyword-accuracy')).toBe('50%');
  });

  it('Given_EveryQueryFails_When_Finished_Then_TransitionsToErrorState', () => {
    runButton().click();
    semanticRequest().flush('bad', { status: 400, statusText: 'Bad Request' });
    semanticRequest().flush('bad', { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(component.state()).toBe('error');
    expect(element().querySelector('.alert-danger')?.textContent).toContain('could not be completed');
    expect(element().querySelector('.metrics')).toBeNull();
    expect(runButton().disabled).toBe(false);
  });

  it('Given_SomeQueriesFail_When_Finished_Then_ShowsSuccessAndMarksFailedRowsAsError', () => {
    runButton().click();
    semanticRequest().flush('bad', { status: 400, statusText: 'Bad Request' });
    semanticRequest().flush(semanticDto('tutor-b'));
    flushClassic(classicDto());
    fixture.detectChanges();

    expect(component.state()).toBe('success');
    expect(rows()[0].querySelector('.badge-error')?.textContent).toContain('Error');
    expect(text('semantic-accuracy')).toBe('100%');
    expect(component.summary().errorCount).toBe(1);
  });

  it('Given_NoCases_When_Running_Then_TransitionsToError', () => {
    fixture.componentRef.setInput('cases', []);

    component.run();

    expect(component.state()).toBe('error');
  });

  it('Given_FinishedRun_When_RunningAgain_Then_ResetsResultsAndEvaluatesAgain', () => {
    component.run();
    semanticRequest().flush(semanticDto('tutor-a'));
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto('tutor-b'));
    flushClassic(classicDto());
    expect(component.results().length).toBe(2);

    component.run();

    expect(component.state()).toBe('loading');
    expect(component.results()).toEqual([]);
    semanticRequest().flush(semanticDto('tutor-a'));
    flushClassic(classicDto());
    semanticRequest().flush(semanticDto());
    flushClassic(classicDto());
    expect(component.state()).toBe('success');
  });

  it('Given_RunInProgress_When_ComponentIsDestroyed_Then_CancelsPendingRequests', () => {
    component.run();
    const pending = semanticRequest();

    fixture.destroy();

    expect(pending.cancelled).toBe(true);
    classicRequests();
  });

  it.each([
    ['hit', 'Top-3 hit'],
    ['miss', 'Miss'],
    ['fallback', 'Keyword fallback'],
    ['error', 'Error']
  ] as const)('Given_Status_When_Labelling_Then_ReturnsReadableText', (status, label) => {
    expect(component.statusLabel(status)).toBe(label);
  });

  it.each([
    [null, '—'],
    [0.876, '88%'],
    [0, '0%']
  ])('Given_Score_When_Formatting_Then_ReturnsPercentageOrDash', (score, expected) => {
    expect(component.formatScore(score)).toBe(expected);
  });
});
