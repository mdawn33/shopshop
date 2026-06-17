import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http"
import { Auth } from "../services/auth"
import { inject } from "@angular/core";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    // const token = inject(Auth).token();
    
    // if(!token){
    //     return next(req);
    // }

    return next(req.clone({
        withCredentials: true
    }));
}