# Branch layout alternatives

Since 0.26.3, Settings → Branches offers one layout dropdown: **Sugiyama**
(default), **Legacy** (the 0.26.2 normal layout), and **Alternative: stronger
crossing reduction**. The branch sidebar no longer contains separate layout
checkboxes. Stored pre-0.26.3 checkbox fields remain readable but the new explicit
choice controls placement. **Stronger crossing reduction** adds a
bounded search followed by fixed-order centering. The 0.26.2 coordinate pass
projects a whole column with a minimum-gap constraint, so a neighbour can move
with the node it previously blocked. It retains the best centering candidate
without increasing the measured crossing count. Node ordering stays unchanged
in that coordinate pass.

**Sugiyama layout** is the new default. Its implementation uses the existing
DAG ranks, normalizes longer edges with temporary dummy vertices, alternates
median and barycentre layer sweeps, then assigns non-overlapping coordinates.
Only a candidate with fewer measured crossings, or equal crossings and better
centering, replaces the starting geometry. Geometry has its own bounded cache.

Reference: Eiglsperger, Siebenhaller and Kaufmann, [An Efficient Implementation
of Sugiyama's Algorithm for Layered Graph Drawing](https://jgaa-v5.cs.brown.edu/index.php/jgaa/article/view/paper111),
JGAA 9(3), 305–325 (2005), DOI 10.7155/jgaa.00111.

This is a bounded implementation of the Sugiyama framework, **not** a reproduction
of that paper's sparse-container data structure or its asymptotic guarantee.
Normalization is capped at 50,000 temporary vertices and a graph-relative limit;
oversized proposals retain the existing drawing. Sweeps have a time budget.
Scores use the application's existing straight-centreline intersection metric;
the renderer uses curves, so this is not a proof that every visible curve crossing
has been minimized. There is no promise of a globally optimal drawing.

Local 0.26.2 example checks (machine/load dependent):

| Example | Vertices | Normal → Sugiyama crossings | Optional layout build |
| --- | ---: | ---: | ---: |
| Pirc | 521 | 40 → 27 | 0.68 s |
| English | 1,009 | 633 → 553 | 3.26 s |

The stronger alternative remains useful: its centering error (sum of parent
distance from the midpoint of its children) fell from approximately 9,859 to
4,801 for Pirc and 71,430 to 51,828 for English in the same bounded check. Its
crossing counts did not increase. These are comparative observations, not timing
guarantees or a recommendation that one layout is best for every repertoire.
