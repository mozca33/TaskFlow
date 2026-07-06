# Análise Crítica — TaskFlow

Documento de trabalho: avaliação honesta do estado atual da API e um roadmap de
melhorias priorizado (segurança, infraestrutura, performance). Não faz parte do
contrato; serve para guiar a evolução incremental.

> **Decisão (2026-07-05):** as melhorias abaixo ficam como **TODO documentado**,
> não implementadas na entrega. O desafio valoriza escopo + spec-first sobre
> perfeição de código; a consciência dessas melhorias já está em `docs/decisoes.md §7`.
> Estas ficam como plano para evolução futura (idealmente atualizando a spec junto).

> Convenção de priorização: **Impacto** (alto/médio/baixo) × **Esforço** (baixo/
> médio/alto). "Quick win" = impacto alto + esforço baixo.

---

## 1. Veredito geral

Para o escopo do desafio, o sistema está **sólido e coerente**: camadas bem
separadas, spec-first, ProblemDetails em todo lugar, semântica de PATCH correta,
28 testes (incluindo validação de schema). O que falta é o que **deliberadamente**
ficou fora de escopo — e é justamente onde moram os riscos de um sistema real:
**não há autenticação, nem limites de taxa, nem paginação, nem observabilidade.**

A base é boa para evoluir: a separação em camadas e o uso de EF Core deixam quase
todas as melhorias abaixo como adições, não reescritas.

---

## 2. O que ficou bom

- **Arquitetura em camadas limpa** (Controllers → Services → Domain/Persistence),
  com regras de negócio concentradas nos services. Fácil de testar e evoluir.
- **Spec-first real**: `openapi.yaml` é autoritativo e os testes validam os
  responses contra ele (`OpenApiSchemaTests`). O loop de aderência fecha sozinho.
- **Tratamento de erro consistente**: RFC 7807 em 400/404/422, num handler central
  (`DomainExceptionHandler`), sem `if` de erro espalhado.
- **Semântica de PATCH correta**: `Optional<T>` distingue ausente de `null` — raro
  de ver bem-feito.
- **Robustez de entrada**: rejeição de campos desconhecidos, enums validados,
  tamanhos limitados, `completedAt` read-only (sem over-posting).
- **UTC consistente** nos timestamps (bug pego na verificação e corrigido).
- **Higiene de git**: um commit por responsabilidade, histórico conta a história.

---

## 3. O que ficou ruim / frágil

Pontos honestos, do mais para o menos crítico:

1. **Sem autenticação/autorização** — qualquer um lê e altera qualquer projeto/
   tarefa. Não há dono de recurso. É o maior gap para um sistema real. *(fora de
   escopo do desafio, mas é o item nº 1.)*
2. **Listagens sem paginação** — `GET /projetos` e `GET /tarefas` retornam **tudo**.
   Com muitos registros vira problema de memória e latência (e vetor de DoS).
3. **`Database.Migrate()` no startup** — conveniente para o desafio, mas é um
   anti-padrão em produção multi-instância (corrida de migrations) e acopla o
   boot à migração.
4. **Sem rate limiting** — nada impede abuso/DoS por volume de requisições.
5. **Sem observabilidade** — só logging de console padrão; sem logs estruturados,
   métricas, tracing ou health check. Difícil de operar e diagnosticar.
6. **Race condition na regra 1** — entre checar "existe tarefa in_progress?" e
   gravar `archived`, uma requisição concorrente pode transicionar uma tarefa. A
   janela é pequena e o `SaveChanges` é transacional, mas a checagem não está sob
   o mesmo isolamento. Sem concorrência otimista (ETag/rowversion), dois PATCH
   simultâneos podem se sobrescrever.
7. **`DateTime.UtcNow` direto nos services** — dificulta testar comportamento
   dependente de tempo. `TimeProvider` (.NET 8) seria injetável e testável.
8. **SQLite** — ótimo para o desafio, inadequado para carga concorrente real.
   Mitigado por usarmos EF Core (troca de provider é de baixo atrito).
9. **Location hardcoded** (`/tarefas/{id}`) na criação de tarefa — quebra se o
   roteamento mudar. Menor.
10. **Dois formatos de 404** — 404 de recurso inexistente (GUID válido, mas não
    encontrado) traz corpo `ProblemDetails` via `DomainExceptionHandler`; já o 404
    de UUID malformado (route constraint `{id:guid}`, D6) vem com **corpo vazio**,
    porque nenhuma rota casa e o framework responde antes do pipeline de exceção.
    É coerente com o D6, mas inconsistente na forma. `UseStatusCodePages` ou um
    middleware de ProblemDetails por status code uniformizaria as duas respostas.
    *(Achado em teste manual, igual ao bug de UTC.)*
11. **Mensagens de validação vazam o nome do tipo interno** — o 400 de campo
    desconhecido (`JsonUnmappedMemberHandling.Disallow`) devolve a mensagem nativa
    do `System.Text.Json`, que cita `TaskFlow.Api.Dtos.CreateTaskRequest`. É
    **information disclosure** de baixa severidade (expõe stack .NET + namespace
    interno). Aceitável no escopo do desafio (é o `ValidationProblemDetails` nativo
    pedido e o campo errado é nomeado), mas seria sanitizado em produção. Ver S8.
12. **Duplicação da fonte de verdade de validação (DRY).** Os limites de tamanho
    aparecem em três lugares: data annotations nos DTOs de criação, re-checagem
    manual no `Validate()` dos DTOs de atualização, e o `openapi.yaml`. Mudar um
    limite exige editar os três → risco de drift. Extrair para constantes/uma
    fonte única reduziria a manutenção. Baixo impacto no MVP; dívida de manutenção.
13. **DTO de apresentação vazando na camada de aplicação.** `ProjectService`/
    `TaskService` retornam `ProjectResponse`/`TaskResponse` diretamente. A camada
    de serviço passa a conhecer o contrato de saída — um leak leve de
    responsabilidade. Alternativa: services retornam entidades/DTOs de domínio e o
    mapeamento para o response fica no controller. Escolha pragmática defensável
    (menos cerimônia no MVP), mantida conscientemente.

---

## 4. Melhorias — Segurança

| # | Item | Impacto | Esforço | Nota |
|---|------|---------|---------|------|
| S1 | **Autenticação** (JWT/OAuth) + **autorização** com dono de recurso | Alto | Alto | Base de tudo; define ownership de projeto/tarefa |
| S2 | **Rate limiting** (`AddRateLimiter`, nativo .NET 8) | Alto | Baixo | **Quick win** contra abuso/DoS |
| S3 | **Limite de tamanho do corpo** (`MaxRequestBodySize`) + validar `Content-Type` | Médio | Baixo | Reduz superfície de payload gigante |
| S4 | **Security headers** (HSTS, X-Content-Type-Options, etc.) | Médio | Baixo | Quick win; `UseHsts` + middleware |
| S5 | **CORS** explícito (allowlist de origens) se houver cliente browser | Médio | Baixo | Hoje é same-origin por omissão |
| S6 | **Logging de segurança/auditoria** de mutações (quem alterou o quê) | Médio | Médio | Depende de S1 para "quem" |
| S7 | **Não aplicar migrations automaticamente** em produção | Médio | Baixo | Ver I2 (também é infra) |
| S8 | **Sanitizar mensagens de validação** (não expor nome de tipo .NET nos 400) | Baixo | Baixo | Info disclosure; hoje o `Disallow` vaza `TaskFlow.Api.Dtos.*` (ver §3.11) |

Já **mitigado**: SQL injection (LINQ parametrizado), over-posting (DTOs +
`Disallow` + `completedAt` read-only), enums/tamanhos validados, 500 não vaza
stack em produção (Developer Page só em Development). **Ressalva:** o 400 de
validação ainda expõe o nome do tipo interno — ver S8 / §3.11.

---

## 5. Melhorias — Infraestrutura

| # | Item | Impacto | Esforço | Nota |
|---|------|---------|---------|------|
| I1 | **Health checks** (`/health` liveness/readiness) | Alto | Baixo | **Quick win**; essencial para orquestração |
| I2 | **Migrations fora do startup** (CLI/step de deploy ou gate) | Alto | Médio | Remove o anti-padrão do boot |
| I3 | **Logging estruturado + tracing** (Serilog / OpenTelemetry) | Alto | Médio | Operabilidade e diagnóstico |
| I4 | **Dockerfile** + `.dockerignore` | Médio | Baixo | Portabilidade e deploy |
| I5 | **CI (GitHub Actions)**: build + `dotnet test` em cada push/PR | Alto | Baixo | **Quick win**; protege a suíte |
| I6 | **Provider de banco por ambiente** (SQLite dev, Postgres/SQL Server prod) | Médio | Médio | Já quase pronto (EF Core) |
| I7 | **Configuração/segredos** por ambiente (env vars, user-secrets) | Médio | Baixo | Connection string fora do appsettings |
| I8 | **Versionamento de API** (`/api/v1`) | Baixo | Médio | Documentado como futuro |

---

## 6. Melhorias — Performance

| # | Item | Impacto | Esforço | Nota |
|---|------|---------|---------|------|
| P1 | **Paginação** nas listagens (limit/offset ou cursor) | Alto | Médio | Também é segurança (evita dump total) |
| P2 | **Projeção na query** (`Select` para DTO no EF) em vez de carregar entidade e mapear em memória | Médio | Baixo | **Quick win**; menos alocação |
| P3 | **Índices compostos** para filtros frequentes (`ProjectId+Status`, `ProjectId+Priority`) | Médio | Baixo | Se o volume de tarefas por projeto crescer |
| P4 | **Response/Output caching** nos GET | Médio | Médio | Ganha em leitura pesada; cuidado com invalidação |
| P5 | **`CountAsync`/streaming** para grandes conjuntos | Baixo | Médio | Depende de P1 |

Já **bom**: `AsNoTracking()` nas leituras, índice em `ProjectId`, DbContext
scoped, sem N+1 (respostas são planas).

---

## 7. Roadmap sugerido (incremental)

**Rodada 1 — Quick wins (impacto alto, esforço baixo):**
- [ ] I5 CI (build+test) · [ ] I1 health checks · [ ] S2 rate limiting · [ ] S4 security headers · [ ] P2 projeção na query

**Rodada 2 — Fundamentos de operação:**
- [ ] I3 logging estruturado/tracing · [ ] I2 migrations fora do startup · [ ] I4 Docker

**Rodada 3 — Escala e robustez:**
- [ ] P1 paginação · [ ] S1 autenticação + ownership · [ ] I6 Postgres · [ ] S6 auditoria

**Rodada 4 — Refinos:**
- [ ] S3/S5 hardening · [ ] P3/P4 índices e cache · [ ] concorrência otimista (item 6 da §3) ·
  [ ] `TimeProvider` (item 7 da §3)

> Sugestão: cada rodada = um branch/PR pequeno, com testes, seguindo a mesma
> disciplina de commits do projeto.

---

## 8. Revisão crítica e caça a defeitos

Revisão rigorosa em todos os eixos (arquitetura, banco, código, erros, testes,
segurança, performance, infra), incluindo ataque à API rodando com entradas em
branco/inválidas, over-posting, enums numéricos, JSON malformado e chaves
duplicadas (happy e sad paths). Nenhum defeito crítico ou alto; nenhuma regra de
negócio incorreta; build limpo e suíte verde.

**Corrigido:**
- `servers.url` do contrato apontava para porta inexistente → porta real (`5192`/`7054`).
- 404/422 sem `type`/`traceId`, assimétricos com o 400 → uniformizados via `IProblemDetailsService` + `CustomizeProblemDetails` (D16).
- Whitespace em `name`/`title` no PATCH passava → rejeitado (D15). Na criação, o `[Required]` já faz `Trim`.
- Enum numérico/fora do domínio (`priority: 1/99`) era aceito e persistido, violando o próprio contrato → rejeitado no corpo e na query (D17).
- Exemplos de erro do `openapi.yaml` mostravam `type: about:blank` → alinhados às URLs RFC reais.
- Suíte reforçada com sad paths (strings longas, over-posting, JSON malformado, tipo trocado, enum numérico/inválido, whitespace, `traceId`): **28 → 50 testes**.

**Mantido como dívida (fora do escopo do MVP):**
- Itens §3.9–3.13 e §4–§6: auth, paginação, rate limiting, observabilidade, DRY de validação, DTO nos services, info disclosure na validação, dois formatos de 404, projeção EF.
- Enum case-insensitive (`"LOW"` aceito) e chave JSON duplicada (last-wins; o pior efeito morreu com D17).
- **Invariante da regra 1 furável por transição lateral:** um projeto já `archived` pode ganhar tarefa `in_progress` avançando uma tarefa `pending` (arquiva → `PATCH pending→in_progress` → 200). Nenhuma das 5 regras do PDF proíbe — é omissão da spec, não bug. Fechar exigiria uma "regra 6" (decisão de spec, não de código).
