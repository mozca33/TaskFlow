# Prompts principais

Registro dos prompts que mais moldaram a Etapa 1, e o que cada um produziu.
Ferramenta: **Claude (Claude Code)**. Os prompts foram conduzidos por mim em
fases — spec antes do código, um documento por vez. O que revisei/corrigi das
respostas está em [revisoes.md](revisoes.md).

---

## P1 — Enquadramento do desafio

- **Prompt (resumo):** apresentar o enunciado (entidades, 5 regras, 8 endpoints,
  stack .NET 8 + EF Core + SQLite, ProblemDetails, xUnit) e pedir um plano de
  execução em fases seguindo SDD, spec antes do código.
- **Produziu:** o plano em etapas (spec → validar → implementar → testar) e o
  scaffold do repositório (README, `.gitignore`, `.editorconfig`).

## P2 — Levantar as decisões de design antes de escrever

- **Prompt (resumo):** listar os pontos ambíguos do enunciado que precisam de
  decisão explícita antes de codar (transição de status, desarquivar, criação de
  tarefa, `completedAt`, tratamento de UUID malformado, semântica de PATCH).
- **Produziu:** a lista de questões que virou a base das decisões D1–D11. **Eu
  decidi cada uma** (ex.: transição estrita de 1 passo; desarquivar permitido;
  tarefa nasce sempre `pending`; `completedAt` read-only).

## P3 — Redigir `docs/decisoes.md`

- **Prompt (resumo):** consolidar as decisões travadas num documento de ADR
  enxuto, no formato `Dn — título. justificativa curta`, cobrindo estrutura de
  dados, tratamento de erros (400/404/422) e persistência.
- **Produziu:** o `decisoes.md`. Na revisão, pedi para **explicitar a interação
  entre as regras 1, 2 e 5** (§4.1) — ver R1 em `revisoes.md`.

## P4 — Gerar o `openapi.yaml`

- **Prompt (resumo):** gerar o contrato OpenAPI 3.0 fiel ao `decisoes.md`, com os
  8 endpoints, filtros nas listagens, os 6 status codes exigidos e exemplos de
  ProblemDetails / ValidationProblemDetails.
- **Produziu:** o `openapi.yaml` v1.0.0. Revisei endpoint por endpoint (inclusive
  com um visualizador renderizado) e conferi contra o PDF do enunciado antes do
  commit.

## P5 — Registrar o uso de IA (esta pasta)

- **Prompt (resumo):** produzir `ai/skills.md`, `ai/prompts.md` e `ai/revisoes.md`
  registrando honestamente o que foi delegado, os prompts e o que revisei/rejeitei.
- **Produziu:** os três arquivos desta pasta, mantidos curtos e factuais.
