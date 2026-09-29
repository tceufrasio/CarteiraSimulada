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
            searchOrders: (page: number) => of({
              items: [{
                id: page === 1
                  ? '11111111-1111-1111-1111-111111111111'
                  : '22222222-2222-2222-2222-222222222222',
                symbol: page === 1 ? 'PETR4' : 'VALE3',
                side: 'BUY',
                quantity: 2,
                price: 35.5,
                createdAt: '2026-09-28T12:00:00Z'
              }],
              totalCount: 12,
              page,
              pageSize: 10
            }),
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
    expect(page.querySelectorAll('.history tbody tr')).toHaveLength(1);
  });

  it('filters open positions and paginates orders independently', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.componentInstance.refresh();

    fixture.componentInstance.setPositionSearch('ITUB');
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelectorAll('.positions-table tbody tr')).toHaveLength(1);
    expect(page.querySelector('.position-list')?.textContent).toContain('ITUB4');

    fixture.componentInstance.positions.set(
      Array.from({ length: 9 }, (_, index) => ({
        symbol: `ATV${index}`, quantity: 1, averagePrice: 10
      }))
    );
    fixture.componentInstance.setPositionSearch('');
    fixture.componentInstance.setPositionPage(2);
    fixture.detectChanges();
    expect(page.querySelectorAll('.positions-table tbody tr')).toHaveLength(1);
    expect(page.querySelector('.position-list')?.textContent).toContain('ATV8');

    fixture.componentInstance.setOrderPage(2);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.querySelector('.history tbody')?.textContent).toContain('VALE3');
    expect(page.querySelector('.history .pagination')?.textContent).toContain('Página 2 de 2');
    expect(page.querySelector('[aria-label="Páginas das posições"]')?.textContent)
      .toContain('Página 2 de 2');
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
