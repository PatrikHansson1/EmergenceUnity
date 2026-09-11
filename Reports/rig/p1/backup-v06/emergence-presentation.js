/* ============================================================================
   EMERGENCE — presentation layer, P1: THE INTERVAL REPORT  (v0.6, 2026-09-01, D-605/D-613/D-615/D-626/D-648)
   A PURE READ over a world state S. Never writes to S. Never draws randomness.
   Loaded AFTER emergence-engine.js by every host (Jint, node bake-runner, browser).
   NOT part of the engine SHA — carries its own SHA (P1-text golden: seed+interval => byte-identical).
   Spec: 20-DESIGN/PRESENTATIONSLAGRET-SPEC-2026-08-29.md §P1 (D-574) + STORY-AND-STATS-DESIGN A5/A6.
   Design law: the story surface reads the ENGINE LOG (S.events), never a projection (KROPP F1).
   ============================================================================ */
(function (root) {
  'use strict';

  // ---- dramatic pressure per event type (P1-spec: death > fall > guild > product > epithet) ----
  // v0.5 (D-626): keyed to the ENGINE'S actual 48 ev-types (grep over v19 8907f6f6) — v0.4 carried 8 dead keys
  // (villageFallen/fall/war/famine/plague/city/guild/extinct) and 21 real types fell to default 1.
  // Unknown future types weigh 1. Routine noise weighs 0 (never reported).
  var WEIGHT = {
    end: 100, start: 85, violence: 75, raid: 70, feud: 62, death: 60, wolfAttack: 58, sickness: 56,
    aggregate: 55, village: 55, rebel: 52, reformation: 50, religion: 48, tabooBroken: 46,
    knowledgeLost: 45, leader: 40, tribute: 38, tradition: 34, rediscovered: 32, trade: 30,
    steal: 30, legend: 30, tech: 28, product: 26, mutation: 24, epithet: 22, conversion: 20,
    customLost: 20, normFades: 18, customBack: 16, mourn: 15, custom: 14, giftway: 12,
    hut: 8, field: 6, journey: 4, hoard: 3, moved: 2, quirk: 2, failed: 1, imitated: 1, observed: 1,
    sharing: 1, taught: 0, star: 0, child: 0, season: 0, hunt: 0
  };

  // ---- deterministic helpers ----
  function stripHtml(s) { return String(s || '').replace(/<[^>]+>/g, ''); }
  // v0.6 (D-648): some engine lines carry their meaning in the SECOND sentence ("X is gone. But someone still …",
  // "… until the end. No one else ever took it up.") — those types keep two sentences.
  var SENTENCES = { legend: 2, customLost: 2 };
  function firstSentence(s, n) {
    n = n || 1;
    s = stripHtml(s).trim();
    // drop leading emoji/symbol run
    s = s.replace(/^[^A-Za-z0-9"']+/, '');
    var out = '', rest = s;
    for (var k = 0; k < n; k++) {
      var m = rest.match(/^(.+?[.!?])(\s+|$)([\s\S]*)$/);
      if (!m) { out += rest; rest = ''; break; }
      out += (k ? ' ' : '') + m[1]; rest = m[3];
      if (!rest) break;
    }
    return out.trim();
  }
  // v0.6 (D-648): a craft lost in ONE village while the world still knows it is local news (20);
  // the world's last knowledge dying ("With Embla died the last knowledge of …") keeps 45.
  var _wS = null;
  function weightOf(e, S) {
    var w = WEIGHT[e.type] === undefined ? 1 : WEIGHT[e.type];
    if (e.type === 'knowledgeLost' && e.village && e.tech && S && S.knowledge && S.knowledge[e.tech] && S.knowledge[e.tech].status === 'alive') w = 20;
    return w;
  }
  function cmpEvent(a, b) { // weight desc, then year asc, then id asc — total order, no ties
    var wa = weightOf(a, _wS);
    var wb = weightOf(b, _wS);
    if (wa !== wb) return wb - wa;
    if (a.year !== b.year) return a.year - b.year;
    return (a.id || 0) - (b.id || 0);
  }

  // ---- name disambiguation (D-604: Loke x9 on seed 97013) ----
  // Two living-or-dead souls sharing a base name inside the interval get a deterministic tag
  // from their own record: epithet if any, else home village, else "born <year>".
  function buildNameIndex(S) {
    var byId = {}, byName = {};
    var agents = S.agents || [];
    for (var i = 0; i < agents.length; i++) {
      var a = agents[i]; if (!a || a.name === undefined) continue;
      byId[a.id] = a;
      var base = String(a.name).replace(/ (II|III|IV|V|VI|VII|VIII|IX|X)$/, '');
      (byName[base] = byName[base] || []).push(a);
    }
    return { byId: byId, byName: byName };
  }
  function tagFor(a, S) {
    // v0.6 (D-648): epithet > home village > age. Never a birth year (a.born is not a year for later-born souls — "born 0").
    if (a.epithet) return a.name + ' ' + a.epithet;
    var v = null;
    if (a._vil && a._vil.name) v = a._vil;
    else if (a.village !== undefined && S.villages) {
      for (var i = 0; i < S.villages.length; i++) if (S.villages[i] && S.villages[i].id === a.village) { v = S.villages[i]; break; }
    }
    if (v && v.name) return a.name + ' of ' + v.name;
    if (a.age !== undefined) return a.name + ', aged ' + Math.floor(a.age);
    return a.name + ' #' + a.id;
  }
  function disambiguate(text, ev, idx, S) {
    // replaces the FIRST occurrence of an ambiguous name mentioned via ev.agent / ev.victim
    var ids = [];
    if (ev.agent !== undefined) ids.push(ev.agent);
    if (ev.victim !== undefined) ids.push(ev.victim);
    for (var k = 0; k < ids.length; k++) {
      var a = idx.byId[ids[k]]; if (!a) continue;
      var base = String(a.name).replace(/ (II|III|IV|V|VI|VII|VIII|IX|X)$/, '');
      var same = idx.byName[base] || [];
      if (same.length > 1 && text.indexOf(a.name) >= 0) {
        text = text.replace(a.name, tagFor(a, S));
      }
    }
    return text;
  }

  // ---- causal chain (A3): walk causes[] -> "because ..." fragments, depth-limited ----
  function buildEventIndex(events) { var m = {}; for (var i = 0; i < events.length; i++) m[events[i].id] = events[i]; return m; }
  function whyChain(ev, evIdx, idx, S, depth) {
    depth = depth || 0;
    if (!ev.causes || !ev.causes.length || depth > 2) return [];
    var out = [];
    for (var i = 0; i < ev.causes.length && out.length < 2; i++) {
      var c = String(ev.causes[i]); var kind = c.split(':')[0]; var ref = c.slice(kind.length + 1);
      if (kind === 'ev') {
        var p = evIdx[Number(ref)];
        if (p && p !== ev) out.push({ year: p.year, text: firstSentence(p.txt), sub: whyChain(p, evIdx, idx, S, depth + 1) });
      } else if (kind === 'cause') {
        out.push({ year: ev.year, text: CAUSE_WORDS[ref] || ref, sub: [] });
      }
      // agent:N refs are actors, not causes — skipped in the why-chain by design
    }
    return out;
  }

  // ---- line rendering: engine txt by default; own English template where the engine text is not shippable ----
  function renderLine(ev, idx, S) {
    // v0.4 (D-615): the engine speaks English since v18 (D-614) — the aggregate mask is retired; every line
    // comes from the engine log through the same first-sentence + disambiguation path.
    return disambiguate(firstSentence(ev.txt, SENTENCES[ev.type] || 1), ev, idx, S);
  }
  var CAUSE_WORDS = { age: 'of old age', hunger: 'of hunger', cold: 'of cold', sickness: 'of sickness', war: 'in war', raid: 'in a raid', thirst: 'of thirst' };

  // ---- THE INTERVAL REPORT ----
  // Returns { lines:[{year,type,text,weight,why:[...]}], header, text }
  // lines: 3–5 (fewer if the interval is quiet), weighted by dramatic pressure, then chronological.
  function writeIntervalReport(S, y0, y1, opts) {
    opts = opts || {};
    var maxLines = opts.maxLines || 5, minLines = opts.minLines || 3;
    var events = S.events || [];
    var idx = buildNameIndex(S), evIdx = buildEventIndex(events);
    var pool = [];
    for (var i = 0; i < events.length; i++) {
      var e = events[i];
      if (e.year < y0 || e.year > y1) continue;
      var w = weightOf(e, S);
      if (w <= 0) continue;
      pool.push(e);
    }
    _wS = S; pool.sort(cmpEvent); _wS = null;
    // pick: top by weight, but never two of the same type unless nothing else remains (variety law)
    // variety law, two keys: never two of the same TYPE while an unseen type remains; never a third line
    // about the same ACTOR (ev.agent) while a line about someone else remains (D-612: three Torv-lines).
    var picked = [], seenType = {}, actorCount = {};
    function actorOf(e) { return e.agent === undefined ? null : e.agent; }
    function hasOther(p, pred) { for (var q = p + 1; q < pool.length; q++) if (pred(pool[q])) return true; return false; }
    for (var p = 0; p < pool.length && picked.length < maxLines; p++) {
      var e0 = pool[p], t = e0.type, a0 = actorOf(e0);
      if (seenType[t] && hasOther(p, function (x) { return !seenType[x.type]; })) continue;
      if (a0 !== null && (actorCount[a0] || 0) >= 2 && hasOther(p, function (x) { var ax = actorOf(x); return ax === null || (actorCount[ax] || 0) < 2; })) continue;
      picked.push(e0); seenType[t] = true; if (a0 !== null) actorCount[a0] = (actorCount[a0] || 0) + 1;
    }
    picked.sort(function (a, b) { return a.year !== b.year ? a.year - b.year : (a.id || 0) - (b.id || 0); });
    var lines = [];
    for (var k = 0; k < picked.length; k++) {
      var ev = picked[k];
      var text = renderLine(ev, idx, S);
      lines.push({ year: ev.year, type: ev.type, weight: weightOf(ev, S), text: text, why: whyChain(ev, evIdx, idx, S, 0) });
    }
    var header = 'Years ' + y0 + '–' + y1 + (lines.length ? ':' : ': a quiet span. Nothing the chronicle kept.');
    var body = lines.map(function (l) { return '[' + l.year + '] ' + l.text; }).join('\n');
    return { y0: y0, y1: y1, header: header, lines: lines, text: header + (body ? '\n' + body : '') };
  }

  // ---- P1-TEXT GOLDEN: canonical digest of reports over fixed intervals ----
  function reportDigest(S, step) {
    step = step || 100;
    var lastYear = 0; var ev = S.events || [];
    for (var i = 0; i < ev.length; i++) if (ev[i].year > lastYear) lastYear = ev[i].year;
    var parts = [];
    for (var y = 0; y <= lastYear; y += step) parts.push(writeIntervalReport(S, y, y + step - 1).text);
    return parts.join('\n\n');
  }

  root.EmergencePresentation = { VERSION: '0.6.0', writeIntervalReport: writeIntervalReport, reportDigest: reportDigest, WEIGHT: WEIGHT, _firstSentence: firstSentence };
})(typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this));
