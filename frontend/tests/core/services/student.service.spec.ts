import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { StudentService } from '../../../src/app/core/services/student.service';

describe('StudentService', () => {
  let service: StudentService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        StudentService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(StudentService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('Given_StudentId_When_GettingStudentById_Then_IssuesGetRequest', () => {
    const studentId = '11111111-1111-1111-1111-111111111111';
    service.getStudentById(studentId).subscribe(student => {
      expect(student.name).toBe('Alex Radu');
      expect(student.email).toBe('alex@student.upt.ro');
    });

    const req = httpTesting.expectOne(`/api/students/${studentId}`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: studentId, name: 'Alex Radu', email: 'alex@student.upt.ro' });
  });

  it('Given_ValidPayload_When_UpdatingStudentProfile_Then_IssuesPutRequest', () => {
    const studentId = '11111111-1111-1111-1111-111111111111';
    service.updateStudentProfile(studentId, { name: 'Alex Radu Updated' }).subscribe();

    const req = httpTesting.expectOne(`/api/students/${studentId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'Alex Radu Updated' });
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
