# Habilidades delegadas à IA

Áreas de conhecimento em que a IA (Claude, via Claude Code) foi usada como
ferramenta neste desafio. A direção, as decisões e a revisão crítica foram
minhas — a IA acelerou a produção, não substituiu o julgamento (ver
[revisoes.md](revisoes.md)).

| Área | O que foi delegado | O que eu controlei |
|------|--------------------|--------------------|
| **Contrato OpenAPI** | Geração do `openapi.yaml` 3.0: paths, schemas, responses, exemplos de ProblemDetails. | Estrutura dos endpoints, status codes por rota, semântica de cada erro. |
| **Decisões de design** | Redação do `docs/decisoes.md` a partir das decisões que travei. | Todas as decisões D1–D12 (transição estrita, desarquivar, completedAt read-only…). |
| **Arquitetura** | Sugestão da separação em camadas (Controllers / Services / Domain / Persistence). | Aderência a Clean Code / SOLID e ao que o enunciado pede. |
| **Casos de erro** | Levantamento sistemático dos status codes e cenários (400 × 404 × 422). | Fronteira 400 vs 422 e o mapeamento regra → resposta. |
| **Interação entre regras** | Verificação de consistência entre as 5 restrições. | Identificação e explicitação do acoplamento regras 1 × 2 × 5 (§4.1). |

> A IA foi tratada como um par de programação sênior a quem se delega execução,
> não a fonte da verdade. A fonte da verdade é a spec — `openapi.yaml` e
> `docs/decisoes.md`.
