import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PromoCarousel } from './promo-carousel';

describe('PromoCarousel', () => {
  let component: PromoCarousel;
  let fixture: ComponentFixture<PromoCarousel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PromoCarousel],
    }).compileComponents();

    fixture = TestBed.createComponent(PromoCarousel);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
