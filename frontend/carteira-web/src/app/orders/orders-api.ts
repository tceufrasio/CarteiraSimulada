import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export type OrderSide = 'BUY' | 'SELL';

export interface Order {
  id: string;
  symbol: string;
  side: OrderSide;
  quantity: number;
  price: number;
  createdAt: string;
  replayed?: boolean;
}

export interface Position {
  symbol: string;
  quantity: number;
  averagePrice: number;
}

export interface PortfolioSummary {
  investedAmount: number;
  realizedProfitLoss: number;
}

export interface OrderPage {
  items: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateOrder {
  symbol: string;
  side: OrderSide;
  quantity: number;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);

  getRecent() {
    return this.http.get<Order[]>('/api/orders');
  }

  searchOrders(page: number, pageSize: number, symbol: string, side: string) {
    const params: Record<string, string> = {
      page: String(page), pageSize: String(pageSize)
    };
    if (symbol) params['symbol'] = symbol;
    if (side) params['side'] = side;
    return this.http.get<OrderPage>('/api/orders/search', { params });
  }

  getSummary() {
    return this.http.get<PortfolioSummary>('/api/portfolio/summary');
  }

  getAllPositions() {
    return this.http.get<Position[]>('/api/positions');
  }
  getPosition(symbol: string) {
    return this.http.get<Position>(
      `/api/positions/${encodeURIComponent(symbol)}`
    );
  }

  create(order: CreateOrder, idempotencyKey: string) {
    return this.http.post<Order>('/api/orders', order, {
      headers: { 'Idempotency-Key': idempotencyKey }
    });
  }
}
