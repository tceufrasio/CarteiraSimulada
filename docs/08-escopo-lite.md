# 08 — Escopo da CarteiraSimulada Lite

## Objetivo
Aplicação pública de portfólio para registrar operações fictícias e visualizar
uma carteira simples. O código será publicado no GitHub para demonstrar
decisões técnicas e testes.

## Funcionalidades
- Registrar compras e vendas fictícias de ativos.
- Consultar ordens por ID e listar ordens recentes.
- Mostrar posição e preço médio por ativo.
- Impedir venda acima da quantidade disponível.
- Usar Angular para registrar operações e visualizar a carteira.
- Não movimentar dinheiro nem enviar ordens a corretoras.

## Engenharia
- API .NET 10 com Clean Architecture e CQRS.
- Regras de domínio e testes automatizados.
- PostgreSQL, EF Core e migrations.
- Idempotência em requisições repetidas e concorrentes.
- Docker Compose para desenvolvimento local.
- RabbitMQ para tarefa não crítica, com confirmação de publicação,
  consumidor idempotente, retry limitado e fila de falhas.
- CI com build e testes automatizados; documentação de instalação e arquitetura.

## Limite da mensageria
RabbitMQ não será a fonte da posição da carteira. A gravação no PostgreSQL
e a publicação no broker não compartilham uma transação. A tarefa escolhida
para o Worker poderá falhar sem tornar incorretos os dados da carteira.

## Fora do escopo
- Operações reais, integração com corretora e cotações automáticas.
- CDB, LCI, LCA e outras funcionalidades do possível produto comercial.
- Recomendações de investimento por IA.
- Outbox.

## Estado
API, posições, Angular e mensageria secundária implementados. Testes locais e CI da etapa de mensageria aprovados.
