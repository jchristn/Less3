import { darkTheme, primaryTheme } from '#/theme/theme';

describe('Less3 theme', () => {
  it('keeps toast notifications at normal UI scale', () => {
    expect(primaryTheme.components?.Message?.fontSize).toBe(14);
    expect(darkTheme.components?.Message?.fontSize).toBe(14);
  });
});
