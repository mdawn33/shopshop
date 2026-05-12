import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { Loading } from "../services/loading";
import { finalize } from "rxjs";

export const loadingInterceptor : HttpInterceptorFn = (req, next) => {
  
    const loading = inject(Loading);
    loading.increment();

    return next(req).pipe(
        finalize(() => loading.decrement())
    );
};