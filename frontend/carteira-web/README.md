# Interface Angular — CarteiraSimulada Lite

Interface Angular 22 para registrar compras e vendas fictícias e consultar
ordens recentes e posições abertas.

## Execução local

Inicie primeiro o PostgreSQL e a API conforme o README da raiz do projeto.
A API deve estar em http://localhost:5080.

Nesta pasta, execute:

1. `npm ci`
2. `npm start -- --proxy-config proxy.conf.json`

Abra http://localhost:4200. O proxy encaminha `/api` e `/health` para a API.

## Validação

- `npm run build`
- `npm test -- --watch=false`

A documentação do projeto está em `../../README.md` e `../../docs/10-angular.md`.
