import { fallbackQueries, splitAddress } from '../../../src/app/core/utils/address-parts';

describe('splitAddress', () => {
  it('Given_FullApartmentAddress_When_Splitting_Then_MovesUnitPartsAndKeepsBlock', () => {
    expect(splitAddress('Str. Pacurari 5, bl. A2, sc. B, et. 4, ap. 18, Iasi')).toEqual({
      query: 'Str. Pacurari 5, bl. A2, Iasi',
      details: 'sc. B, et. 4, ap. 18'
    });
  });

  it('Given_SpelledOutWordsAndParter_When_Splitting_Then_RecognisesThem', () => {
    expect(splitAddress('Bulevardul Carol I 11 etaj parter apartament 3 interfon 12')).toEqual({
      query: 'Bulevardul Carol I 11',
      details: 'etaj parter, apartament 3, interfon 12'
    });
  });

  it('Given_UniversityRoom_When_Splitting_Then_MovesSala', () => {
    expect(splitAddress('Bulevardul Carol I 11, sala C309')).toEqual({
      query: 'Bulevardul Carol I 11',
      details: 'sala C309'
    });
  });

  it('Given_StreetNamesStartingLikeUnits_When_Splitting_Then_LeavesThemAlone', () => {
    expect(splitAddress('Strada Scolii 4, Sala Palatului, Strada Etajului 2')).toEqual({
      query: 'Strada Scolii 4, Sala Palatului, Strada Etajului 2',
      details: ''
    });
  });
});

describe('fallbackQueries', () => {
  it('Given_AddressWithBlockAndNumber_When_Building_Then_TriesExactThenWithoutBlockThenStreet', () => {
    expect(fallbackQueries('Str. Pacurari 5, bl. A2, Iasi')).toEqual([
      { query: 'Str. Pacurari 5, bl. A2, Iasi', streetOnly: false },
      { query: 'Str. Pacurari 5, Iasi', streetOnly: false },
      { query: 'Str. Pacurari, Iasi', streetOnly: true }
    ]);
  });

  it('Given_NrPrefix_When_Building_Then_StreetQueryDropsIt', () => {
    expect(fallbackQueries('Strada Lapusneanu nr. 12, Iasi').at(-1)).toEqual({
      query: 'Strada Lapusneanu, Iasi',
      streetOnly: true
    });
  });

  it('Given_StreetWithoutNumberOrBlock_When_Building_Then_TriesOnce', () => {
    expect(fallbackQueries('Palas Mall, Iasi')).toEqual([{ query: 'Palas Mall, Iasi', streetOnly: false }]);
  });

  it('Given_TooShortQuery_When_Building_Then_ReturnsNothing', () => {
    expect(fallbackQueries('ab')).toEqual([]);
  });
});
