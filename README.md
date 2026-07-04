# TaskFlow — Módulo de Projetos e Tarefas

API REST em **ASP.NET Core (.NET 8)** para o módulo de **Projetos e Tarefas** da
plataforma de gerenciamento de tarefas colaborativas _TaskFlow_.

Este repositório é um desafio técnico conduzido sob a metodologia
**Specification-Driven Development (SDD)**: toda feature começa por uma
especificação clara e acordada, a spec é a fonte da verdade, não o contrário.

> **Status:** em construção. O histórico de commits é intencional e reflete o
> fluxo SDD, na ordem **especificar → implementar → validar**.

## Metodologia (SDD)

O trabalho segue quatro fases, refletidas no histórico de commits:

| Fase | Etapa SDD              | Entregável                                                                    |
| ---- | ---------------------- | ----------------------------------------------------------------------------- |
| 0    | Scaffold               | Estrutura do repositório, convenções                                          |
| 1    | **Especificar**        | `openapi.yaml` (contrato), `docs/decisoes.md`, registro de uso de IA em `ai/` |
| 2    | **Implementar**        | API guiada pela spec (Controllers, Services, Entidades)                       |
| 3    | **Garantir aderência** | Testes de contrato (xUnit + `WebApplicationFactory`)                          |

A especificação é commitada **antes** do código de implementação. Basta ler o
`git log` para verificar essa ordem.

## Stack

- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core + SQLite
- Respostas de erro em `ProblemDetails` / `ValidationProblemDetails` (RFC 7807)
- Testes: xUnit + `WebApplicationFactory`

## Estrutura do repositório (planejada)

```
TaskFlow/
├── openapi.yaml                 # Contrato da API (Fase 1)
├── docs/
│   └── decisoes.md              # Decisões de design (ADR)
├── ai/                          # Registro de uso de IA
│   ├── skills.md
│   ├── prompts.md
│   └── revisoes.md
├── src/
│   └── TaskFlow.Api/            # Implementação (Fase 2)
└── tests/
    └── TaskFlow.ContractTests/  # Testes de contrato (Fase 3)
```

## Pré-requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download) (desenvolvido com 8.0.x)

## Como rodar

> Disponível a partir da Fase 2. Comando planejado:

```bash
dotnet run --project src/TaskFlow.Api
```

## Como executar os testes de contrato

> Disponível a partir da Fase 3. Comando planejado:

```bash
dotnet test tests/TaskFlow.ContractTests
```

## Convenção de commits

[Conventional Commits](https://www.conventionalcommits.org/) — `tipo(escopo): descrição`
(tipo/escopo em inglês, descrição em português). Cada commit representa uma
unidade lógica coerente.
