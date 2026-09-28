# 07 — CQRS e Clean Architecture

## Estado
Implementado e validado em 25/09/2026.

## CQRS neste projeto
- Command: RegisterOrderCommandHandler registra ordens com idempotência.
- Queries: GetOrderByIdQueryHandler e GetRecentOrdersQueryHandler consultam ordens.
- Escrita e leitura têm contratos separados na Application.
- As consultas usam AsNoTracking na Infrastructure.
- Ambos os lados usam o mesmo PostgreSQL. CQRS não exige bancos separados.

## Direção das dependências
- Domain contém Order, OrderSide e regras de criação; não depende das outras camadas.
- Application depende de Domain e define casos de uso e contratos.
- Infrastructure implementa os contratos da Application com EF Core e PostgreSQL.
- Api recebe HTTP e liga as implementações por injeção de dependência.

## DDD: estado atual
- Order concentra invariantes da criação da ordem.
- Ainda precisamos definir melhor o contexto da carteira, posições e regras de venda.
- Ter uma entidade de domínio não significa que todo o desenho DDD esteja concluído.

## Validação
- dotnet build: aprovado.
- dotnet test: 19 aprovados, 0 falhas.
- GET por ID e listagem foram verificados pela API com dados persistidos.
- Testes das queries cobrem ordem existente, inexistente e limite da listagem.

## Próximo passo
Definir as regras de posição da carteira antes de implementar compras e vendas com saldo.
