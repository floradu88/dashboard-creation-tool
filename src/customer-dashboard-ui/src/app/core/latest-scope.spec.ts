import { LatestScope } from './latest-scope';

describe('LatestScope', () => {
  it('ignores a response issued before the latest customer or filter change', () => {
    const scope = new LatestScope();
    const first = scope.issue();
    const second = scope.issue();
    expect(scope.isCurrent(first)).toBe(false);
    expect(scope.isCurrent(second)).toBe(true);
  });
});
