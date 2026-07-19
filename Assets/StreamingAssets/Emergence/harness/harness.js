/* ============================================================================
   EMERGENCE — Golden-master harness (shared JS) — engine 1.2.1 baseline
   Runs identically in: browser (V8/Chromium), Node, and Jint (C#/.NET).
   Produces a CANONICAL, formatting-independent serialization of a full run:
   every number is emitted as its exact IEEE-754 bit pattern (16 hex chars),
   so two runs match bit-for-bit iff every value is bit-identical — no
   dependence on each engine's number→string formatting.
   Reads: global `Emergence` (UMD export of emergence-engine.js).
   Exposes: runGolden(seed, ticks) -> canonical string.
   Per JINT-GOLDEN-MASTER-PLAN.md §2 + Appendix A. Regenerated 2026-07-19 for
   engine 1.2.1 (D-068/D-078): endState adds seenWinter/brewing/endedYear;
   granary/tablet fields belong to the 2.0-proto harness (harness2.js), not here.
   ============================================================================ */
(function (root) {
  'use strict';

  // ---- number → exact bits (IEEE-754 double, big-endian hex) ----
  var _buf = new ArrayBuffer(8);
  var _dv = new DataView(_buf);
  function numBits(n) {
    _dv.setFloat64(0, n, false);
    var s = '';
    for (var i = 0; i < 8; i++) {
      var b = _dv.getUint8(i).toString(16);
      s += (b.length === 1 ? '0' : '') + b;
    }
    return s;
  }

  // ---- canonical serializer ----
  // Deterministic traversal: objects serialize keys in insertion order
  // (spec-defined for string keys in modern engines, incl. Jint 3).
  var _path = [];
  function canon(v, out, depth) {
    if (depth > 64) { out.push('<maxdepth>'); return; }
    var t = typeof v;
    if (v !== null && (t === 'object')) {
      for (var pi = 0; pi < _path.length; pi++) if (_path[pi] === v) { out.push('<cycle>'); return; }
    }
    if (v === null) { out.push('null'); return; }
    if (t === 'number') { out.push('n:' + numBits(v)); return; }
    if (t === 'string') { out.push('s:' + JSON.stringify(v)); return; }
    if (t === 'boolean') { out.push(v ? 'true' : 'false'); return; }
    if (t === 'undefined') { out.push('undef'); return; }
    if (Object.prototype.toString.call(v) === '[object Array]') {
      _path.push(v);
      out.push('[');
      for (var i = 0; i < v.length; i++) { canon(v[i], out, depth + 1); out.push(','); }
      out.push(']');
      _path.pop();
      return;
    }
    if (t === 'object') {
      _path.push(v);
      if (v instanceof Set) { // insertion order is spec-defined
        out.push('Set[');
        v.forEach(function (x) { canon(x, out, depth + 1); out.push(','); });
        out.push(']');
        _path.pop();
        return;
      }
      if (v instanceof Map) {
        out.push('Map[');
        v.forEach(function (val, key) { canon(key, out, depth + 1); out.push('=>'); canon(val, out, depth + 1); out.push(','); });
        out.push(']');
        _path.pop();
        return;
      }
      out.push('{');
      var keys = Object.keys(v); // insertion order, spec-defined
      for (var k = 0; k < keys.length; k++) {
        out.push(JSON.stringify(keys[k]) + ':');
        canon(v[keys[k]], out, depth + 1);
        out.push(',');
      }
      out.push('}');
      _path.pop();
      return;
    }
    out.push('<' + t + '>');
  }
  function canonicalize(v) { var out = []; canon(v, out, 0); return out.join(''); }

  // ---- the golden run ----
  // Full-world capture: entire event log + DNA + history + end-state digest.
  function runGolden(seed, ticks) {
    var E = root.Emergence;
    var S = E.createWorld(seed);
    S.silent = true;
    while (S.tick < ticks && !S.ended) E.tickWorld(S);
    var dna = E.computeDNA(S);
    var hist = E.writeHistory(S);
    var endState = {
      tick: S.tick, hour: S.hour, day: S.day, season: S.season,
      ended: S.ended, endedYear: S.endedYear,
      maxPop: S.maxPop, nextId: S.nextId, nextCustomId: S.nextCustomId,
      nextAnimalId: S.nextAnimalId, maxGeneration: S.maxGeneration,
      usedNames: S.usedNames, winterSeverity: S.winterSeverity,
      seenWinter: S.seenWinter, brewing: S.brewing,
      stats: S.stats, traitSum: S.traitSum,
      agents: S.agents, animals: S.animals, villages: S.villages,
      knowledge: S.knowledge, customs: S.customs,
      fires: S.fires, huts: S.huts, fields: S.fields, regrows: S.regrows,
      tiles: S.tiles,
      eventCount: S.events.length
    };
    var payload = {
      meta: { seed: seed, ticksRequested: ticks, engine: 'emergence-engine.js' },
      events: S.events,
      dna: dna,
      history: hist,
      endState: endState
    };
    return canonicalize(payload);
  }

  // Human-readable variant for diff triage (NOT the authority — canonical is).
  function runReadable(seed, ticks) {
    var E = root.Emergence;
    var S = E.createWorld(seed);
    S.silent = true;
    while (S.tick < ticks && !S.ended) E.tickWorld(S);
    return JSON.stringify({ seed: seed, events: S.events, dna: E.computeDNA(S) }, function (k, v) {
      if (v instanceof Set) return { $set: Array.from(v) };
      if (v instanceof Map) { var o = {}; v.forEach(function (val, key) { o[key] = val; }); return { $map: o }; }
      return v;
    }, 1);
  }

  root.EmergenceGolden = { runGolden: runGolden, runReadable: runReadable, canonicalize: canonicalize, numBits: numBits };
})(typeof globalThis !== 'undefined' ? globalThis : (typeof self !== 'undefined' ? self : this));
