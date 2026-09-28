# 06 — Idempotência no registro de ordens

## Problema
O cliente pode repetir um POST após timeout ou falha de rede.
Sem proteção, cada tentativa criaria uma nova ordem.

## Contrato proposto
- POST /api/orders exige o cabeçalho Idempotency-Key com um UUID.
- Chave nova e pedido válido: cria ordem e retorna 201.
- Mesma chave e mesmo pedido: retorna o ID da ordem original, sem nova gravação.
- Mesma chave com pedido diferente: retorna 409 Conflict.
- Chave ausente ou inválida: retorna 400.

## Persistência
- Criar tabela para chave, identificação do pedido e ID da ordem.
- A chave terá índice único no PostgreSQL.
- A ordem e o registro da chave serão confirmados na mesma transação.
- Requisições concorrentes com a mesma chave devem produzir uma única ordem.

## Casos de teste
1. Primeira requisição cria uma ordem.
2. Repetição exata retorna a ordem original.
3. Reuso da chave com outro pedido retorna 409.
4. Requisições simultâneas com a mesma chave persistem uma única ordem.
5. Chave nova com o mesmo pedido cria uma nova ordem.

## Estado
Concluído e validado em 25/09/2026.


## Evidências
- Primeira chamada: HTTP 201, replayed false.
- Repetição com mesma chave e pedido: HTTP 200, replayed true e mesmo ID.
- Mesma chave com quantidade diferente: HTTP 409.
- Oito chamadas simultâneas: uma resposta 201, sete 200 e um único ID.
- Contagem final: 3 ordens e 2 registros de idempotência, incluindo dados de etapas anteriores.
- dotnet test: 16 testes aprovados.

## Observação operacional
- O EF registra a violação esperada da chave única no log antes de o writer tratá-la.
- Revisar a apresentação desse log na etapa de observabilidade.
