import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { catchError, throwError } from "rxjs";
import { AppHttpError } from "../errors/app-http-error";

export const errorInterceptor : HttpInterceptorFn = (req, next) => {
    return next(req).pipe(
        catchError((error: unknown) => {
            console.error("error caught in interceptor: ", error);
            if(error instanceof HttpErrorResponse) {
                const appError = new AppHttpError(error.status, error.message, error.error);
                console.error("HttpErrorResponse in interceptor: ", appError);

                // Is this a good practice?
                return throwError(() => appError);
            }

            return throwError(() => error);
        })
    );
};