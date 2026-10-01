import { DestroyRef, Signal, WritableSignal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, map, Observable, of, switchMap } from 'rxjs';
import { LatestScope } from './latest-scope';

export const loadError = 'We could not load this view. Check the API, customer and reporting dates, then retry.';

export function watchTab<T>(revision: Signal<string>, destroyRef: DestroyRef, state: { data: WritableSignal<T | null>; loading: WritableSignal<boolean>; error: WritableSignal<string> }, load: () => Observable<T>) {
  const scope = new LatestScope();
  toObservable(revision).pipe(
    switchMap(() => {
      const token = scope.issue();
      const [customerId] = revision().split('|');
      if (!customerId) {
        state.loading.set(true);
        return EMPTY;
      }
      if (state.data() === null) state.loading.set(true);
      state.error.set('');
      return load().pipe(
        map(value => ({ token, value, error: '' })),
        catchError(() => of({ token, value: null as T | null, error: loadError })),
      );
    }),
    takeUntilDestroyed(destroyRef),
  ).subscribe(result => {
    if (!scope.isCurrent(result.token)) return;
    if (result.error) state.error.set(result.error);
    else { state.data.set(result.value); state.error.set(''); }
    state.loading.set(false);
  });
}
