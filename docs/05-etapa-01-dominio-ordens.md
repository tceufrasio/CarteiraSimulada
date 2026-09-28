# 05 — Etapa 01: domínio de ordens

## Estado
Domínio inicial concluído em 25/09/2026. API e persistência foram implementadas nas etapas seguintes.

## Implementado
- OrderSide: Buy e Sell.
- Order.Create: identificador, data e normalização do ativo.
- Validação de ativo, lado, quantidade e preço.
- Quantidade com até 4 casas e preço com até 2 casas decimais.

## Decisões
- O domínio não depende de HTTP, banco ou Angular.
- As ordens são fictícias e não movimentam dinheiro.
- A regra de venda sem posição ainda será definida.

## Validação
dotnet build: aprovado.
dotnet test: 11 aprovados, 0 falhas.

## Próximo passo
Criar os contratos e o caso de uso de registro na Application.

## Application — registro de ordem
- Criado IOrderRepository como contrato de persistência.
- Criado CreateOrder para montar e salvar uma ordem.
- TimeProvider fornece a data de criação.
- Ordem inválida não chama o repositório.
- Validação: dotnet build aprovado; dotnet test com 13 aprovados e 0 falhas.
- PostgreSQL e endpoint HTTP ainda pendentes.

## Persistência PostgreSQL
- Criado CarteiraDbContext e mapeamento de Order.
- Criado EfOrderRepository, implementação de IOrderRepository.
- Migration InitialOrders criada e aplicada.
- Tabela orders verificada diretamente no PostgreSQL.
- Colunas: Id, Symbol, Side, Quantity, Price e CreatedAt.
- Índice IX_orders_CreatedAt criado.
- PostgreSQL local em Docker, porta 15432.
- Endpoints e gravação pela API implementados e validados nas etapas seguintes.

## Primeiro fluxo HTTP validado
- POST /api/orders registrou uma ordem PETR4 no PostgreSQL.
- ID retornado pela API: 8dbc668c-aa19-41e4-bc5e-a928e6f7e22e.
- A mesma linha foi conferida diretamente na tabela orders.
- Ativo inválido AB retornou HTTP 400.
- Após a requisição inválida, a tabela continuou com 1 ordem.
- Ainda não há consulta por ID nem proteção contra duplicidade.

## Consulta de ordem por ID
- Criado GetOrderById na Application.
- EfOrderRepository consulta com AsNoTracking.
- GET /api/orders/{id} retornou a ordem persistida anteriormente.
- O endereço informado no 201 Created agora pode ser consultado.
- ID inexistente retornou HTTP 404.

## Listagem e fechamento da etapa 01
- GET /api/orders lista até 100 ordens recentes.
- A ordem PETR4 criada anteriormente apareceu na resposta.
- Fluxos demonstrados: POST /api/orders, GET /api/orders/{id} e GET /api/orders.
- Entrada inválida retorna 400; ID inexistente retorna 404.
- Build aprovado e 13 testes automatizados aprovados.
- Pendente para a etapa 02: impedir duplicação quando o cliente repete um POST.

