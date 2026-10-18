# Opening JSON format

Place one JSON file per repertoire in the Openings folder. Reload the library after editing. Use a unique stable id and perspective of white or black. At each nonterminal learner position there must be exactly one prescribed move; opponent positions may have multiple replies. All moves must be legal UCI strings (e2e4, g8f6, e7e8q), not SAN.

## Recommended format: schema version 2

A directed acyclic graph shares transpositions without duplicating continuations:

    {
      "schemaVersion": 2,
      "id": "example_black_reply",
      "name": "A reply to e4",
      "perspective": "black",
      "description": "A small demonstration.",
      "source": "Author / research sources and date",
      "rootNodeId": "n0",
      "nodes": {
        "n0": { "moves": [{ "uci": "e2e4", "nextId": "n1" }] },
        "n1": { "moves": [{ "uci": "e7e5", "nextId": "n2" }] },
        "n2": { "moves": [] }
      }
    }

startFen is optional; its absence means the standard starting position. Each node may include id, fen and comment. Moves may include name and explanation. nextId must refer to an existing node; multiple incoming moves must reach the same legal position, including side to move, castling and relevant en-passant rights. Cycles are rejected. An empty moves array marks a branch endpoint, not necessarily a terminal chess position.

Keep opening and node IDs stable after learning begins. Progress keys use openingId/node/nodeId/uci. isSubbranch and subbranchColor are optional visual metadata. humanUnfriendly on a move is an optional shallow-engine plausibility label, not permission to insert an illegal move.

## Earlier author-friendly formats

Schema version 1 accepts a nested tree with moves containing uci and next objects. Alternatively, lines contains objects with name, description and a moves array of complete UCI sequences from startFen. Identical prefixes are combined. Do not supply conflicting learner responses or mix representations ambiguously.

The two bundled examples are generated version-2 graphs in Samples/Openings in the source tree; public builds place them in Openings. Do not treat their entire generated content as an instruction prompt.

## Authoring

Validate JSON, legal moves, references and learner consistency before sharing. Use concise original explanations and cite research sources; do not copy paid annotations or database dumps. IDs are technical identifiers, not personal names. See AI_RESEARCH_BRIEF.md.
