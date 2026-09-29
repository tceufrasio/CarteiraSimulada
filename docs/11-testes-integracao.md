# 11 — Testes de integração PostgreSQL

## O que verificam
- Primeira gravação cria a ordem; a mesma chave e pedido retornam a ordem
  original; reutilizar a chave com outro pedido causa conflito.
- Duas vendas simultâneas disputando uma única unidade produzem apenas
  uma venda persistida.
- Cada cenário usa identificadores próprios e remove seus registros ao final.

- Processar duas vezes a mesma notificação registra apenas um OrderId.
- Publicar uma notificação no RabbitMQ entrega mensagem persistente em uma fila exclusiva do teste.

## Preparação local
1. Iniciar PostgreSQL e RabbitMQ com docker compose up -d.
2. Criar o banco carteira_tests se ele ainda não existir.
3. Aplicar nele as migrations da Infrastructure com dotnet-ef.

O banco carteira_tests é separado do banco carteira usado pela aplicação.

## Execução no PowerShell
Definir temporariamente a conexão e executar:

$env:CARTEIRA_TEST_CONNECTION = 'Host=localhost;Port=15432;Database=carteira_tests;Username=carteira;Password=carteira_local'
$env:CARTEIRA_TEST_RABBITMQ = 'amqp://carteira:carteira_local@localhost:5673/'
dotnet test tests\Carteira.IntegrationTests\Carteira.IntegrationTests.csproj
Remove-Item Env:\CARTEIRA_TEST_CONNECTION
Remove-Item Env:\CARTEIRA_TEST_RABBITMQ

Para rodar apenas os 29 testes de domínio e aplicação:
dotnet test tests\Carteira.Tests\Carteira.Tests.csproj

## Estado
Quatro testes de integração aprovados localmente com PostgreSQL 17 e RabbitMQ.
O CI inicia PostgreSQL, aplica as migrations e executa os testes. Primeira execução aprovada no GitHub Actions.
