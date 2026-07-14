import { Component, inject, input, computed } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiRequests } from '../../../core/services/api-requests';
import { getProductById } from '../../../shared/data/sample-data';
import { RouterLink } from "@angular/router";
import { filledStars } from '../../../core/helpers/catalog-format-helpers';

@Component({
  selector: 'app-product-detail',
  imports: [RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail {
  // ActivatedRoute is the service that provides the information about the current route -> url, data, params, queryParams
  // Page navigation => events   over time, and every time I can retrieve a route snapshot of that specific moment 
  // private route = inject(ActivatedRoute);

  private productRequestsService = inject(ApiRequests);
  protected readonly starSlots = [0,1,2,3,4];

  // Since Angular 16 we can bind route data to a component input:
  readonly id = input.required<string>();

  // Use the following line to get the product from the sample data. Until the backend call is implemented
  protected product = computed(() => getProductById(this.id())!);

  protected readonly filledStars = filledStars;

  // 2. Automatically triggers a reload whenever the 'id' input changes
  // productResource = rxResource({
  //   // params tracks dependencies. When id() changes, the stream automatically re-runs
  //   params: () => ({ id: this.id() }),
  //   // stream handles the RxJS Observable backend calls. How ?????
  //   stream: ({params}) => this.productRequestsService.getProductDetails(params.id)
  // });


  constructor() {
    // access parameters from snapshot
    // this.productId = this.route.snapshot.paramMap.get('id');
  }

  ngOnInit() {

  }

}
