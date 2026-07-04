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
