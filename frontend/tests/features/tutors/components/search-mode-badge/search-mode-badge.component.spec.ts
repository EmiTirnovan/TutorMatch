import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SearchMode } from '../../../../../src/app/core/models/semantic-search.models';
import { SearchModeBadgeComponent } from '../../../../../src/app/features/tutors/components/search-mode-badge/search-mode-badge.component';

describe('SearchModeBadgeComponent', () => {
  let fixture: ComponentFixture<SearchModeBadgeComponent>;

  const render = (mode: SearchMode, topScore: number | null = null): HTMLElement => {
    fixture = TestBed.createComponent(SearchModeBadgeComponent);
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('topScore', topScore);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SearchModeBadgeComponent] }).compileComponents();
  });

  it('Given_SemanticMode_When_Rendering_Then_ShowsSemanticLabelAndClassWithoutMessage', () => {
    const host = render('semantic');
    const badge = host.querySelector('.mode-badge') as HTMLElement;

    expect(badge.textContent).toContain('Semantic Search');
    expect(badge.classList).toContain('mode-semantic');
    expect(badge.classList).not.toContain('mode-fallback');
    expect(host.querySelector('.mode-message')).toBeNull();
  });

  it('Given_KeywordFallbackMode_When_Rendering_Then_ShowsFallbackLabelClassAndCalmMessage', () => {
    const host = render('keyword_fallback');
    const badge = host.querySelector('.mode-badge') as HTMLElement;

    expect(badge.textContent).toContain('Keyword Fallback');
    expect(badge.classList).toContain('mode-fallback');
    expect(badge.classList).not.toContain('mode-semantic');
    expect(host.querySelector('.mode-message')?.textContent).toContain(
      'Căutarea semantică este indisponibilă momentan, afișăm rezultate pe baza cuvintelor cheie'
    );
  });

  it('Given_SemanticModeWithScore_When_Rendering_Then_ShowsRoundedPercentage', () => {
    const host = render('semantic', 0.876);

    expect(host.querySelector('.mode-score')?.textContent).toContain('88% match');
  });

  it('Given_SemanticModeWithoutScore_When_Rendering_Then_HidesScore', () => {
    const host = render('semantic', null);

    expect(host.querySelector('.mode-score')).toBeNull();
  });

  it('Given_KeywordFallbackWithScore_When_Rendering_Then_HidesScore', () => {
    const host = render('keyword_fallback', 0.9);

    expect(host.querySelector('.mode-score')).toBeNull();
  });

  it.each([
    [1.4, 100],
    [-0.2, 0],
    [0, 0]
  ])('Given_OutOfRangeScore_When_Rendering_Then_ClampsToPercentage', (score, expected) => {
    render('semantic', score);

    expect(fixture.componentInstance.scorePercent()).toBe(expected);
  });

  it('Given_ModeChange_When_InputUpdates_Then_SwitchesClassAndText', () => {
    const host = render('semantic');

    fixture.componentRef.setInput('mode', 'keyword_fallback');
    fixture.detectChanges();

    const badge = host.querySelector('.mode-badge') as HTMLElement;
    expect(badge.classList).toContain('mode-fallback');
    expect(badge.textContent).toContain('Keyword Fallback');
  });
});
