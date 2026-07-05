# AGENTS.md

Guia de navegação deste repositório para agentes de IA (Claude, Copilot, Cursor,
Codex e afins) e para novos desenvolvedores. O objetivo é que qualquer pessoa —
ou assistente — entenda em segundos do que se trata e onde está cada coisa.

## O que é

API REST em **ASP.NET Core (.NET 8)** do módulo de **Projetos e Tarefas** do
TaskFlow, construída sob **Specification-Driven Development (SDD)**: a spec é a
fonte da verdade e é commitada antes do código.

## Fontes da verdade (leia primeiro)

| Arquivo                | O que contém                                                    |
| ---------------------- | --------------------------------------------------------------- |
| `openapi.yaml`         | Contrato da API (endpoints, schemas, erros). **Autoritativo.**  |
| `docs/decisoes.md`     | Decisões de design (D1–D14) com justificativa.                  |
| `ai/revisoes.md`       | O que foi revisado/corrigido/rejeitado das sugestões da IA.     |
| `ai/skills.md`         | Áreas delegadas à IA.                                           |
| `ai/prompts.md`        | Prompts principais e o que produziram.                          |

Se o código divergir da spec, a spec vence — ou a divergência deve ser
reconciliada e registrada em `docs/decisoes.md` / `ai/revisoes.md`.

## Mapa do código (`src/TaskFlow.Api/`)

| Pasta           | Responsabilidade                                                       |
| --------------- | --------------------------------------------------------------------- |
| `Controllers/`  | Adaptam HTTP ↔ casos de uso. Sem regra de negócio.                    |
| `Services/`     | **Regras de negócio** (as 5 restrições + máquina de estados).          |
| `Domain/`       | Entidades (`Project`, `TaskItem`) e enums.                             |
| `Dtos/`         | Contratos de entrada/saída. `Optional<T>` distingue ausente de null.  |
| `Persistence/`  | `DbContext`, mapeamento (`Configurations/`), migrations.              |
| `Errors/`       | Exceções de domínio + `DomainExceptionHandler` → ProblemDetails.      |
| `Binding/`      | Model binder que aceita enums em snake_case na query string.          |

Onde ficam as **5 regras de negócio**: `Services/ProjectService.cs` (regra 1) e
`Services/TaskService.cs` (regras 2–5). Erros viram `ProblemDetails` (422/404) em
`Errors/DomainExceptionHandler.cs`.

## Rodar e testar

```bash
dotnet run  --project src/TaskFlow.Api        # sobe a API em http://localhost:5192
dotnet test tests/TaskFlow.ContractTests      # testes de contrato (xUnit)
```

## Convenções

- **Commits:** Conventional Commits — `tipo(escopo): descrição` (tipo/escopo em
  inglês, descrição em português). Um commit = uma unidade lógica; a ordem prova
  o fluxo spec-first.
- **Enums no wire:** string minúscula/snake_case (`active`, `in_progress`, `low`).
- **Erros:** 400 (validação), 404 (inexistente / UUID malformado), 422 (regra de
  negócio, sempre com `detail`).
- **Timestamps:** UTC (sufixo `Z`).

## Ao alterar

1. Se mudar comportamento da API, atualize `openapi.yaml` **antes** do código.
2. Registre decisões relevantes em `docs/decisoes.md`; revisões de IA em `ai/revisoes.md`.
3. Rode `dotnet test` — os testes de contrato são a rede de segurança da aderência.
