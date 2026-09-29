import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  CreateOrder,
  Order,
  OrderSide,
  OrdersApi,
  Position
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
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');

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
      const [orders, positions] = await Promise.all([
        firstValueFrom(this.api.getRecent()),
        firstValueFrom(this.api.getAllPositions())
      ]);

      this.orders.set(orders);
      this.positions.set(positions);
    } catch {
      this.error.set('Não foi possível carregar a carteira. Confira a API.');
    } finally {
      this.loading.set(false);
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
