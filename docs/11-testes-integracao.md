# 11 — Testes de integração PostgreSQL

## O que verificam
- Primeira gravação cria a ordem; a mesma chave e pedido retornam a ordem
  original; reutilizar a chave com outro pedido causa conflito.
- Duas vendas simultâneas disputando uma única unidade produzem apenas
  uma venda persistida.
- Cada cenário usa identificadores próprios e remove seus registros ao final.

## Preparação local
1. Iniciar o PostgreSQL com docker compose up -d postgres.
2. Criar o banco carteira_tests se ele ainda não existir.
3. Aplicar nele as migrations da Infrastructure com dotnet-ef.

O banco carteira_tests é separado do banco carteira usado pela aplicação.

## Execução no PowerShell
Definir temporariamente a conexão e executar:

$env:CARTEIRA_TEST_CONNECTION = 'Host=localhost;Port=15432;Database=carteira_tests;Username=carteira;Password=carteira_local'
dotnet test tests\Carteira.IntegrationTests\Carteira.IntegrationTests.csproj
Remove-Item Env:\CARTEIRA_TEST_CONNECTION

Para rodar apenas os 29 testes de domínio e aplicação:
dotnet test tests\Carteira.Tests\Carteira.Tests.csproj

## Estado
Dois testes de integração aprovados no PostgreSQL 17 local.
O CI ainda precisa iniciar PostgreSQL e aplicar migrations antes dos testes.
