# 10 — Interface Angular da CarteiraSimulada Lite

## Implementado
- Angular 22 com formulário de compra e venda fictícia.
- Consulta de ordens recentes e posições abertas.
- GET /api/positions usa o histórico completo; a lista de posições não
  depende do limite de 100 ordens recentes.
- Proxy local encaminha /api e /health para a API na porta 5080.
- POST envia Idempotency-Key gerada no navegador.
- Mensagens de sucesso e de erro são exibidas na tela.

## Validação
- npm run build: aprovado.
- npm test -- --watch=false: 2 testes aprovados.
- Testes da interface verificam posições ausentes das ordens recentes e
  a mensagem de conflito de uma venda rejeitada.
- No navegador: compra exibiu a nova posição.
- Venda parcial reduziu a quantidade e manteve o preço médio.
- Venda acima da posição exibiu erro e não alterou a carteira.

## Execução
1. Iniciar PostgreSQL e API conforme o README da raiz.
2. Em frontend/carteira-web, executar npm install.
3. Executar npm start -- --proxy-config proxy.conf.json.
4. Abrir http://localhost:4200.

## Limites da versão Lite
- Uma carteira fictícia compartilhada, sem contas de usuário.
- Preços informados manualmente, sem cotações de mercado.
- Histórico mostra as ordens recentes; posições consultam todo o histórico.
