# CarteiraSimulada Lite

Carteira fictícia para demonstrar .NET 10, Clean Architecture, CQRS,
PostgreSQL, Angular e RabbitMQ. Não movimenta dinheiro nem envia ordens
a corretoras.

## Funcionalidades

- Registrar compras e vendas fictícias.
- Consultar ordens e posições por ativo.
- Calcular quantidade e preço médio.
- Impedir venda acima da posição, inclusive em chamadas concorrentes.
- Repetir uma ordem com a mesma Idempotency-Key sem duplicá-la.
- Processar uma notificação secundária com RabbitMQ e Worker.

## Requisitos

.NET SDK 10, Docker Desktop, Node.js 24 e npm.

## Execução local

No PowerShell, defina `$env:ASPNETCORE_ENVIRONMENT = 'Development'` antes
de aplicar as migrations e iniciar a API. Defina
`$env:DOTNET_ENVIRONMENT = 'Development'` antes de iniciar o Worker.

1. Na raiz, execute `docker compose up -d` e `dotnet tool restore`.
2. Configure `ConnectionStrings:Carteira` e `ConnectionStrings:RabbitMq`
   nos user secrets de `src\Carteira.Api` e `src\Carteira.Worker`.
   Os comandos completos estão em `docs/12-mensageria.md`.
3. Aplique as migrations com `dotnet tool run dotnet-ef database update
   --project src\Carteira.Infrastructure --startup-project src\Carteira.Api`.
4. Inicie a API com `dotnet run --project src\Carteira.Api
   --no-launch-profile --urls http://localhost:5080`.
5. Inicie o Worker com `dotnet run --project src\Carteira.Worker
   --no-launch-profile`.
6. Em `frontend\carteira-web`, execute `npm ci` e
   `npm start -- --proxy-config proxy.conf.json`. Abra http://localhost:4200.

O painel RabbitMQ local fica em http://localhost:15673 (`carteira` / `carteira_local`).

## Endpoints

- `GET /health`
- `POST /api/orders` (exige Idempotency-Key com UUID)
- `GET /api/orders/{id}`
- `GET /api/orders`
- `GET /api/positions/{symbol}`
- `GET /api/positions`

## Testes

Execute `dotnet build CarteiraSimulada.sln` e
`dotnet test tests\Carteira.Tests\Carteira.Tests.csproj`.
Os testes de integração com PostgreSQL e RabbitMQ estão em `docs/11-testes-integracao.md`.
Em `frontend\carteira-web`, execute `npm run build` e
`npm test -- --watch=false`. O GitHub Actions executa o pipeline na `main`.

## Limites

Há uma carteira fictícia compartilhada, sem usuários, autenticação
ou cotações automáticas. O endpoint `/health` não verifica o PostgreSQL.
Sem Outbox, uma notificação pode ser perdida após gravar a ordem;
a carteira permanece correta. Veja `docs/12-mensageria.md`.
