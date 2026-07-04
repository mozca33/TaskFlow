# Decisões de Design — TaskFlow (Projetos e Tarefas)

Este documento registra as decisões de arquitetura e design tomadas **antes** da
implementação, seguindo Specification-Driven Development. Cada decisão abaixo é
refletida no contrato `openapi.yaml` e coberta pelos testes de contrato.

> Convenção: cada decisão recebe um identificador (`D1`…`D11`) referenciado nos
> commits e nos testes.

---

## 1. Contexto e objetivo

API REST para o módulo de **Projetos** e **Tarefas** do TaskFlow. Um projeto
agrupa tarefas; cada tarefa pertence a exatamente um projeto. O escopo é o
conjunto mínimo de endpoints e regras de negócio definido no desafio — nada além
(ver §7, Fora de escopo).

---

## 2. Estrutura de dados

### Entidades

**Projeto**

| Campo | Tipo | Regras |
|-------|------|--------|
| `id` | UUID (Guid) | Gerado pelo servidor |
| `name` | string | Obrigatório, ≤ 100 caracteres |
| `description` | string? | Opcional |
| `status` | enum `active` \| `archived` | Default `active` |
| `createdAt` | datetime (UTC) | Gerado pelo servidor |

**Tarefa**

| Campo | Tipo | Regras |
|-------|------|--------|
| `id` | UUID (Guid) | Gerado pelo servidor |
| `title` | string | Obrigatório, ≤ 200 caracteres |
| `description` | string? | Opcional |
| `status` | enum `pending` \| `in_progress` \| `done` | Default `pending` |
| `priority` | enum `low` \| `medium` \| `high` | Obrigatório |
| `createdAt` | datetime (UTC) | Gerado pelo servidor |
| `completedAt` | datetime? (UTC) | Gerado pelo servidor ao concluir (ver D4) |
| `projectId` | UUID (Guid) | Referência ao projeto pai |

Relacionamento: **1 Projeto → N Tarefas**. A tarefa é criada sempre no contexto
de um projeto (`POST /projetos/{id}/tarefas`).

### Persistência (D8)

- **Entity Framework Core** com provider **SQLite** (arquivo local), versionado
  por migrations.
- SQLite persiste entre execuções e demonstra o uso real de EF (migrations,
  mapeamento). O provider in-memory é aceito pelo desafio e fica documentado
  como alternativa de configuração, mas não é o padrão adotado.
- Os enums são persistidos como **string** (não como inteiro), para que o banco
  seja legível e resistente a reordenação de valores.

---

## 3. Modelo de tratamento de erros (D5)

Todos os erros usam os tipos nativos do ASP.NET Core, no formato **RFC 7807**:

| Situação | Status | Corpo |
|----------|--------|-------|
| Validação de entrada: campo obrigatório ausente, tamanho excedido, enum inválido, JSON malformado | **400** | `ValidationProblemDetails` |
| Recurso inexistente (projeto/tarefa não encontrado) | **404** | `ProblemDetails` |
| Violação de regra de negócio (regras 1–5) | **422** | `ProblemDetails` com `detail` explicativo |

- **400 vs 422:** 400 significa "a requisição está malformada" (o cliente errou o
  formato); 422 significa "a requisição está bem formada, mas viola uma regra de
  negócio no estado atual". Essa separação é o que o desafio pede ao exigir 422
  para as restrições de negócio.
- **UUID malformado na rota (D6):** tratado como **404** via route constraint
  `{id:guid}` do ASP.NET Core. Um id que não é um Guid válido não identifica
  nenhum recurso, então "não encontrado" é a resposta consistente e evita código
  de validação manual.
- Toda resposta 422 traz uma mensagem (`detail`) explicando qual regra foi
  violada, conforme exigido pelo enunciado.

---

## 4. Regras de negócio

As cinco restrições do desafio e como cada uma se manifesta na API:

| # | Regra | Efeito na API |
|---|-------|---------------|
| 1 | Arquivar projeto exige que **nenhuma** tarefa esteja `in_progress` | `PATCH /projetos/{id}` com `status=archived` → **422** se houver tarefa `in_progress` |
| 2 | Excluir tarefa só se `status=pending` | `DELETE /tarefas/{id}` em tarefa `in_progress`/`done` → **422** |
| 3 | Concluir preenche `completedAt` automaticamente | `PATCH /tarefas/{id}` com `status=done` → servidor grava `completedAt = agora (UTC)` |
| 4 | Não criar tarefa em projeto `archived` | `POST /projetos/{id}/tarefas` em projeto arquivado → **422** |
| 5 | Transição de status estrita | Ver D1 |

### 4.1 Interação entre regras (arquivamento × ciclo de vida da tarefa)

As regras 1, 2 e 5 se compõem numa restrição não-óbvia, mas consistente. Para
arquivar um projeto que possui uma tarefa `in_progress`, essa tarefa precisa
deixar o estado `in_progress` — e há apenas **uma** saída permitida:

| Saída | Permitido? | Por quê |
|-------|-----------|---------|
| Retroceder `in_progress → pending` | ❌ | Regra 5 / D1 proíbe retroceder |
| Excluir a tarefa | ❌ | Regra 2 só permite excluir tarefa `pending` |
| Concluir `in_progress → done` | ✅ | Única transição válida a partir de `in_progress` |

Consequência: **só é possível arquivar um projeto com tarefa `in_progress` após
concluir essa tarefa.** Tarefas `pending` e `done` não bloqueiam o arquivamento;
apenas `in_progress` bloqueia. Isso é uma consequência lógica e determinística
das regras — não uma contradição — e é implementado exatamente assim.

---

## 5. Decisões pontuais

- **D1 — Transição de status da tarefa (estrita).** O fluxo é
  `pending → in_progress → done`, avançando **um passo por vez**. Não é permitido
  pular etapa (`pending → done`) nem retroceder. Qualquer transição fora dessa
  sequência retorna **422**. É a leitura literal de "deve seguir o fluxo".

- **D2 — Desarquivar projeto (permitido).** `PATCH` de `archived → active` é
  aceito. Nenhuma regra do desafio proíbe reativar um projeto; apenas o ato de
  *arquivar* tem pré-condição (regra 1). Reativar é uma transição livre.

- **D3 — Criação de tarefa nasce `pending`.** O corpo de `POST .../tarefas`
  aceita apenas `title`, `description` e `priority`. O `status` inicial é sempre
  `pending`; qualquer mudança de status ocorre exclusivamente via `PATCH`, sob a
  regra D1. Isso mantém um único ponto de controle da máquina de estados.

- **D4 — `completedAt` é read-only.** Nunca é aceito no corpo de requisições (nem
  na criação, nem no PATCH). É gravado pelo servidor exatamente quando o status
  passa a `done`, com o timestamp atual em UTC. Enviar `completedAt` no corpo é
  ignorado (o campo não faz parte dos DTOs de entrada).

- **D7 — Semântica de PATCH (parcial).** Um campo omitido no corpo permanece
  inalterado. Campos atualizáveis — Projeto: `name`, `description`, `status`;
  Tarefa: `title`, `description`, `status`, `priority`.

- **D9 — Serialização.** Nomes de campo em camelCase, iguais às tabelas do
  desafio. Enums serializados como string minúscula: `active`/`archived`,
  `pending`/`in_progress`/`done`, `low`/`medium`/`high`. Timestamps em ISO 8601
  (UTC).

- **D10 — Listagens.** Retornam um array JSON puro (sem envelope de paginação no
  MVP). Filtros por query string: `GET /projetos?status=`,
  `GET /projetos/{id}/tarefas?status=&priority=`.

- **D11 — Rotas e versionamento.** Caminhos exatamente como no enunciado
  (`/projetos`, `/tarefas`), sem prefixo de versão. Versionamento fica para
  próximos passos.

---

## 6. Organização do código (Clean Code / SOLID)

Separação de responsabilidades em camadas, cada uma com um propósito único:

```
src/TaskFlow.Api/
├── Controllers/        # Adaptam HTTP ↔ casos de uso; sem regra de negócio
├── Services/           # Regras de negócio (arquivamento, transições, exclusão)
├── Domain/             # Entidades e enums
├── Dtos/               # Contratos de request/response (entrada/saída)
├── Persistence/        # DbContext, configurações de mapeamento, migrations
└── Errors/             # Exceções de domínio → mapeadas para ProblemDetails
```

- **Controllers** só orquestram: validam o shape (via model binding/data
  annotations → 400 automático) e delegam ao serviço.
- **Services** concentram as regras de negócio (as cinco restrições e a máquina
  de estados D1). São a fonte das respostas 422.
- Regras de negócio violadas são sinalizadas por **exceções de domínio**
  traduzidas para `ProblemDetails` 422 por um ponto central (middleware/filtro),
  evitando `if` de erro espalhado pelos controllers.

---

## 7. Fora de escopo (próximos passos)

Deliberadamente **não** implementado no MVP, para manter o foco no que o desafio
avalia. Documentado como evolução natural:

- Autenticação/autorização e usuários (ownership de projetos/tarefas).
- Paginação, ordenação e busca textual nas listagens.
- Soft-delete, `updatedAt` e trilha de auditoria.
- Versionamento de API (`/api/v1`).
- Observabilidade (logs estruturados, métricas, health checks) e containerização
  (Docker) / CI.
