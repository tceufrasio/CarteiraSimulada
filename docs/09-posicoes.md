# 09 — Posições da carteira Lite

## Implementado
- Position calcula quantidade e preço médio por ativo.
- Compra recalcula o preço médio ponderado.
- Venda parcial mantém o preço médio; venda total zera a posição.
- O domínio rejeita venda acima da quantidade disponível.
- GetPositionQuery reconstrói a posição a partir do histórico de ordens.
- EfPositionOrderReader lê ordens do PostgreSQL com AsNoTracking.
- GET /api/positions/{symbol} retorna a posição ou HTTP 404.

## Evidências
- dotnet build: aprovado.
- dotnet test: 28 aprovados, 0 falhas.
- GET /api/positions/PETR4: quantidade 2, preço médio 35,50.
- GET para ativo sem ordens: HTTP 404.

## Segurança da gravação
- Ordens têm Sequence gerada pelo PostgreSQL; as ordens antigas foram
  numeradas pela data e pelo ID na migration AddOrderSequence.
- Operações do mesmo ativo são serializadas por bloqueio transacional
  no PostgreSQL.
- O writer consulta a chave de idempotência antes de validar uma
  venda repetida.
- Histórico, validação, ordem e chave são tratados na transação.
- Venda sem posição retornou HTTP 409.
- Compra, venda e replay retornaram 201, 201 e 200; o replay manteve
  o mesmo ID e não reduziu novamente a posição.
- Duas vendas simultâneas disputando uma unidade retornaram 201 e 409;
  a posição final foi zero.
- O cenário concorrente ainda precisa de teste de integração automatizado.

## Escopo
Esta versão tem uma carteira fictícia única. Contas e carteiras por
usuário pertencem ao planejamento separado do produto Full.


