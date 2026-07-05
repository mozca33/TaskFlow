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

- **Sugestão:** manter a serialização JSON padrão do .NET.
- **Revisão:** o padrão emite enum como número (`1`); o contrato exige string snake_case (`in_progress`). Divergência que quebraria os testes de contrato.
- **Decisão:** `JsonStringEnumConverter` + `JsonNamingPolicy.SnakeCaseLower` (nativo do .NET 8), bidirecional. A verificação no wire real fica para os testes da Etapa 3.

## R8 — `priority` obrigatória vs default silencioso

- **Sugestão:** IA modelou `priority` como enum anulável + `[Required]` (ausência → 400).
- **Revisão:** cogitei tornar `Low` o default silencioso (toda tarefa tem prioridade). Mas isso contraria a tabela do enunciado (`priority` Obrigatório = Sim) e exigiria afrouxar o contrato.
- **Decisão:** mantida obrigatória, fiel ao enunciado. Fidelidade à spec acima da ergonomia — é o que o desafio avalia.

## R9 — Rejeição de campos desconhecidos no corpo

- **Sugestão:** aceitar o comportamento padrão do System.Text.Json, que ignora campos desconhecidos.
- **Revisão:** ignorar em silêncio deixa um typo (`{"naem": ...}`) passar como se `name` estivesse ausente, gerando erro confuso. O contrato declara `additionalProperties: false`.
- **Decisão:** `[JsonUnmappedMemberHandling(Disallow)]` nos DTOs de entrada — campo estranho → 400 nomeando o campo.

## R10 — Nunca expor entidades de domínio

- **Sugestão:** retornar as entidades diretamente nas respostas.
- **Revisão:** a resposta é a camada mais externa antes da tela; entidade vazaria a navegação (`Tasks`) e acoplaria o contrato ao EF. Fronteira de segurança e de contrato.
- **Decisão:** respostas sempre via DTO (`ProjectResponse`/`TaskResponse`), expondo só os campos do schema.
