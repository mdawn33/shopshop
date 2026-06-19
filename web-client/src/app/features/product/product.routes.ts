import { Routes } from "@angular/router";
import { ProductDetail } from "./product-detail/product-detail";

export default [
    {
        path: 'detail/:id', component: ProductDetail
    }
] as Routes;