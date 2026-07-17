import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

// Global error handling so components stay clean (CLAUDE.md §5).
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error) => {
      // Central place to log / surface errors (extend with a toast service later).
      console.error(`[API error] ${req.method} ${req.url}`, error);
      return throwError(() => error);
    })
  );
};
