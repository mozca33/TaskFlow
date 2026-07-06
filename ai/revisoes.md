# Revisões da IA

Registro do que foi **revisado, corrigido ou rejeitado** das sugestões da IA — o
ponto central do fluxo AI-First: dirigir a ferramenta com senso crítico.

Cada entrada segue o mesmo padrão, curto: **Sugestão** → **Revisão** → **Decisão**.

---

## R1 — Interação entre as regras 1, 2 e 5

- **Sugestão:** IA descreveu as regras de negócio de forma isolada no `decisoes.md`.
- **Revisão:** notei que regra 1 + D1 + regra 2 se combinam. Para arquivar um projeto com tarefa `in_progress`, as saídas "retroceder" (D1) e "excluir" (regra 2) são proibidas — só concluir (`→ done`) resta.
- **Decisão:** consequência consistente das regras, não contradição. Pedi explicitar → adicionada a §4.1 ao `decisoes.md`.

## R2 — Enums persistidos como string

- **Sugestão:** gravar os enums como string no banco, em vez do inteiro ordinal padrão do EF Core.
- **Revisão:** questionei o motivo. Razões: resistência à reordenação (int corromperia dados), legibilidade em queries e consistência com o contrato (JSON já usa string).
- **Decisão:** aceito. Ganho de integridade/manutenção supera o custo marginal. Mantido `HasConversion<string>()`.

## R3 — Validação da especificação

- **Sugestão:** IA finalizou o `decisoes.md`.
- **Revisão:** li o documento inteiro conferindo consistência, completude e aderência ao enunciado.
- **Decisão:** aprovado sem alterações (etapa "Validar a spec" do SDD). Processo dirigido por mim em fases, documento por documento, spec antes do código.

## R4 — Nomenclatura da entidade de tarefa

- **Sugestão:** IA modelaria a entidade como classe `Task`, espelhando o schema `Task` do contrato.
- **Revisão:** `Task` colide com `System.Threading.Tasks.Task` e poluiria todo o código async (`Task<T>`) com ambiguidade. O nome da classe não afeta o JSON (usa nome de propriedade) nem a rota (`/tarefas`).
- **Decisão:** classe C# renomeada para `TaskItem`; contrato e rota preservados. Consistência de wire mantida, ergonomia do código preservada.
- **Correção posterior:** apliquei a regra só à classe e não aos enums; o build acusou `TaskStatus` colidindo com `System.Threading.Tasks.TaskStatus`. Renomeei os enums para `TaskItemStatus`/`TaskItemPriority` (uniformes com `TaskItem`). Lição: a decisão de nomenclatura vale para todo o namespace do domínio, não só a entidade raiz.

## R5 — Divergência de limite em `description` (spec × persistência)

- **Sugestão:** IA configurou `HasMaxLength(2000)` em `description` na camada de persistência.
- **Revisão:** revisando a camada, notei que o `openapi.yaml` declarava `description` sem limite — divergência spec × código. Num fluxo SDD a spec é a fonte da verdade; o código não pode impor restrição não documentada.
- **Decisão:** limite de 2000 é razoável (evita texto ilimitado), então **alinhei a spec ao código**: adicionei `maxLength: 2000` no `openapi.yaml` (Project, Task e DTOs) e nas tabelas do `decisoes.md`. A validação de entrada (DTO) refletirá o mesmo limite → 400.

## R6 — Distinção "ausente" × "null explícito" no PATCH

- **Sugestão:** IA propôs o atalho de MVP — tratar `null` como "inalterado", sem distinguir campo ausente de campo enviado como `null`.
- **Revisão:** rejeitei o atalho. PATCH parcial deve separar os dois: ausente = inalterado; `null` explícito = limpar (só onde o campo aceita null). `null` em campo obrigatório deve dar 400.
- **Decisão:** implementado `Optional<T>` com conversor que só dispara quando a propriedade existe no corpo. Verificado com teste dos três estados antes de seguir.

## R7 — Serialização de enum no wire

- **Sugestão:** IA implementou a serialização de enum como string snake_case (`JsonStringEnumConverter` + `SnakeCaseLower`), apontando que o padrão do .NET emitiria número (`1`).
- **Revisão:** conferi contra o contrato — os valores exigidos são string minúscula/snake_case (`in_progress`), e a conversão precisa valer nos dois sentidos (serializar e desserializar).
- **Decisão:** configuração mantida. A verificação no wire real fica para os testes da Etapa 3.

## R8 — Rejeição de campos desconhecidos no corpo

- **Sugestão:** IA implementou `[JsonUnmappedMemberHandling(Disallow)]` nos DTOs de entrada para honrar o `additionalProperties: false` do contrato.
- **Revisão:** reforcei o requisito de que o 400 resultante deve **nomear o campo** desconhecido (ex.: typo `naem`), não devolver um erro genérico.
- **Decisão:** mantido `Disallow`; a resposta de validação identifica o campo estranho.

## R9 — Timestamps perdendo o marcador UTC

- **Sugestão:** IA implementou a persistência sem tratar o `DateTimeKind` na leitura do SQLite.
- **Revisão:** na verificação de wire dos endpoints, notei que `createdAt`/`completedAt` vinham com `Z` na criação, mas **sem `Z`** após ler do banco (o SQLite devolve `Kind=Unspecified`) — inconsistente com o contrato (D9: UTC).
- **Decisão:** `UtcDateTimeConverter` aplicado por convenção a todo `DateTime`, marcando as datas lidas como UTC. Confirmado no wire: todos os timestamps voltam com `Z`.

## R10 — Revisão crítica geral do sistema

- **Revisão:** revisão adversarial completa (arquitetura, banco, erros, testes, segurança, performance, infra), questionando cada escolha e cada regra.
- **Corrigi:** `servers.url` do contrato (porta inexistente); 404/422 sem `type` → uniformizados via `IProblemDetailsService` ([D16](../docs/decisoes.md)); whitespace em `name`/`title` no PATCH → rejeitado ([D15](../docs/decisoes.md)) — na criação o `[Required]` já faz `Trim`, então descartei uma validação redundante; lacunas de teste (404 de listagem, enum inválido no corpo, no-op D1).
- **Decisão:** correções de aderência aplicadas e testadas. Duplicação de validação e mapeamento entidade→response nos services ficam como dívida (`../docs/analise.md`), não implementados — disciplina de escopo > gold-plating.

## R11 — Enum numérico furando o contrato

- **Achado:** `JsonStringEnumConverter` sem `allowIntegerValues: false` → `priority: 1/99/-5` era aceito e **persistido**, com a API violando o próprio `openapi.yaml`; filtro `?priority=99` idem. Os testes não pegavam (só enviavam dados válidos).
- **Revisão:** a correção de "uma linha" fechava só o corpo; a query ainda parseava `"99"` — descobri **rodando os testes**. Endureci o `WireEnumModelBinder` (rejeita string numérica + `Enum.IsDefined`).
- **Decisão:** [D17](../docs/decisoes.md) + testes em corpo e query, verificado ao vivo. Enum case-insensitive (`"LOW"`) e chave JSON duplicada ficam como dívida.

## R12 — `traceId` alegado, mas ausente em 404/422

- **Achado:** D16 afirmava `traceId` em todos os erros, mas só o 400 o trazia — a doc dizia o que o código não entregava (a mesma classe do `servers.url`).
- **Decisão:** corrigi o **código**, não a doc: `CustomizeProblemDetails` injeta `traceId` em 400/404/422 (+ teste, verificado ao vivo). Alinhei também os exemplos `about:blank` → URLs RFC no `openapi.yaml`.
- **Rejeitei:** bloquear transição de tarefa em projeto arquivado — não está nas 5 regras do PDF; fica como observação documentada, não código.
