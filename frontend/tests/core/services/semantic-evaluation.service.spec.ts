import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { EVALUATION_CASES } from '../../../src/app/core/data/evaluation-cases';
import {
  EvaluationCase,
  EvaluationCaseResult,
  SemanticSearchResult
} from '../../../src/app/core/models/semantic-search.models';
import { TutorSearchFilters, TutorSearchItem, TutorSearchResponse } from '../../../src/app/core/models/tutor-search.models';
import {
  SemanticEvaluationService,
  summarizeEvaluation
} from '../../../src/app/core/services/semantic-evaluation.service';
import { SemanticSearchService } from '../../../src/app/core/services/semantic-search.service';
import { TutorService } from '../../../src/app/core/services/tutor.service';

const tutor = (id: string, similarityScore: number | null = null): TutorSearchItem => ({
  id,
  name: `Tutor ${id}`,
  hourlyRate: 80,
  averageRating: 0,
  ratingCount: 0,
  subjects: [],
  similarityScore
});

const SCORES = [0.9, 0.8, 0.7, 0.6];

const semantic = (ids: string[], mode: SemanticSearchResult['mode'] = 'semantic'): SemanticSearchResult => ({
  mode,
  message: null,
  tutors: ids.map((id, index) => tutor(id, mode === 'semantic' ? SCORES[index] : null))
});

const classic = (ids: string[]): TutorSearchResponse => ({
  tutors: ids.map((id) => tutor(id)),
  totalCount: ids.length,
  page: 1,
  pageSize: 3,
  mode: 'keyword'
});

const testCase = (query: string, expectedTutorId: string): EvaluationCase => ({
  query,
  expectedTutorId,
  expectedTutorName: `Tutor ${expectedTutorId}`,
  category: 'Test'
});

const result = (overrides: Partial<EvaluationCaseResult> = {}): EvaluationCaseResult => ({
  testCase: testCase('q', 'a'),
  status: 'miss',
  semanticHit: false,
  keywordHit: false,
  matchScore: null,
  ...overrides
});

describe('SemanticEvaluationService', () => {
  let service: SemanticEvaluationService;
  let searchSpy: ReturnType<typeof vi.fn<(query: string) => Observable<SemanticSearchResult>>>;
  let keywordSpy: ReturnType<typeof vi.fn<(filters: TutorSearchFilters) => Observable<TutorSearchResponse>>>;

  const evaluate = (cases: EvaluationCase[]): { results: EvaluationCaseResult[]; completed: boolean } => {
    const output = { results: [] as EvaluationCaseResult[], completed: false };
    service.evaluate(cases).subscribe({
      next: (item) => output.results.push(item),
      complete: () => (output.completed = true)
    });
    return output;
  };

  beforeEach(() => {
    searchSpy = vi.fn<(query: string) => Observable<SemanticSearchResult>>();
    keywordSpy = vi.fn<(filters: TutorSearchFilters) => Observable<TutorSearchResponse>>().mockReturnValue(of(classic([])));
    TestBed.configureTestingModule({
      providers: [
        { provide: SemanticSearchService, useValue: { search: searchSpy } },
        { provide: TutorService, useValue: { searchTutors: keywordSpy } }
      ]
    });
    service = TestBed.inject(SemanticEvaluationService);
  });

  it('Given_ExpectedTutorInTopThree_When_Evaluating_Then_ReportsHitWithItsScore', () => {
    searchSpy.mockReturnValue(of(semantic(['x', 'y', 'a'])));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results).toEqual([
      expect.objectContaining({ status: 'hit', semanticHit: true, keywordHit: false, matchScore: 0.7 })
    ]);
  });

  it('Given_ExpectedTutorAtFourthPosition_When_Evaluating_Then_ReportsMiss', () => {
    searchSpy.mockReturnValue(of(semantic(['x', 'y', 'z', 'a'])));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results[0]).toEqual(expect.objectContaining({ status: 'miss', semanticHit: false, matchScore: null }));
  });

  it('Given_KeywordBaselineFindsTutor_When_Evaluating_Then_MarksKeywordHit', () => {
    searchSpy.mockReturnValue(of(semantic(['x'])));
    keywordSpy.mockReturnValue(of(classic(['x', 'a'])));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results[0].keywordHit).toBe(true);
    expect(results[0].semanticHit).toBe(false);
  });

  it('Given_KeywordBaselineReturnsMoreThanThree_When_Evaluating_Then_OnlyTopThreeCount', () => {
    searchSpy.mockReturnValue(of(semantic([])));
    keywordSpy.mockReturnValue(of(classic(['x', 'y', 'z', 'a'])));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results[0].keywordHit).toBe(false);
  });

  it('Given_Query_When_Evaluating_Then_BaselineUsesQueryAsSubjectWithPageSizeThree', () => {
    searchSpy.mockReturnValue(of(semantic([])));

    evaluate([testCase('pointeri in C', 'a')]);

    expect(searchSpy).toHaveBeenCalledWith('pointeri in C');
    expect(keywordSpy).toHaveBeenCalledWith({
      subject: 'pointeri in C',
      maxPrice: null,
      date: '',
      sortBy: 'priceAsc',
      page: 1,
      pageSize: 3
    });
  });

  it('Given_SemanticFallback_When_Evaluating_Then_ReportsFallbackAndNeverCountsSemanticHit', () => {
    searchSpy.mockReturnValue(of(semantic(['a'], 'keyword_fallback')));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results[0]).toEqual(expect.objectContaining({ status: 'fallback', semanticHit: false, matchScore: null }));
  });

  it('Given_SemanticFailure_When_Evaluating_Then_ReportsErrorAndContinuesWithNextCase', () => {
    searchSpy.mockReturnValueOnce(throwError(() => new Error('boom'))).mockReturnValueOnce(of(semantic(['b'])));

    const { results, completed } = evaluate([testCase('first', 'a'), testCase('second', 'b')]);

    expect(results.map((item) => item.status)).toEqual(['error', 'hit']);
    expect(completed).toBe(true);
  });

  it('Given_KeywordBaselineFailure_When_Evaluating_Then_StillEvaluatesSemanticSearch', () => {
    searchSpy.mockReturnValue(of(semantic(['a'])));
    keywordSpy.mockReturnValue(throwError(() => new Error('boom')));

    const { results } = evaluate([testCase('q', 'a')]);

    expect(results[0]).toEqual(expect.objectContaining({ status: 'hit', keywordHit: false }));
  });

  it('Given_MultipleCases_When_Evaluating_Then_EmitsResultsInOrderAndCompletes', () => {
    searchSpy.mockImplementation((query) => of(semantic([query])));

    const { results, completed } = evaluate([testCase('a', 'a'), testCase('b', 'b'), testCase('c', 'x')]);

    expect(results.map((item) => item.status)).toEqual(['hit', 'hit', 'miss']);
    expect(completed).toBe(true);
  });

  it('Given_NoCases_When_Evaluating_Then_CompletesWithoutResults', () => {
    const { results, completed } = evaluate([]);

    expect(results).toEqual([]);
    expect(completed).toBe(true);
  });
});

describe('summarizeEvaluation', () => {
  it('Given_NoResults_When_Summarizing_Then_ReturnsZeros', () => {
    expect(summarizeEvaluation([])).toEqual({
      total: 0,
      evaluated: 0,
      semanticHits: 0,
      keywordHits: 0,
      fallbackCount: 0,
      errorCount: 0,
      semanticAccuracy: 0,
      keywordAccuracy: 0,
      improvement: 0
    });
  });

  it('Given_MixedResults_When_Summarizing_Then_ComputesTopThreeAccuracyForBothModes', () => {
    const summary = summarizeEvaluation([
      result({ status: 'hit', semanticHit: true, keywordHit: true }),
      result({ status: 'hit', semanticHit: true }),
      result({ status: 'hit', semanticHit: true }),
      result({ status: 'miss', keywordHit: false })
    ]);

    expect(summary.semanticAccuracy).toBe(75);
    expect(summary.keywordAccuracy).toBe(25);
    expect(summary.improvement).toBe(50);
    expect(summary.semanticHits).toBe(3);
    expect(summary.keywordHits).toBe(1);
  });

  it('Given_ErroredCases_When_Summarizing_Then_ExcludesThemFromDenominator', () => {
    const summary = summarizeEvaluation([
      result({ status: 'hit', semanticHit: true }),
      result({ status: 'error', keywordHit: true }),
      result({ status: 'miss' })
    ]);

    expect(summary).toEqual(
      expect.objectContaining({ total: 3, evaluated: 2, errorCount: 1, semanticAccuracy: 50, keywordAccuracy: 0, keywordHits: 0 })
    );
  });

  it('Given_FallbackCases_When_Summarizing_Then_CountsThemAsSemanticMisses', () => {
    const summary = summarizeEvaluation([
      result({ status: 'hit', semanticHit: true }),
      result({ status: 'fallback', keywordHit: true })
    ]);

    expect(summary).toEqual(
      expect.objectContaining({ fallbackCount: 1, semanticAccuracy: 50, keywordAccuracy: 50, improvement: 0 })
    );
  });

  it('Given_ThirdsOfCases_When_Summarizing_Then_RoundsToOneDecimal', () => {
    const summary = summarizeEvaluation([
      result({ status: 'hit', semanticHit: true }),
      result({ status: 'miss' }),
      result({ status: 'miss' })
    ]);

    expect(summary.semanticAccuracy).toBe(33.3);
    expect(summary.improvement).toBe(33.3);
  });

  it('Given_BaselineBeatsSemantic_When_Summarizing_Then_ImprovementIsNegative', () => {
    const summary = summarizeEvaluation([result({ status: 'miss', keywordHit: true })]);

    expect(summary.improvement).toBe(-100);
  });
});

describe('EVALUATION_CASES', () => {
  it('Given_EvaluationSet_When_Inspecting_Then_HasBetweenTwentyAndThirtyCases', () => {
    expect(EVALUATION_CASES.length).toBeGreaterThanOrEqual(20);
    expect(EVALUATION_CASES.length).toBeLessThanOrEqual(30);
  });

  it('Given_EvaluationSet_When_Inspecting_Then_QueriesAreNonEmptyAndUnique', () => {
    const queries = EVALUATION_CASES.map((item) => item.query.trim().toLowerCase());

    expect(queries.every((query) => query.length > 0)).toBe(true);
    expect(new Set(queries).size).toBe(queries.length);
  });

  it('Given_EvaluationSet_When_Inspecting_Then_EveryCaseHasExpectedTutorAndCategory', () => {
    expect(EVALUATION_CASES.every((item) => item.expectedTutorId && item.expectedTutorName && item.category)).toBe(true);
  });
});
