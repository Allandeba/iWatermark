# iWatermark

| **Redes Sociais** | **Linguagens** | **Versão do .NET** | **Último Commit** |
| ----------------- | -------------- | ------------------- | ------------------ |
| [![LinkedIn](https://img.shields.io/badge/LinkedIn-Connect-blue)](https://www.linkedin.com/in/allan-debastiani/) | ![GitHub language count](https://img.shields.io/github/languages/count/allandeba/iWatermark) ![GitHub top language](https://img.shields.io/github/languages/top/allandeba/iWatermark) | ![.NET 10](https://img.shields.io/badge/.NET-10.0-green) | ![GitHub last commit](https://img.shields.io/github/last-commit/allandeba/iWatermark) |

## Visão Geral

Seja bem-vindo ao iWatermark, um aplicativo intuitivo para adicionar logomarca a várias fotos simultaneamente.

**Para experimentar o sistema, acesse: [iwatermark.allandeba.dev.br](https://iwatermark.allandeba.dev.br)**


## Tecnologias Utilizadas

- **Framework:** ASP.NET Core 10.0
- **Conteinerização:** Docker
- **Hospedagem:** Coolify em Linux
- **Arquitetura:** MVC
- **UI:** Bootstrap

## Deploy no Coolify

Use o build pack **Dockerfile**, com `Dockerfile` na raiz e porta interna `8080`.
O proxy do Coolify recebe o HTTPS; o app atende por HTTP dentro do contêiner.
O health check consulta `GET /` na porta interna. O app não requer banco de dados,
volume de uploads ou segredos de build. O build e a publicação são feitos pelo
Coolify, sem GitHub Actions.

## Configurações Personalizadas

- **Posição da Logomarca:** Escolha o local onde deseja aplicar sua logomarca em cada foto.
- **Formato de Saída:** Selecione o formato desejado para as imagens finais.
- **Opacidade da Logomarca:** Ajuste a transparência para garantir uma integração perfeita com suas fotos.
- **Proporção da Logomarca:** Mantenha o equilíbrio ideal entre o tamanho da logomarca e a foto anexada.

Aprecie a facilidade de personalizar suas imagens em lote com o iWatermark!
