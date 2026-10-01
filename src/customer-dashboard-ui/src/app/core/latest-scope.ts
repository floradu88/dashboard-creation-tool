export class LatestScope {
  private generation = 0;
  issue(): number { return ++this.generation; }
  isCurrent(token: number): boolean { return token === this.generation; }
}
