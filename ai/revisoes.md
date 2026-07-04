# Revisões da IA

Registro do que foi **revisado, corrigido ou rejeitado** das sugestões da IA
durante o desenvolvimento. Este é o registro mais importante do fluxo AI-First:
mostra a direção crítica sobre o que a ferramenta produziu, não apenas o uso.

Cada entrada descreve: o que a IA sugeriu, o que eu (revisor) questionei ou
alterei, e a decisão final.

---

## R1 — Interação entre as regras 1, 2 e 5 (arquivamento × ciclo de vida)

**Contexto:** a IA redigiu o `docs/decisoes.md` com a decisão D1 (transição de
status estrita) e a tabela das regras de negócio de forma isolada.

**O que eu levantei na revisão:** ao ler a regra 1 (não arquivar projeto com
tarefa `in_progress`) junto da D1 (não retroceder status) e da regra 2 (só
excluir tarefa `pending`), percebi que as três se combinam numa restrição que
não estava explícita: se um projeto tem uma tarefa `in_progress`, **como** eu
consigo arquivá-lo?

**Análise:** verifiquei as saídas possíveis para tirar a tarefa de `in_progress`:
- retroceder para `pending` → proibido pela D1;
- excluir a tarefa → proibido pela regra 2 (só exclui `pending`);
- concluir (`in_progress → done`) → **única saída válida**.

**Conclusão:** só é possível arquivar um projeto com tarefa `in_progress` depois
de **concluir** essa tarefa. Confirmei que isso é uma consequência lógica e
consistente das regras (não uma contradição).

**Ação:** pedi que a IA tornasse essa interação explícita. Foi adicionada a seção
§4.1 ao `docs/decisoes.md` documentando a composição das regras 1 × 2 × 5. A spec
ficou mais completa e o comportamento, inequívoco para a implementação e os
testes.

## R2 — Persistência de enums como string em vez de inteiro

**O que a IA propôs:** persistir os enums (`status`, `priority`) como **string**
no banco, em vez do inteiro ordinal padrão do EF Core.

**O que eu revisei:** questionei o motivo da escolha. A IA apresentou três
justificativas — (1) resistência à reordenação/inserção de valores no enum, que
com inteiro corromperia silenciosamente dados existentes; (2) legibilidade em
consultas e debug (`'in_progress'` vs `1`); (3) consistência com o contrato, que
já expõe os enums como string.

**Decisão:** aceitei os três pontos. O custo (espaço/índice marginalmente
maiores) é irrelevante nesta escala e o ganho em integridade de dados e
manutenção compensa. Mantido `HasConversion<string>()` no mapeamento EF.

## R3 — Validação completa da especificação

**Contexto:** concluída a redação do `docs/decisoes.md` pela IA (estrutura de
dados, modelo de erros, as 5 regras, decisões D1–D11, §4.1 e escopo).

**O que eu revisei:** li o documento inteiro conferindo consistência interna,
completude e aderência ao enunciado. Não encontrei contradições nem lacunas.

**Decisão:** spec aprovada sem alterações adicionais. Esta é a etapa "Validar a
spec" do fluxo SDD — a especificação passa a ser a fonte da verdade para a
implementação.

**Nota de processo:** eu (candidato) dirigi o fluxo em fases SDD
(**especificar → implementar → validar**), trabalhando **documento por
documento** e **feature por feature**, com a spec commitada **antes** do código e
commits limpos (Conventional Commits). A IA executa dentro dessas diretrizes; as
decisões de escopo e as validações são minhas.
