import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { OrdersApi } from './orders/orders-api';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: OrdersApi,
          useValue: {
            getRecent: () => of([
              {
                id: '11111111-1111-1111-1111-111111111111',
                symbol: 'PETR4',
                side: 'BUY',
                quantity: 2,
                price: 35.5,
                createdAt: '2026-09-28T12:00:00Z'
              }
            ]),
            getAllPositions: () => of([
              { symbol: 'ITUB4', quantity: 1, averagePrice: 30 },
              { symbol: 'PETR4', quantity: 2, averagePrice: 35.5 }
            ]),
            getSummary: () => of({ investedAmount: 101, realizedProfitLoss: 3 }),
            create: () => throwError(() => ({
              status: 409,
              error: { error: 'Quantidade indisponível para venda.' }
            }))
          }
        }
      ]
    }).compileComponents();
  });

  it('shows positions even when the asset is absent from recent orders', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.componentInstance.refresh();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('');
    expect(fixture.componentInstance.positions()).toHaveLength(2);
    const page = fixture.nativeElement as HTMLElement;
    const positions = page.querySelectorAll('.position');

    expect(positions).toHaveLength(2);
    expect(page.textContent).toContain('Investido nas posições abertas');
    expect(page.textContent).toContain('101,00');
    expect(page.textContent).toContain('Lucro/prejuízo realizado nas vendas');
    expect(page.textContent).toContain('3,00');
    expect(page.querySelector('.position-list')?.textContent).toContain('ITUB4');
    expect(page.querySelector('.position-list')?.textContent).toContain('PETR4');
    expect(page.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('shows the API conflict message when a sale is rejected', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.componentInstance.refresh();

    fixture.componentInstance.form = {
      symbol: 'PETR4',
      side: 'SELL',
      quantity: 3,
      price: 35.5
    };

    await fixture.componentInstance.submit();
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('[role="alert"]')?.textContent)
      .toContain('Quantidade indisponível para venda.');
  });
});
