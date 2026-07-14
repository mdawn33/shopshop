import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Product } from '../models/product';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

//TODO separate the requests for feature?

@Injectable({
  providedIn: 'root',
})
export class ApiRequests {

  private http = inject(HttpClient);

  getProductDetails(productId: string): Observable<Product> {

    // TODO handle null values returned (404 Not Found)
    return this.http.get<Product>(`${environment.apiGatewayUrl}/products-api/products/${productId}`);
  }
}
