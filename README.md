# TaskFlow — Módulo de Projetos e Tarefas

API REST em **ASP.NET Core (.NET 8)** para o módulo de **Projetos e Tarefas** da
plataforma de gerenciamento de tarefas colaborativas _TaskFlow_.

Este repositório é um desafio técnico conduzido sob a metodologia
**Specification-Driven Development (SDD)**: toda feature começa por uma
especificação clara e acordada, e a spec é a fonte da verdade — não o contrário.

## Metodologia (SDD)

O trabalho segue quatro fases, refletidas no histórico de commits — a
especificação é commitada **antes** do código. Basta ler o `git log` para
verificar essa ordem (`especificar → implementar → validar`).

| Etapa SDD              | Entregável                                                                    |
| ---------------------- | ----------------------------------------------------------------------------- |
| **Especificar**        | `openapi.yaml` (contrato), `docs/decisoes.md`, registro de uso de IA em `ai/` |
| **Implementar**        | API guiada pela spec (Controllers, Services, Domain, Persistence)             |
| **Garantir aderência** | Testes de contrato (xUnit + `WebApplicationFactory`)                          |

O registro do uso de IA fica em `ai/` — em especial `ai/revisoes.md`, que
documenta o que foi revisado, corrigido ou rejeitado das sugestões da IA.

## Stack

- .NET 8 (ASP.NET Core Web API, controllers)
- Entity Framework Core + SQLite (migrations aplicadas na inicialização)
- Erros em `ProblemDetails` / `ValidationProblemDetails` (RFC 7807)
- Testes: xUnit + `WebApplicationFactory` (SQLite in-memory)

## Pré-requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download) (desenvolvido com 8.0.4xx)

## Como rodar

```bash
dotnet run --project src/TaskFlow.Api
```

A API sobe em `http://localhost:5192` (ver `Properties/launchSettings.json`), cria
o arquivo `taskflow.db` (SQLite) e aplica as migrations automaticamente. Em
ambiente de desenvolvimento, a documentação Swagger fica em `/swagger`.

## Como executar os testes de contrato

```bash
dotnet test tests/TaskFlow.ContractTests
```

Os testes sobem a API em memória (SQLite in-memory) e validam o contrato:
criação de recursos, as 5 regras de negócio (retornos `422`), recursos
inexistentes (`404`), validação de entrada (`400`: campos obrigatórios, tamanhos,
enums, campos desconhecidos), filtros, a máquina de estados das tarefas, e a
conformidade dos responses com o schema do `openapi.yaml` (NSwag/NJsonSchema).

## Endpoints

| Método   | Rota                        | Descrição                                    |
| -------- | --------------------------- | -------------------------------------------- |
| `POST`   | `/projetos`                 | Criar projeto                                |
| `GET`    | `/projetos`                 | Listar projetos (filtro `?status=`)          |
| `GET`    | `/projetos/{id}`            | Buscar projeto por ID                        |
| `PATCH`  | `/projetos/{id}`            | Atualizar projeto (nome, descrição, status)  |
| `POST`   | `/projetos/{id}/tarefas`    | Criar tarefa no projeto                      |
| `GET`    | `/projetos/{id}/tarefas`    | Listar tarefas (filtros `?status=&priority=`)|
| `PATCH`  | `/tarefas/{id}`             | Atualizar tarefa (título, descrição, status, prioridade) |
| `DELETE` | `/tarefas/{id}`             | Excluir tarefa                               |

Enums trafegam como string minúscula no contrato: `active`/`archived`,
`pending`/`in_progress`/`done`, `low`/`medium`/`high`.

## Regras de negócio

1. **Arquivar projeto** só é permitido se nenhuma tarefa estiver `in_progress` → senão `422`.
2. **Excluir tarefa** só é permitido se ela estiver `pending` → senão `422`.
3. **Concluir tarefa** (`status=done`) preenche `completedAt` automaticamente (nunca manual).
4. **Projeto arquivado** não aceita novas tarefas → `422`.
5. **Transição de status** estrita: `pending → in_progress → done`, sem pular nem retroceder → senão `422`.

As decisões de design por trás dessas regras estão em [`docs/decisoes.md`](docs/decisoes.md);
o contrato completo, em [`openapi.yaml`](openapi.yaml).

## Análise crítica e próximos passos

O escopo entregue é o mínimo definido no desafio. Uma avaliação honesta do que
ficou **deliberadamente fora** (autenticação, paginação, rate limiting,
observabilidade) e um roadmap de melhorias priorizado por impacto × esforço estão
em [`docs/analise.md`](docs/analise.md). O resumo de fronteira de escopo também
está em [`docs/decisoes.md` §7](docs/decisoes.md).

## Estrutura do repositório

```
TaskFlow/
├── openapi.yaml                 # Contrato da API (OpenAPI 3.0)
├── docs/
│   ├── decisoes.md              # Decisões de design (ADR)
│   └── analise.md               # Análise crítica e roadmap de melhorias
├── ai/                          # Registro de uso de IA
│   ├── skills.md
│   ├── prompts.md
│   └── revisoes.md
├── src/
│   └── TaskFlow.Api/
│       ├── Controllers/         # Adaptam HTTP ↔ casos de uso
│       ├── Services/            # Regras de negócio (as 5 restrições)
│       ├── Domain/              # Entidades e enums
│       ├── Dtos/                # Contratos de request/response
│       ├── Persistence/         # DbContext, mapeamento, migrations
│       ├── Errors/              # Exceções de domínio → ProblemDetails
│       └── Binding/             # Model binder de enum (query em snake_case)
└── tests/
    └── TaskFlow.ContractTests/  # Testes de contrato (xUnit)
```

## Convenção de commits

[Conventional Commits](https://www.conventionalcommits.org/) — `tipo(escopo): descrição`
(tipo/escopo em inglês, descrição em português). Cada commit representa uma
unidade lógica coerente, e a ordem prova o fluxo spec-first.

## Navegação para IA e manutenção

Este repositório inclui um [`AGENTS.md`](AGENTS.md) com um mapa do projeto —
fontes da verdade, estrutura do código e convenções — para orientar assistentes
de IA e novos desenvolvedores a encontrar cada coisa em segundos.
