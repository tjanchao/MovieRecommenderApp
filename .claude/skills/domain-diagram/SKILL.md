---
name: domain-diagram
description: >
  Builds a consolidated domain model at docs/domain.md — a Mermaid ER diagram of
  every entity and value object declared across all feature specs in docs/specs/,
  with their attributes, relationships, and a traceability table back to the
  source specs. Creates the file if missing, updates it in place if it exists.
  Slash-command only.
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(ls *)
---

# Domain Diagram Generator

You consolidate the per-spec domain models scattered across `docs/specs/` into a
single Mermaid ER diagram at `docs/domain.md`.

This is a **derivation task, not a design task**. The specs are the source of
truth. Do not invent entities, attributes, or relationships that no spec states,
and do not silently drop ones that are stated. If the specs contradict each
other, surface the contradiction in the output rather than picking a winner.

## Arguments

`$ARGUMENTS` is optional.

- **Empty** — regenerate from all specs in `docs/specs/`. This is the normal case.
- **A spec number or filename** (e.g. `005` or `005-006-007-008-live-movie-catalog.md`)
  — still read every spec, but treat that one as the reason for the refresh and
  call out in your summary what changed because of it.

## Procedure

### 1. Gather the sources

1. List `docs/specs/*.md`. If the directory doesn't exist or is empty, stop and
   tell the user there are no specs to derive a domain model from — do not write
   an empty `docs/domain.md`.
2. Read **every** spec file in full. Do not sample or skim — attributes and
   invariants live in the details.
3. From each spec, extract section **5. Domain Model**:
   - 5.1 Entities — name, description, and the full attribute table
   - 5.2 Relationships — every stated relationship, including ones pointing at
     entities owned by other specs
   - 5.3 Value Objects — name and attributes
   - 5.4 Domain Rules and Invariants — keep the ones that constrain *structure*
     (uniqueness, nullability, referential integrity, cardinality); ignore the
     purely behavioural ones
4. Also scan sections 3 (Functional Requirements) and 7 (Edge Cases) for
   entities or fields that are named there but never made it into section 5.
   These are gaps — record them, don't guess at them.
5. Read `docs/architecture/` if it has anything filled in about persistence or
   building blocks, so your naming doesn't contradict it.

### 2. Merge across specs

Specs are written independently and overlap. Resolve as follows:

- **Same entity in multiple specs** — merge into one node. Union the attributes.
  If two specs give the same attribute different types or constraints, keep both
  in the notes and flag the conflict in section "Open Issues".
- **Forward references** — spec 001 may say "a User has many Ratings" while
  `Rating` is only defined in a later spec. Include `Rating` as a node with
  whatever attributes exist by then, and mark it in the traceability table as
  defined by the later spec. If nothing defines it anywhere, include it as an
  attribute-less node and list it under "Open Issues" as forward-declared only.
- **Explicit non-entities** — when a spec states something is deliberately *not*
  an entity (e.g. "There is no Session entity"), do not add it to the diagram.
  Record it under "Deliberate Non-Entities" so a future reader doesn't re-add it.
- **Ubiquitous language wins** — never rename an entity to something you find
  more natural. Use the spec's exact name.

### 3. Write the Mermaid diagram

Use `erDiagram`. Constraints on the syntax — violating these breaks rendering:

- Entity names: `PascalCase`, no spaces, no quotes.
- Attribute lines are `type name KEY "comment"`. The type and name must each be a
  single token of `[A-Za-z0-9_]`. Normalize spec types:
  - `datetime?` / `string?` → drop the `?`, put `nullable` in the comment
  - `enum [a, b, c]` → type `enum`, put the allowed values in the comment
  - `UUID` → `UUID`, `integer` → `integer`, keep the rest as written
- Keys: `PK` for identity, `FK` for a reference to another entity, `UK` for a
  uniqueness constraint. An attribute may carry only one key token; if it's both
  FK and part of a unique constraint, use `FK` and say so in the comment.
- Put the essential constraint in the quoted comment (required, max length,
  default, unit) — keep it under ~60 chars. The full constraint text stays in the
  source spec; this diagram links to it, it doesn't replace it.
- Relationship lines are `EntityA <card>--<card> EntityB : "label"`, where the
  cardinality tokens are:
  - `||` exactly one, `|o` zero or one
  - `}|` one or more, `}o` zero or more
  Read the left token left-to-right and the right token right-to-left.
- Label every relationship with a verb phrase from the spec (`"rates"`,
  `"appears on"`), not with `"has"` everywhere.
- Many-to-many that the specs resolve through a join entity must be drawn as two
  one-to-many relationships through that entity, not as `}o--o{`.
- Value objects: if a value object is owned by exactly one entity, flatten it
  into that entity's attributes and note it. If more than one entity uses it,
  give it its own node and relate it with `||--||` labelled `"embeds"`.
- Use `%%` comment lines to group the diagram by spec or subdomain when there are
  more than ~8 entities.

### 4. Write `docs/domain.md`

The generated region is delimited so hand-written notes survive regeneration:

````markdown
# Domain Model

_Derived from the feature specs in `docs/specs/`. Do not edit the generated
region by hand — run `/domain-diagram` after changing a spec. Text outside the
markers is preserved._

<!-- BEGIN GENERATED: domain-diagram -->

## Entity Relationship Diagram

```mermaid
erDiagram
    ...
```

## Entities

| Entity | Description | Source Spec |
| ------ | ----------- | ----------- |
| User   | ...         | [001-002](specs/001-002-accounts-and-sessions.md) |

## Value Objects

| Value Object | Owned By | Source Spec |
| ------------ | -------- | ----------- |

## Structural Invariants

_Cross-entity rules that the diagram cannot express — uniqueness across columns,
conditional nullability, ordering, cascade behaviour._

- **[Rule]** — statement, followed by the spec it comes from.

## Deliberate Non-Entities

| Concept | Why not an entity | Source Spec |
| ------- | ----------------- | ----------- |

## Open Issues

_Conflicts between specs, forward-declared entities, and entities named in
requirements but never modelled. Empty is the goal._

- ...

<!-- END GENERATED: domain-diagram -->
````

Omit a section entirely if it would be empty, except **Open Issues** — always
render it, with `_None._` when clean, so its absence never reads as "not checked".

### 5. Update vs. create

1. Check whether `docs/domain.md` exists.
2. **If it does not exist** — create `docs/` if needed and write the full file.
3. **If it exists** — read it first. Replace only the text between
   `<!-- BEGIN GENERATED: domain-diagram -->` and `<!-- END GENERATED: domain-diagram -->`,
   leaving everything above and below untouched. Use `Edit` for this, not `Write`.
4. **If it exists but has no markers** — it was hand-written before this skill
   existed. Show the user what you'd generate, ask whether to replace the file or
   append the generated region, and do not overwrite without an answer.

### 6. Report

After writing, tell the user in a few lines:

- the path written, and whether it was created or updated
- entity count, relationship count, and which specs contributed
- what changed since the previous version, if you replaced an existing region
- any Open Issues, stated plainly — these are the reason someone reads this file

Never claim the diagram renders unless you've checked the syntax rules in step 3
against every line you wrote.
