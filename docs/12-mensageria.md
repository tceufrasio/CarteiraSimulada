# 12 — Mensageria da CarteiraSimulada Lite

## Finalidade
Após criar uma ordem, a API publica uma notificação fictícia para uma tarefa
secundária. Ordens e posições são gravadas e consultadas no PostgreSQL;
o RabbitMQ não participa do cálculo da carteira.

## Fluxo
1. A API grava a ordem e a chave de idempotência no PostgreSQL.
2. Apenas para uma ordem nova, publica OrderRegisteredNotification.
3. O publicador usa mensagem persistente e aguarda confirmação do RabbitMQ.
4. O Worker consome com confirmação manual após registrar o processamento.
5. OrderId é chave primária em processed_order_notifications; entregas
   repetidas não criam outro processamento.

## Falhas no consumidor
- Uma falha envia a mensagem para carteira.order-notifications.retry.
- Essa fila espera 5 segundos e devolve a mensagem à fila principal.
- Após 3 retries, o Worker envia a mensagem para
  carteira.order-notifications.failed.
- A fila de falhas exige inspeção e decisão manual antes de reprocessar.

## Limite sem Outbox
A transação do PostgreSQL não inclui a publicação no RabbitMQ. Se a API
falhar após confirmar a ordem e antes de confirmar a mensagem, a ordem
permanece válida, mas a notificação pode ser perdida. A API registra
um aviso no log e mantém HTTP 201. Este fluxo serve apenas para tarefa
secundária; não oferece entrega garantida de evento de negócio.

## Configuração local
Inicie os serviços com `docker compose up -d`.

Configure os user secrets da API:
`dotnet user-secrets set "ConnectionStrings:Carteira" "Host=localhost;Port=15432;Database=carteira;Username=carteira;Password=carteira_local" --project src\Carteira.Api`
`dotnet user-secrets set "ConnectionStrings:RabbitMq" "amqp://carteira:carteira_local@localhost:5673/" --project src\Carteira.Api`

Configure os user secrets do Worker:
`dotnet user-secrets set "ConnectionStrings:Carteira" "Host=localhost;Port=15432;Database=carteira;Username=carteira;Password=carteira_local" --project src\Carteira.Worker`
`dotnet user-secrets set "ConnectionStrings:RabbitMq" "amqp://carteira:carteira_local@localhost:5673/" --project src\Carteira.Worker`

Inicie o Worker com `DOTNET_ENVIRONMENT=Development` e
`dotnet run --project src\Carteira.Worker --no-launch-profile`.

## Validação local
- Compra nova: HTTP 201 e uma mensagem na fila.
- Replay da mesma chave: HTTP 200, mesmo ID e nenhuma mensagem adicional.
- Worker consumiu a mensagem e gravou um registro no PostgreSQL.
- Mensagem inválida passou pela fila de retry e chegou à fila de falhas.
- Broker indisponível: a compra permaneceu válida e retornou HTTP 201.
- Teste de integração: mesmo OrderId processado duas vezes gera um registro.
