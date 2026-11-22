import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SubjectService } from '../../../src/app/core/services/subject.service';

describe('SubjectService', () => {
  let service: SubjectService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        SubjectService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(SubjectService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('Given_ValidQuery_When_SuggestingSubjects_Then_IssuesGetRequestWithParam', () => {
    service.suggestSubjects('java').subscribe(suggestions => {
      expect(suggestions).toEqual(['Java', 'JavaScript']);
    });

    const req = httpTesting.expectOne('/api/subjects/suggest?q=java');
    expect(req.request.method).toBe('GET');
    req.flush({ suggestions: ['Java', 'JavaScript'] });
  });

  it('Given_EmptyQuery_When_SuggestingSubjects_Then_ReturnsEmptyArrayWithoutHttpCall', () => {
    let result: string[] | undefined;
    service.suggestSubjects('').subscribe(suggestions => {
      result = suggestions;
    });

    expect(result).toEqual([]);
    httpTesting.expectNone('/api/subjects/suggest');
  });

  it('Given_WhitespaceQuery_When_SuggestingSubjects_Then_ReturnsEmptyArrayWithoutHttpCall', () => {
    let result: string[] | undefined;
    service.suggestSubjects('   ').subscribe(suggestions => {
      result = suggestions;
    });

    expect(result).toEqual([]);
    httpTesting.expectNone('/api/subjects/suggest');
  });
});
