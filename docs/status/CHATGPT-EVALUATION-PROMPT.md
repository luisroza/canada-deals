# Prompt para avaliação no ChatGPT

Anexe primeiro `CHATGPT-EVALUATION-PACK.md` e `VALIDATION-SNAPSHOT-2026-08-31.md`. Se quiser uma análise mais profunda, anexe também os documentos indicados em `README.md`.

Use este prompt:

> Atue como um conselho sênior composto por Product Owner, UX/Product Designer, Solution Architect, Backend Lead, Frontend Lead, QA Lead, Application Security Reviewer e Release Manager. Avalie o estado atual do GreatDeals.ca usando somente os artefatos anexados.
>
> Separe claramente fatos validados, evidência histórica, inferências e informações ausentes. Não trate propostas futuras, integrações bloqueadas, credenciais, direitos de dados/imagens ou relações comerciais como aprovados. Respeite os checkpoints e a direção atual: ofertas individuais por listing, filtros públicos apenas por Category e Store, Wishlist como única retenção, sem comparação entre lojas, sem tracker/histórico público e sem alertas de preço.
>
> Produza: (1) resumo executivo; (2) avaliação de coerência do produto e UX; (3) avaliação de arquitetura e dados; (4) qualidade de backend e frontend; (5) QA e confiabilidade; (6) segurança e privacidade; (7) prontidão operacional; (8) inconsistências entre requisitos, documentos e implementação; (9) riscos P0/P1/P2; (10) plano priorizado das próximas ações; e (11) uma decisão objetiva entre “não pronto”, “pronto para checkpoint”, “release candidate local” ou “pronto para produção”, justificando a classificação.
>
> Dê atenção especial à árvore Git não commitada, à falha concorrente do teste Clear filters, ao conflito sobre a aba administrativa Brands, ao checkpoint de segurança do owner e aos gates externos de merchants/providers. Evite recomendar novas funcionalidades antes de fechar os bloqueadores atuais.
