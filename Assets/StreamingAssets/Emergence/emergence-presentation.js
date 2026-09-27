/* ============================================================================
   EMERGENCE — presentation layer  v0.9 (2026-09-26, D-876): P1 interval report · P2 era band · P4 self-stop · P3 predictions
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
    end: 100, start: 85, violence: 75, raid: 70, feud: 62, death: 25, wolfAttack: 58, sickness: 56,   // v0.8 (D-767): death 60->25 — FS1 fix, §12a READ-weight; an old-age death is background, not the turning point of every window
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

  // ---- v0.7 (PENDING): four read-side polishes over the engine line — deterministic, no writes ----
  var POSSESSIVE_TYPES = { tech: 1, rediscovered: 1, knowledgeLost: 1, taught: 1 };
  function polish(text, ev, S) {
    // fynd 11: knowledge names carrying an article after a possessive ("Ask's the sail" -> "Ask's sail")
    if (POSSESSIVE_TYPES[ev.type]) text = text.replace(/'s the /g, "'s ");
    // fynd 13: double epithet gets a comma ("Ask the First the Firebringer" -> "Ask the First, the Firebringer")
    text = text.replace(/(\bthe [A-Z][A-Za-z-]+) (the [A-Z])/, '$1, $2');
    // fynd 12: "With X died the last knowledge of Y" long after X's death promises a simultaneity the data
    // does not have — when the loss came more than a year after the death, say it honestly.
    if (ev.type === 'knowledgeLost') {
      var m = text.match(/^With (.+?) died the last knowledge of (.+?)(?: \(|\.| —)/);
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

  // ============================================================================
  // v0.9 (D-876): P2 ERA BAND · P4 SELF-STOP · P3 PREDICTIONS — pure reads (PRESENTATIONSLAGRET-SPEC §P2/§P4/§P3).
  // None of these touch P1 (writeIntervalReport/reportDigest stay byte-identical — P1-golden neutral).
  // Engine tables (TECH, ERAS) are taken from an explicit `E` argument or root.Emergence (Jint/browser host).
  // ============================================================================
  function engineOf(E) { return E || root.Emergence || null; }
  function techEra(E, id) { var t = E && E.TECH ? E.TECH[id] : null; return t && t.era !== undefined ? t.era : 0; }
  function eraNameOf(E, era) {
    if (E && typeof E.eraName === 'function') return E.eraName(era);
    var names = E && E.ERAS ? E.ERAS : []; era = era | 0;
    return names[era < 0 ? 0 : (era >= names.length ? names.length - 1 : era)] || ('Era ' + era);
  }
  function lastYearOf(S) { var y = 0, ev = S.events || []; for (var i = 0; i < ev.length; i++) if (ev[i].year > y) y = ev[i].year; return y; }
  function livingAgents(S) { var out = [], ag = S.agents || []; for (var i = 0; i < ag.length; i++) if (ag[i] && !ag[i].dead) out.push(ag[i]); return out; }
  function villageNameOf(a, S) {
    if (a._vil && a._vil.name) return a._vil.name;
    if (a.village !== undefined && S.villages) for (var i = 0; i < S.villages.length; i++) if (S.villages[i] && S.villages[i].id === a.village) return S.villages[i].name;
    return null;
  }
  // era KNOWN at the end of year y, reconstructed from the engine log: a technology counts from its birth
  // (knowledge.yearBorn) until a WORLD-level 'knowledgeLost' (no village field = the last knowledge died) and again
  // from a world-level 'rediscovered'. NOTE (measured, D-876): this is the era the WORLD knows — guild/city-held
  // knowledge included — which runs ahead of the engine's worldEra(S) (what LIVING souls know): 97013 reaches the
  // Press at 203 here vs 232 by living souls, and living-soul eras can fall (272→Mills, 288→Press) while the world's
  // record keeps the craft. The host may pass opts.eraSamples = [{year, era}] sampled from worldEra(S) at each jump;
  // when present they override this reconstruction for the band's colour.
  function eraByYear(S, E) {
    var kn = S.knowledge || {}, ids = Object.keys(kn), events = S.events || [], lastYear = lastYearOf(S), marks = [];
    for (var i = 0; i < ids.length; i++) { var k = kn[ids[i]]; if (!k || k.yearBorn === undefined) continue; marks.push({ year: k.yearBorn, id: i, tech: ids[i], on: true }); }
    for (var j = 0; j < events.length; j++) {
      var e = events[j];
      if (e.village !== undefined || e.tech === undefined) continue;
      if (e.type === 'knowledgeLost') marks.push({ year: e.year, id: 100000 + j, tech: e.tech, on: false });
      else if (e.type === 'rediscovered') marks.push({ year: e.year, id: 100000 + j, tech: e.tech, on: true });
    }
    marks.sort(function (a, b) { return a.year !== b.year ? a.year - b.year : a.id - b.id; });
    var perYear = [], alive = {}, m = 0;
    for (var y = 0; y <= lastYear; y++) {
      while (m < marks.length && marks[m].year <= y) { alive[marks[m].tech] = marks[m].on; m++; }
      var er = 0; for (var t in alive) if (alive[t]) { var te = techEra(E, t); if (te > er) er = te; }
      perYear[y] = er;
    }
    return function (yy) { yy = yy | 0; if (yy < 0) yy = 0; if (yy > lastYear) yy = lastYear; return perYear[yy] || 0; };
  }

  // ---- P2 · THE ERA BAND ----
  // Returns { decades:[{y0,y1,count,pressure,era,eraName}], eras:[{era,name,fromYear}], bookmarks:[{year,kind,text,evId}] }
  // decades: event count (weight>0) + summed dramatic pressure per <step> years; era = era reached by the decade's last year.
  // bookmarks (spec): first village, first city, first guild-technology, extinction, plus the chronicle's own turning points.
  var BOOKMARK_KINDS = { start: 'dawn', village: 'firstVillage', end: 'extinction', reformation: 'reformation', religion: 'faith' };
  function eraBand(S, E, opts) {
    E = engineOf(E); opts = opts || {};
    var step = opts.step || 10, events = S.events || [], lastYear = Math.max(lastYearOf(S), opts.untilYear || 0);
    var eraAt = eraByYear(S, E);
    if (opts.eraSamples && opts.eraSamples.length) {
      var smp = opts.eraSamples.slice().sort(function (a, b) { return a.year - b.year; });
      eraAt = function (y) { var er = smp[0].era; for (var q = 0; q < smp.length && smp[q].year <= y; q++) er = smp[q].era; return er; };
    }
    var decades = [], n = Math.floor((Math.max(lastYear, 1) - 1) / step) + 1;
    for (var d = 0; d < n; d++) decades.push({ y0: d * step + 1, y1: (d + 1) * step, count: 0, pressure: 0, era: 0, eraName: '' });
    for (var i = 0; i < events.length; i++) {
      var e = events[i], w = weightOf(e, S); if (w <= 0) continue;
      var di = Math.floor((e.year - 1) / step); if (di < 0 || di >= n) continue;
      decades[di].count++; decades[di].pressure += w;
    }
    for (var k = 0; k < n; k++) { decades[k].era = eraAt(decades[k].y1); decades[k].eraName = eraNameOf(E, decades[k].era); }
    var eras = [], prevEra = -1;
    for (var y = 1; y <= lastYear; y++) { var er = eraAt(y); if (er !== prevEra) { eras.push({ era: er, name: eraNameOf(E, er), fromYear: y }); prevEra = er; } }
    var bookmarks = [], seen = {};
    for (var j = 0; j < events.length; j++) {
      var ev = events[j], kind = null;
      if (BOOKMARK_KINDS[ev.type] && !seen[ev.type]) { kind = BOOKMARK_KINDS[ev.type]; seen[ev.type] = true; }
      else if (ev.type === 'aggregate' && !seen.city && String(ev.txt || '').indexOf('grown past') >= 0) { kind = 'firstCity'; seen.city = true; }
      else if (ev.type === 'tech' && ev.guild !== undefined && !seen.guild) { kind = 'firstGuildTech'; seen.guild = true; }
      else if (ev.type === 'tech' && techEra(E, ev.tech) >= 7 && !seen.high) { kind = 'ageOfMachines'; seen.high = true; }
      else if (ev.type === 'knowledgeLost' && weightOf(ev, S) >= 45) kind = 'knowledgeExtinct';
      else if (ev.type === 'violence' || ev.type === 'raid') { if (!seen[ev.type + 'First']) { kind = ev.type === 'raid' ? 'firstRaid' : 'firstBlood'; seen[ev.type + 'First'] = true; } }
      if (kind) bookmarks.push({ year: ev.year, kind: kind, text: firstSentence(ev.txt), evId: ev.id });
    }
    return { step: step, lastYear: lastYear, decades: decades, eras: eras, bookmarks: bookmarks };
  }

  // ---- P4 · SELF-STOP ----
  // snapshot(S,E) -> a small plain object the host keeps between jumps; selfStop(S, prev, E) -> { stop, reasons:[{kind,text}], snapshot }
  // Thresholds live HERE (presentation), spec: extinction risk (pop < 15), a city falls, guild breakthrough (era >= 7), an era shift.
  var STOP = { minPop: 15, cityMinSouls: 30, machineEra: 7 };
  // population = living souls + the folded people of each aggregate (g.cohorts[0..3], the engine's own count at line
  // ~1365) — otherwise a fold ("grown past the single gaze") would read as a die-off (measured 4242: 55 → 18 at y97).
  function aggregateSouls(g) { var c = g && g.cohorts ? g.cohorts : [], n = 0; for (var i = 0; i < 4 && i < c.length; i++) n += c[i] || 0; return Math.round(n); }
  function snapshot(S, E) {
    E = engineOf(E);
    var living = livingAgents(S), byVillage = {}, aggs = S.aggregates || [], pop = living.length;
    for (var i = 0; i < living.length; i++) { var v = villageNameOf(living[i], S); if (v) byVillage[v] = (byVillage[v] || 0) + 1; }
    for (var g = 0; g < aggs.length; g++) { var n = aggregateSouls(aggs[g]); pop += n; if (aggs[g].village) byVillage[aggs[g].village] = (byVillage[aggs[g].village] || 0) + n; }
    var era = 0; if (E && typeof E.worldEra === 'function') era = E.worldEra(S); else { var f = eraByYear(S, E); era = f(lastYearOf(S)); }
    return { year: lastYearOf(S), pop: pop, era: era, eventCount: (S.events || []).length, villages: byVillage };
  }
  function selfStop(S, prev, E) {
    E = engineOf(E);
    var now = snapshot(S, E), reasons = [];
    // extinction risk = a FALL below the threshold (the first morning has four souls — that is dawn, not danger)
    if (prev && now.pop < STOP.minPop && prev.pop >= STOP.minPop) reasons.push({ kind: 'extinctionRisk', text: 'Only ' + now.pop + ' souls remain.' });
    if (prev) {
      var names = Object.keys(prev.villages).sort();
      for (var i = 0; i < names.length; i++) {
        var was = prev.villages[names[i]], is = now.villages[names[i]] || 0;
        if (was >= STOP.cityMinSouls && is === 0) reasons.push({ kind: 'cityFell', text: names[i] + ' stands empty — ' + was + ' souls lived there.' });
      }
      var events = S.events || [];
      for (var j = prev.eventCount; j < events.length; j++) {
        var ev = events[j];
        if (ev.type === 'tech' && ev.guild !== undefined && techEra(E, ev.tech) >= STOP.machineEra) reasons.push({ kind: 'guildBreakthrough', text: firstSentence(ev.txt), evId: ev.id });
        if (ev.type === 'end') reasons.push({ kind: 'extinction', text: firstSentence(ev.txt), evId: ev.id });
      }
      if (now.era !== prev.era) reasons.push({ kind: now.era > prev.era ? 'eraRises' : 'eraFalls', text: (now.era > prev.era ? 'The world enters ' : 'The world falls back to ') + eraNameOf(E, now.era) + '.' });
    }
    return { stop: reasons.length > 0, reasons: reasons, snapshot: now };
  }

  // ---- P3 · PREDICTIONS (v1: three templates — tech-before-year / village-endures / line-survives) ----
  // offer(S,E,opts) -> [{id,kind,text,subject,byYear,madeYear}] ; judge(S,pred,E) -> 'hit' | 'miss' | 'open'
  // JOURNAL LAW: the host judges every open prediction at every jump and KEEPS the first non-'open' verdict.
  // A verdict is never re-judged later — the aggregate layer folds crowds into a people (D-388, "grown past the
  // single gaze") and individual descent is gone by design after a fold, so a line that was alive at its byYear
  // is a hit even if the chronicle can no longer trace its souls afterwards. Measured 97013: y80 hit, y100 untraceable.
  var HORIZON = 100;
  function lineOf(a, S) { // deterministic ancestor id via parents[0] name → the eldest candidate of the previous generation
    var ag = S.agents || [], cur = a, guard = 0;
    while (cur && cur.parents && cur.parents.length && guard++ < 64) {
      var pname = cur.parents[0], next = null;
      for (var i = 0; i < ag.length; i++) { var c = ag[i]; if (c && c.name === pname && c.gen < cur.gen && c.id < cur.id && (!next || c.gen > next.gen)) next = c; }
      if (!next) break; cur = next;
    }
    return cur ? cur.id : a.id;
  }
  function offer(S, E, opts) {
    E = engineOf(E); opts = opts || {};
    var year = lastYearOf(S), by = year + (opts.horizon || HORIZON), out = [], madeYear = year;
    // (a) technology before year: the next unknown techs whose prerequisites are all alive, ordered by era then id
    var kn = S.knowledge || {}, cands = [];
    if (E && E.TECHS) for (var i = 0; i < E.TECHS.length; i++) {
      var t = E.TECHS[i]; if (!t || kn[t.id]) continue;
      var pre = t.pre || [], ok = true; for (var p = 0; p < pre.length; p++) if (!kn[pre[p]] || kn[pre[p]].status !== 'alive') { ok = false; break; }
      if (ok) cands.push(t);
    }
    cands.sort(function (a, b) { return (a.era || 0) !== (b.era || 0) ? (a.era || 0) - (b.era || 0) : (a.id < b.id ? -1 : 1); });
    for (var c = 0; c < cands.length && c < 3; c++) out.push({ id: 'tech:' + cands[c].id + ':' + by, kind: 'techBefore', subject: cands[c].id, text: 'They will reach ' + cands[c].base.toLowerCase() + ' before year ' + by + '.', byYear: by, madeYear: madeYear });
    // (b) village endures: the largest living villages
    var living = livingAgents(S), byV = {}, aggs2 = S.aggregates || [];
    for (var k = 0; k < living.length; k++) { var vn = villageNameOf(living[k], S); if (vn) byV[vn] = (byV[vn] || 0) + 1; }
    for (var ga = 0; ga < aggs2.length; ga++) if (aggs2[ga].village) byV[aggs2[ga].village] = (byV[aggs2[ga].village] || 0) + aggregateSouls(aggs2[ga]);
    var vs = Object.keys(byV).sort(function (a, b) { return byV[b] !== byV[a] ? byV[b] - byV[a] : (a < b ? -1 : 1); });
    for (var v = 0; v < vs.length && v < 3; v++) out.push({ id: 'village:' + vs[v] + ':' + by, kind: 'villageEndures', subject: vs[v], text: vs[v] + ' will still stand in year ' + by + '.', byYear: by, madeYear: madeYear });
    // (c) line survives: the lines with most living souls
    var lines = {}, rootName = {};
    for (var m = 0; m < living.length; m++) { var r = lineOf(living[m], S); lines[r] = (lines[r] || 0) + 1; }
    var ag = S.agents || []; for (var q = 0; q < ag.length; q++) if (ag[q] && lines[ag[q].id]) rootName[ag[q].id] = ag[q].name;
    var ls = Object.keys(lines).map(Number).sort(function (a, b) { return lines[b] !== lines[a] ? lines[b] - lines[a] : a - b; });
    for (var l = 0; l < ls.length && l < 3; l++) {
      var members = []; for (var mm = 0; mm < living.length; mm++) if (lineOf(living[mm], S) === ls[l]) members.push(living[mm].id);
      out.push({ id: 'line:' + ls[l] + ':' + by, kind: 'lineSurvives', subject: ls[l], members: members, text: 'The line of ' + (rootName[ls[l]] || ('#' + ls[l])) + ' will survive to year ' + by + '.', byYear: by, madeYear: madeYear });
    }
    return out;
  }
  function judge(S, pred, E) {
    E = engineOf(E);
    var year = lastYearOf(S), kn = S.knowledge || {};
    if (pred.kind === 'techBefore') {
      var k = kn[pred.subject]; if (k && k.yearBorn !== undefined && k.yearBorn <= pred.byYear) return 'hit';
      return year >= pred.byYear ? 'miss' : 'open';
    }
    var living = livingAgents(S);
    if (pred.kind === 'villageEndures') {
      var alive = false; for (var i = 0; i < living.length; i++) if (villageNameOf(living[i], S) === pred.subject) { alive = true; break; }
      var aggs3 = S.aggregates || []; for (var g3 = 0; g3 < aggs3.length && !alive; g3++) if (aggs3[g3].village === pred.subject && aggregateSouls(aggs3[g3]) > 0) alive = true;
      if (!alive) return 'miss';
      return year >= pred.byYear ? 'hit' : 'open';
    }
    if (pred.kind === 'lineSurvives') {
      // The engine stores parents by NAME, not id (names repeat), so descent is resolved FORWARD from the members recorded
      // at offer time: a soul belongs to the line if it was a member, or if a parent name matches a member one generation
      // up. Ambiguous names err towards inclusion — the lenient direction, stated here rather than hidden.
      var ag = S.agents || [], inLine = {}, byId = {}, changed = true, i2;
      for (i2 = 0; i2 < ag.length; i2++) if (ag[i2]) byId[ag[i2].id] = ag[i2];
      for (i2 = 0; i2 < (pred.members || []).length; i2++) if (byId[pred.members[i2]]) inLine[pred.members[i2]] = true;
      while (changed) {
        changed = false;
        for (i2 = 0; i2 < ag.length; i2++) {
          var c = ag[i2]; if (!c || inLine[c.id] || !c.parents) continue;
          for (var pi = 0; pi < c.parents.length && !inLine[c.id]; pi++) {
            var ids = Object.keys(inLine);
            for (var mi = 0; mi < ids.length; mi++) { var m = byId[ids[mi]]; if (m && m.name === c.parents[pi] && m.gen < c.gen && m.id < c.id) { inLine[c.id] = true; changed = true; break; } }
          }
        }
      }
      var found = false; for (var j = 0; j < living.length; j++) if (inLine[living[j].id]) { found = true; break; }
      if (!found) return 'miss';
      return year >= pred.byYear ? 'hit' : 'open';
    }
    return 'open';
  }

  root.EmergencePresentation = { VERSION: '0.9.0', writeIntervalReport: writeIntervalReport, reportDigest: reportDigest, WEIGHT: WEIGHT, _firstSentence: firstSentence,
    eraBand: eraBand, snapshot: snapshot, selfStop: selfStop, STOP: STOP, offer: offer, judge: judge, HORIZON: HORIZON };
})(typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this));
