import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  CreateOrder,
  Order,
  OrderSide,
  OrdersApi,
  Position,
  PortfolioSummary
} from './orders/orders-api';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly api = inject(OrdersApi);
  private pendingRequest: { body: string; key: string } | null = null;

  readonly orders = signal<Order[]>([]);
  readonly positions = signal<Position[]>([]);
  readonly summary = signal<PortfolioSummary>({ investedAmount: 0, realizedProfitLoss: 0 });
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');

  readonly positionSearch = signal('');
  readonly positionPage = signal(1);
  readonly positionPageSize = 8;
  readonly filteredPositions = computed(() => {
    const term = this.positionSearch().trim().toUpperCase();
    return this.positions().filter(position => position.symbol.includes(term));
  });
  readonly positionPageCount = computed(() =>
    Math.max(1, Math.ceil(this.filteredPositions().length / this.positionPageSize))
  );
  readonly currentPositionPage = computed(() =>
    Math.min(this.positionPage(), this.positionPageCount())
  );
  readonly visiblePositions = computed(() => {
    const start = (this.currentPositionPage() - 1) * this.positionPageSize;
    return this.filteredPositions().slice(start, start + this.positionPageSize);
  });

  orderSearch = '';
  orderSide = '';
  readonly orderPage = signal(1);
  readonly orderPageSize = 10;
  readonly orderTotal = signal(0);
  readonly orderPageCount = computed(() =>
    Math.max(1, Math.ceil(this.orderTotal() / this.orderPageSize))
  );
  readonly loadingOrders = signal(false);
  private orderRequestId = 0;

  form: CreateOrder = {
    symbol: '',
    side: 'BUY',
    quantity: 1,
    price: 1
  };

  ngOnInit(): void {
    void this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    try {
      const [positions, summary] = await Promise.all([
        firstValueFrom(this.api.getAllPositions()),
        firstValueFrom(this.api.getSummary()),
        this.loadOrders()
      ]);

      this.positions.set(positions);
      this.summary.set(summary);
    } catch {
      this.error.set('Não foi possível carregar a carteira. Confira a API.');
    } finally {
      this.loading.set(false);
    }
  }

  setPositionSearch(value: string): void {
    this.positionSearch.set(value);
    this.positionPage.set(1);
  }

  setPositionPage(page: number): void {
    if (page >= 1 && page <= this.positionPageCount())
      this.positionPage.set(page);
  }

  searchOrders(): void {
    this.orderPage.set(1);
    void this.loadOrders();
  }

  setOrderPage(page: number): void {
    if (page < 1 || page > this.orderPageCount()) return;
    this.orderPage.set(page);
    void this.loadOrders();
  }

  async loadOrders(): Promise<void> {
    const requestId = ++this.orderRequestId;
    this.loadingOrders.set(true);
    try {
      const result = await firstValueFrom(this.api.searchOrders(
        this.orderPage(),
        this.orderPageSize,
        this.orderSearch.trim().toUpperCase(),
        this.orderSide
      ));
      if (requestId !== this.orderRequestId) return;
      this.orders.set(result.items);
      this.orderTotal.set(result.totalCount);
    } catch {
      if (requestId !== this.orderRequestId) return;
      this.error.set('Não foi possível consultar as ordens. Confira a API.');
    } finally {
      if (requestId === this.orderRequestId) this.loadingOrders.set(false);
    }
  }

  async submit(): Promise<void> {
    if (this.saving()) return;

    this.error.set('');
    this.notice.set('');

    const order: CreateOrder = {
      symbol: this.form.symbol.trim().toUpperCase(),
      side: this.form.side as OrderSide,
      quantity: Number(this.form.quantity),
      price: Number(this.form.price)
    };

    if (!/^[A-Z0-9]{3,12}$/.test(order.symbol) ||
        order.quantity <= 0 || order.price <= 0) {
      this.error.set('Confira o ativo, a quantidade e o preço.');
      return;
    }

    const body = JSON.stringify(order);
    if (this.pendingRequest?.body !== body) {
      this.pendingRequest = { body, key: crypto.randomUUID() };
    }

    this.saving.set(true);

    try {
      const result = await firstValueFrom(
        this.api.create(order, this.pendingRequest.key)
      );

      this.pendingRequest = null;
      this.notice.set(
        result.replayed
          ? 'Operação já registrada anteriormente.'
          : 'Operação registrada com sucesso.'
      );
      this.form = { symbol: '', side: 'BUY', quantity: 1, price: 1 };
      await this.refresh();
    } catch (error: unknown) {
      const response = error as {
        status?: number;
        error?: { error?: string };
      };

      this.error.set(
        response.error?.error ??
        (response.status === 409
          ? 'Venda acima da posição disponível ou chave já utilizada.'
          : 'Não foi possível registrar. Tente novamente.')
      );
    } finally {
      this.saving.set(false);
    }
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      dateStyle: 'short',
      timeStyle: 'short'
    }).format(new Date(value));
  }
}
