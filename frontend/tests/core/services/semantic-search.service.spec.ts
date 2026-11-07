import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import {
  SemanticSearchResponseDto,
  SemanticSearchResult
} from '../../../src/app/core/models/semantic-search.models';
import { TutorSearchResponse } from '../../../src/app/core/models/tutor-search.models';
import {
  KEYWORD_FALLBACK_MESSAGE,
  SEMANTIC_SEARCH_TIMEOUT_MS,
  SemanticSearchService
} from '../../../src/app/core/services/semantic-search.service';

describe('SemanticSearchService', () => {
  let service: SemanticSearchService;
  let httpMock: HttpTestingController;
  let results: SemanticSearchResult[];
  let errors: unknown[];

  const semanticResponse = (overrides: Partial<SemanticSearchResponseDto> = {}): SemanticSearchResponseDto => ({
    mode: 'semantic',
    results: [
      {
        tutorId: 'tutor-1',
        name: 'Alexandru Radu',
        bio: 'Algoritmi',
        hourlyRate: 90,
        similarityScore: 0.92,
        subjects: ['Algoritmi', 'C++']
      },
      {
        tutorId: 'tutor-2',
        name: 'Mihai Enache',
        bio: 'SQL',
        hourlyRate: 80,
        similarityScore: 0.61,
        subjects: ['SQL']
      }
    ],
    ...overrides
  });

  const classicResponse: TutorSearchResponse = {
    tutors: [{ id: 'tutor-9', name: 'Maria Ionescu', hourlyRate: 70, averageRating: 4.5, ratingCount: 3, subjects: ['Engleza'] }],
    totalCount: 1,
    page: 1,
    pageSize: 5,
    mode: 'keyword'
  };

  const semanticRequest = (): TestRequest =>
    httpMock.expectOne((request) => request.url === '/api/tutors/semantic-search');
  const classicRequest = (): TestRequest => httpMock.expectOne((request) => request.url === '/api/tutors/search');

  const run = (query: string): void => {
    service.search(query).subscribe({
      next: (result) => results.push(result),
      error: (error: unknown) => errors.push(error)
    });
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(SemanticSearchService);
    httpMock = TestBed.inject(HttpTestingController);
    results = [];
    errors = [];
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  it('Given_Query_When_Searching_Then_SendsTrimmedQueryToSemanticEndpoint', () => {
    run('  pointeri in C  ');

    const request = semanticRequest();
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('q')).toBe('pointeri in C');
    request.flush(semanticResponse());
  });

  it('Given_SemanticResponse_When_Searching_Then_MapsTutorsWithSimilarityAndSemanticMode', () => {
    run('algoritmi');

    semanticRequest().flush(semanticResponse());

    expect(results).toEqual([
      {
        mode: 'semantic',
        message: null,
        tutors: [
          {
            id: 'tutor-1',
            name: 'Alexandru Radu',
            hourlyRate: 90,
            averageRating: 0,
            ratingCount: 0,
            subjects: ['Algoritmi', 'C++'],
            similarityScore: 0.92
          },
          {
            id: 'tutor-2',
            name: 'Mihai Enache',
            hourlyRate: 80,
            averageRating: 0,
            ratingCount: 0,
            subjects: ['SQL'],
            similarityScore: 0.61
          }
        ]
      }
    ]);
  });

  it('Given_BackendKeywordMode_When_Searching_Then_MapsToKeywordFallbackWithoutScores', () => {
    run('algoritmi');

    semanticRequest().flush(semanticResponse({ mode: 'keyword', message: 'fallback aplicat' }));

    expect(results[0].mode).toBe('keyword_fallback');
    expect(results[0].message).toBe('fallback aplicat');
    expect(results[0].tutors.map((tutor) => tutor.similarityScore)).toEqual([null, null]);
  });

  it('Given_EmptySemanticResults_When_Searching_Then_ReturnsEmptySemanticResult', () => {
    run('ceva obscur');

    semanticRequest().flush(semanticResponse({ results: [] }));

    expect(results).toEqual([{ mode: 'semantic', tutors: [], message: null }]);
  });

  it('Given_BlankQuery_When_Searching_Then_ReturnsEmptyResultWithoutCallingApi', () => {
    run('   ');

    expect(results).toEqual([{ mode: 'semantic', tutors: [], message: null }]);
    httpMock.expectNone(() => true);
  });

  it('Given_ServerError_When_Searching_Then_FallsBackToClassicSearchSilently', () => {
    run('pointeri');

    semanticRequest().flush('boom', { status: 500, statusText: 'Server Error' });
    const fallback = classicRequest();
    expect(fallback.request.params.get('subject')).toBe('pointeri');
    expect(fallback.request.params.get('page')).toBe('1');
    expect(fallback.request.params.get('pageSize')).toBe('5');
    fallback.flush(classicResponse);

    expect(errors).toEqual([]);
    expect(results).toEqual([
      {
        mode: 'keyword_fallback',
        message: KEYWORD_FALLBACK_MESSAGE,
        tutors: [{ ...classicResponse.tutors[0], similarityScore: null }]
      }
    ]);
  });

  it('Given_ServiceUnavailable_When_Searching_Then_FallsBackToClassicSearch', () => {
    run('pointeri');

    semanticRequest().flush('', { status: 503, statusText: 'Service Unavailable' });
    classicRequest().flush(classicResponse);

    expect(results[0].mode).toBe('keyword_fallback');
  });

  it('Given_NetworkError_When_Searching_Then_FallsBackToClassicSearch', () => {
    run('pointeri');

    semanticRequest().error(new ProgressEvent('error'));
    classicRequest().flush(classicResponse);

    expect(results[0].mode).toBe('keyword_fallback');
    expect(errors).toEqual([]);
  });

  it('Given_SlowOllama_When_TimeoutElapses_Then_CancelsRequestAndFallsBack', () => {
    vi.useFakeTimers();
    run('pointeri');
    const pending = semanticRequest();

    vi.advanceTimersByTime(SEMANTIC_SEARCH_TIMEOUT_MS);

    expect(pending.cancelled).toBe(true);
    classicRequest().flush(classicResponse);
    expect(results[0].mode).toBe('keyword_fallback');
    expect(errors).toEqual([]);
  });

  it('Given_ResponseBeforeTimeout_When_TimeoutWouldElapse_Then_DoesNotFallBack', () => {
    vi.useFakeTimers();
    run('pointeri');

    semanticRequest().flush(semanticResponse());
    vi.advanceTimersByTime(SEMANTIC_SEARCH_TIMEOUT_MS * 2);

    expect(results.map((result) => result.mode)).toEqual(['semantic']);
  });

  it('Given_ClientError_When_Searching_Then_PropagatesErrorWithoutFallback', () => {
    run('pointeri');

    semanticRequest().flush('bad', { status: 400, statusText: 'Bad Request' });

    httpMock.expectNone((request) => request.url === '/api/tutors/search');
    expect(results).toEqual([]);
    expect(errors.length).toBe(1);
  });

  it('Given_ServerErrorAndFailingFallback_When_Searching_Then_PropagatesFallbackError', () => {
    run('pointeri');

    semanticRequest().flush('', { status: 500, statusText: 'Server Error' });
    classicRequest().flush('', { status: 500, statusText: 'Server Error' });

    expect(results).toEqual([]);
    expect(errors.length).toBe(1);
  });
});
