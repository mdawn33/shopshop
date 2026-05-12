import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { catchError, EMPTY, throwError } from "rxjs";
import { AppHttpError } from "../errors/app-http-error";
import { inject } from "@angular/core";
import { Auth } from "../services/auth";

// This interceptor is passed through in the request's way out but catches any error in the way back in
// Even when rethrowing an error, the interceptors chain is not stopped, it goes till the end
export const errorInterceptor : HttpInterceptorFn = (req, next) => {
    return next(req).pipe(
        catchError((error: unknown) => {
            if(error instanceof HttpErrorResponse) {

                if(error.status === 401){
                    inject(Auth).logout();
                    return EMPTY;
                }
                
                const appError = new AppHttpError(error.status, error.message, error.error);
                console.error(appError);
                return throwError(() => appError); //rethrow, not next().
                // The error can be propagated like in the line above or it can be stopped from propagating further using "return EMPTY;""
            }
          
            return throwError(() => error); 
        })
    );
};