import { HttpInterceptorFn } from '@angular/common/http';

// Placeholder for auth — the API is read-only in Phase 1 with no auth yet.
// When auth lands, attach the token header here.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // const token = ...; req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(req);
};
