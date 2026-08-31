/* ============================================================================
   EMERGENCE — presentation layer, P1: THE INTERVAL REPORT  (v0.3, 2026-08-31, D-605/D-613)
   A PURE READ over a world state S. Never writes to S. Never draws randomness.
   Loaded AFTER emergence-engine.js by every host (Jint, node bake-runner, browser).
   NOT part of the engine SHA — carries its own SHA (P1-text golden: seed+interval => byte-identical).
   Spec: 20-DESIGN/PRESENTATIONSLAGRET-SPEC-2026-08-29.md §P1 (D-574) + STORY-AND-STATS-DESIGN A5/A6.
   Design law: the story surface reads the ENGINE LOG (S.events), never a projection (KROPP F1).
   ============================================================================ */
(function (root) {
  'use strict';

  // ---- dramatic pressure per event type (P1-spec: death > fall > guild > product > epithet) ----
  // Types observed on v17 (D-604 probe). Unknown types weigh 1. Routine noise weighs 0 (never reported).
  var WEIGHT = {
    extinct: 100, villageFallen: 90, fall: 90, war: 80, raid: 70, famine: 70, plague: 65,
    death: 60, aggregate: 55, city: 55, guild: 50, leader: 40, tribute: 38, steal: 30, legend: 30,
    epithet: 22, tech: 28, product: 26, conversion: 20, customLost: 20, custom: 14, hut: 8, field: 6,
    journey: 4, hoard: 3, moved: 2, quirk: 2, failed: 1, imitated: 1, observed: 1,
    taught: 0, star: 0, child: 0, season: 0, hunt: 0
  };

  // ---- deterministic helpers ----
  function stripHtml(s) { return String(s || '').replace(/<[^>]+>/g, ''); }
  function firstSentence(s) {
    s = stripHtml(s).trim();
    // drop leading emoji/symbol run
    s = s.replace(/^[^A-Za-z0-9"']+/, '');
    var m = s.match(/^(.+?[.!?])(\s|$)/);
    return (m ? m[1] : s).trim();
  }
  function cmpEvent(a, b) { // weight desc, then year asc, then id asc — total order, no ties
    var wa = WEIGHT[a.type] === undefined ? 1 : WEIGHT[a.type];
    var wb = WEIGHT[b.type] === undefined ? 1 : WEIGHT[b.type];
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
    if (a.epithet) return a.name + ' ' + a.epithet;
    var v = null;
    if (a.village !== undefined && S.villages) {
      for (var i = 0; i < S.villages.length; i++) if (S.villages[i] && S.villages[i].id === a.village) { v = S.villages[i]; break; }
    }
    if (v && v.name) return a.name + ' of ' + v.name;
    if (a.bornYear !== undefined) return a.name + ' (born ' + a.bornYear + ')';
    if (a.born !== undefined) return a.name + ' (born ' + Math.floor(a.born / 144) + ')';
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
  function villageName(S, id) {
    if (S.villages) for (var i = 0; i < S.villages.length; i++) if (S.villages[i] && S.villages[i].id === id) return S.villages[i].name || ('settlement #' + id);
    return null;
  }
  function renderLine(ev, idx, S) {
    if (ev.type === 'aggregate') {
      // engine emits this line in Swedish (D-605 motor debt); the report speaks English.
      var m = String(ev.txt || '').match(/^(?:<b>)?([^<—]+?)(?:<\/b>)? har växt.*?(\d+) själar/);
      var vn = (m ? m[1] : (villageName(S, ev.village) || 'A settlement')).replace(/^[^A-Za-z0-9]+/, '').trim();
      var n = m ? m[2] : '';
      return vn + ' has grown past the single gaze — ' + (n ? n + ' souls now live as a people' : 'its people now live as a crowd') + '; the chronicle keeps only the names that mattered.';
    }
    return disambiguate(firstSentence(ev.txt), ev, idx, S);
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
      var w = WEIGHT[e.type] === undefined ? 1 : WEIGHT[e.type];
      if (w <= 0) continue;
      pool.push(e);
    }
    pool.sort(cmpEvent);
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
      lines.push({ year: ev.year, type: ev.type, weight: WEIGHT[ev.type] === undefined ? 1 : WEIGHT[ev.type], text: text, why: whyChain(ev, evIdx, idx, S, 0) });
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

  root.EmergencePresentation = { VERSION: '0.3.0', writeIntervalReport: writeIntervalReport, reportDigest: reportDigest, WEIGHT: WEIGHT, _firstSentence: firstSentence };
})(typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this));
