# CarteiraSimulada Lite

Carteira fictÃ­cia para demonstrar uma API .NET 10 com Clean Architecture,
CQRS, PostgreSQL e idempotÃªncia. NÃ£o movimenta dinheiro nem envia ordens
a corretoras.

## Funcionalidades atuais

- Registrar compras e vendas fictÃ­cias.
- Consultar ordens e posiÃ§Ã£o por ativo.
- Calcular quantidade e preÃ§o mÃ©dio.
- Impedir venda acima da posiÃ§Ã£o, inclusive em chamadas concorrentes.
- Repetir uma ordem com a mesma Idempotency-Key sem duplicÃ¡-la.
- 29 testes automatizados de domÃ­nio e aplicaÃ§Ã£o, alÃ©m de 1 teste Angular.

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

## ValidaÃ§Ã£o

Execute `dotnet build` e `dotnet test tests\Carteira.Tests\Carteira.Tests.csproj` para os testes rápidos. Os testes com PostgreSQL estão em `docs/11-testes-integracao.md`. Para o Angular, execute `npm run build` e `npm test -- --watch=false` em `frontend/carteira-web`.

## Estado

RabbitMQ, Worker, CI/CD e testes de integraÃ§Ã£o automatizados
ainda estÃ£o pendentes. A aplicaÃ§Ã£o tem uma Ãºnica carteira fictÃ­cia, sem
contas de usuÃ¡rio, autenticaÃ§Ã£o ou cotaÃ§Ãµes automÃ¡ticas. O endpoint
`/health` nÃ£o verifica a conexÃ£o com o PostgreSQL.

Consulte `docs/08-escopo-lite.md` para o escopo, `docs/10-angular.md` para a interface e `docs/09-posicoes.md`
para as regras e os testes de posiÃ§Ã£o.
