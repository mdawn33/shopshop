import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductRail } from './product-rail';

describe('ProductRail', () => {
  let component: ProductRail;
  let fixture: ComponentFixture<ProductRail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductRail],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductRail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
