import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from '../src/app/app';
import { routes } from '../src/app/app.routes';
import { AuthService } from '../src/app/core/services/auth.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render brand logo text', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.logo-text')?.textContent).toContain('TutorMatch');
  });

  it('Given_Navigation_When_Rendering_Then_ShowsDiscoverTutorsLinkToSearchPage', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('nav.nav-links a'));
    const discover = links.find((link) => link.textContent?.includes('Discover tutors'));

    expect(discover?.getAttribute('href')).toBe('/tutors/search');
    expect(links.map((link) => link.textContent?.trim())).toContain('Become a Tutor');
    expect(links.map((link) => link.textContent?.trim())).toContain('My Sessions');
  });
});

describe('App navigation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('Given_SearchRoute_When_Navigating_Then_RendersSearchPageAndMarksNavLinkActive', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    await TestBed.inject(Router).navigateByUrl('/tutors/search');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const discover = Array.from(compiled.querySelectorAll<HTMLAnchorElement>('nav.nav-links a')).find((link) =>
      link.textContent?.includes('Discover tutors')
    );
    expect(compiled.querySelector('app-search-tutors')).not.toBeNull();
    expect(discover?.classList.contains('active')).toBe(true);
  });

  it('Given_SessionsRoute_When_Navigating_Then_RendersMySessionsPageAndMarksNavLinkActive', async () => {
    const authService = TestBed.inject(AuthService);
    authService.handleAuthSuccess({
      token: 'fake-jwt',
      userId: 'user-123',
      email: 'alex@example.com',
      fullName: 'Alex Ionescu',
      role: 'student'
    });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    await TestBed.inject(Router).navigateByUrl('/sessions');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const mySessionsLink = Array.from(compiled.querySelectorAll<HTMLAnchorElement>('nav.nav-links a')).find((link) =>
      link.textContent?.includes('My Sessions')
    );
    expect(compiled.querySelector('app-my-sessions')).not.toBeNull();
    expect(mySessionsLink?.classList.contains('active')).toBe(true);
  });

  it('Given_OtherRoute_When_Navigating_Then_DiscoverLinkIsNotActive', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    await TestBed.inject(Router).navigateByUrl('/tutors/register');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const discover = Array.from(compiled.querySelectorAll<HTMLAnchorElement>('nav.nav-links a')).find((link) =>
      link.textContent?.includes('Discover tutors')
    );
    expect(compiled.querySelector('app-search-tutors')).toBeNull();
    expect(discover?.classList.contains('active')).toBe(false);
  });

  it('Given_UnauthenticatedUser_When_RenderingHeader_Then_ShowsLoginAndRegisterButtons', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[data-testid="header-login-btn"]')?.textContent).toContain('Log In');
    expect(compiled.querySelector('[data-testid="header-register-btn"]')?.textContent).toContain('Register');
    expect(compiled.querySelector('[data-testid="header-logout-btn"]')).toBeNull();
  });

  it('Given_AuthenticatedUser_When_RenderingHeader_Then_ShowsUserIdentityAndLogoutButton', () => {
    const authService = TestBed.inject(AuthService);
    authService.handleAuthSuccess({
      token: 'fake-token',
      userId: 'user-123',
      email: 'alex@example.com',
      fullName: 'Alex Ionescu',
      role: 'student'
    });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[data-testid="user-name-display"]')?.textContent).toContain('Alex Ionescu');
    expect(compiled.querySelector('[data-testid="user-role-label"]')?.textContent).toContain('Student');
    expect(compiled.querySelector('[data-testid="header-logout-btn"]')?.textContent).toContain('Log Out');
    expect(compiled.querySelector('[data-testid="header-login-btn"]')).toBeNull();
  });
});
