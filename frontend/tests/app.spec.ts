import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from '../src/app/app';
import { routes } from '../src/app/app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
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

  it('Given_Header_When_Rendering_Then_ShowsAdminTestModeBadge', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.admin-badge')?.textContent).toContain('Admin / Test Mode');
  });
});
