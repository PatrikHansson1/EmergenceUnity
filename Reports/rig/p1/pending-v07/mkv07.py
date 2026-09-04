#!/usr/bin/env python3
"""mkv07.py <presentation-v06-in> <presentation-v07-out>
P1 v0.7 (PENDING, vag C i TEXTPAKET-OMSPELNINGSPLAN-2026-09-03): four READ-SIDE text polishes,
no engine change, no replay. Fynd 11 (possessive+article), 12 (loss long after death), 13 (double
epithet -> comma), 14/9 (re-elected voice -> "listen again"). 3 anchors, each exactly once.
"""
import sys
src=open(sys.argv[1],encoding='utf-8').read()
n0=len(src)
def rep(anchor,new,label):
    global src
    c=src.count(anchor)
    assert c==1,f"{label}: anchor count {c}: {anchor[:80]!r}"
    src=src.replace(anchor,new)
    print(f"ok {label}")

# A1: header line
rep("P1: THE INTERVAL REPORT  (v0.6, 2026-09-01, D-605/D-613/D-615/D-626/D-648)",
    "P1: THE INTERVAL REPORT  (v0.7 PENDING, 2026-09-03, +fynd 11/12/13/14-9 som render-fixar, vag C)","header")

# A2: polish() + wiring in renderLine
A="""  // ---- line rendering: engine txt by default; own English template where the engine text is not shippable ----
  function renderLine(ev, idx, S) {
    // v0.4 (D-615): the engine speaks English since v18 (D-614) — the aggregate mask is retired; every line
    // comes from the engine log through the same first-sentence + disambiguation path.
    return disambiguate(firstSentence(ev.txt, SENTENCES[ev.type] || 1), ev, idx, S);
  }"""
B="""  // ---- v0.7 (PENDING): four read-side polishes over the engine line — deterministic, no writes ----
  var POSSESSIVE_TYPES = { tech: 1, rediscovered: 1, knowledgeLost: 1, taught: 1 };
  function polish(text, ev, S) {
    // fynd 11: knowledge names carrying an article after a possessive ("Ask's the sail" -> "Ask's sail")
    if (POSSESSIVE_TYPES[ev.type]) text = text.replace(/'s the /g, "'s ");
    // fynd 13: double epithet gets a comma ("Ask the First the Firebringer" -> "Ask the First, the Firebringer")
    text = text.replace(/(\\bthe [A-Z][A-Za-z-]+) (the [A-Z])/, '$1, $2');
    // fynd 12: "With X died the last knowledge of Y" long after X's death promises a simultaneity the data
    // does not have — when the loss came more than a year after the death, say it honestly.
    if (ev.type === 'knowledgeLost') {
      var m = text.match(/^With (.+?) died the last knowledge of (.+?)(?: \\(|\\.| —)/);
      if (m) {
        var who = m[1], what = m[2], dy = -1, evs = S.events || [];
        for (var i = (ev.id || 0) - 1; i >= 0; i--) {
          var p = evs[i]; if (!p) continue;
          if (p.type === 'death' && p.txt && String(p.txt).indexOf(who) >= 0) { dy = p.year; break; }
        }
        if (dy >= 0 && ev.year - dy > 1) {
          text = 'The last who knew ' + what + ', ' + who + ', is long gone — that knowledge is extinct until someone rediscovers it.';
        }
      }
    }
    // fynd 14/9: a village that has already recognized a voice listens AGAIN, not anew
    if (ev.type === 'leader' && text.indexOf('listen when') >= 0 && ev.village !== undefined) {
      var evs2 = S.events || [];
      for (var j = (ev.id || 0) - 1; j >= 0; j--) {
        var q = evs2[j]; if (!q) continue;
        if (q.type === 'leader' && q.village === ev.village && q.txt && String(q.txt).indexOf('listen when') >= 0) {
          text = text.replace('listen when', 'listen again when'); break;
        }
      }
    }
    return text;
  }

  // ---- line rendering: engine txt by default; own English template where the engine text is not shippable ----
  function renderLine(ev, idx, S) {
    // v0.4 (D-615): the engine speaks English since v18 (D-614) — the aggregate mask is retired; every line
    // comes from the engine log through the same first-sentence + disambiguation path.
    return polish(disambiguate(firstSentence(ev.txt, SENTENCES[ev.type] || 1), ev, idx, S), ev, S);
  }"""
rep(A,B,"polish")

# A3: version bump
rep("VERSION: '0.6.0',","VERSION: '0.7.0',","version")

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} chars)")
