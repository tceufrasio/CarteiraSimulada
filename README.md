# CarteiraSimulada Lite

Carteira fictícia para demonstrar uma API .NET 10 com Clean Architecture,
CQRS, PostgreSQL e idempotência. Não movimenta dinheiro nem envia ordens
a corretoras.

## Funcionalidades atuais

- Registrar compras e vendas fictícias.
- Consultar ordens e posição por ativo.
- Calcular quantidade e preço médio.
- Impedir venda acima da posição, inclusive em chamadas concorrentes.
- Repetir uma ordem com a mesma Idempotency-Key sem duplicá-la.
- 29 testes automatizados de domínio e aplicação, além de 1 teste Angular.

## Como executar no PowerShell

1. Inicie o banco: `docker compose up -d postgres`.
2. Restaure a ferramenta: `dotnet tool restore`.
3. Configure o segredo da API:
   `dotnet user-secrets set "ConnectionStrings:Carteira" "Host=localhost;Port=15432;Database=carteira;Username=carteira;Password=carteira_local" --project src\Carteira.Api`
4. Defina o ambiente: `$env:ASPNETCORE_ENVIRONMENT = 'Development'`.
5. Aplique as migrations:
   `dotnet tool run dotnet-ef database update --project src\Carteira.Infrastructure --startup-project src\Carteira.Api`
6. Inicie a API:
   `dotnet run --project src\Carteira.Api --no-launch-profile --urls http://localhost:5080`

A senha do exemplo serve apenas para o PostgreSQL local em `compose.yaml`.

## Endpoints

- `GET /health`
- `POST /api/orders` (exige Idempotency-Key com UUID)
- `GET /api/orders/{id}`
- `GET /api/orders`
- `GET /api/positions/{symbol}`
- `GET /api/positions`

## Validação

Execute `dotnet build` e `dotnet test`. Para o Angular, execute `npm run build` e `npm test -- --watch=false` em `frontend/carteira-web`.

## Estado

RabbitMQ, Worker, CI/CD e testes de integração automatizados
ainda estão pendentes. A aplicação tem uma única carteira fictícia, sem
contas de usuário, autenticação ou cotações automáticas. O endpoint
`/health` não verifica a conexão com o PostgreSQL.

Consulte `docs/08-escopo-lite.md` para o escopo, `docs/10-angular.md` para a interface e `docs/09-posicoes.md`
para as regras e os testes de posição.
