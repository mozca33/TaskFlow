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
