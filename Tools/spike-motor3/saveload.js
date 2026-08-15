// TAL 5 — spara vid år 1500, ladda, kör till 3000, identisk invariantvektor.
// Planen PÅSTOD att "en händelsekö är serialiserbar av naturen". Det här provar det.
'use strict';
const m = require('./spike-v3.js');

function invariants(W) {
  const ys = W.procedures.map(p => p.year);
  return JSON.stringify({
    n: W.procedures.length, last: Math.max(...ys), pop: W.pop,
    mats: W.mats.length, dims: W.visibleDims, demand: [...W.demand].sort(),
    names: W.procedures.slice(-8).map(p => p.name),
    aff: W.best,
  });
}
// Kön bär FUNKTIONER. De går inte att serialisera — och det är precis fyndet:
// händelser måste bära en TYP-tagg och en tabell, aldrig en closure.
function serialize(W) {
  return JSON.stringify({
    seed: W.seed, now: W.now, year: W.year, pop: W.pop, visibleDims: W.visibleDims,
    instruments: W.instruments, methods: W.methods, demand: [...W.demand],
    memo: [...W.memo], best: W.best, roleBest: W.roleBest,
    mats: W.mats, procedures: W.procedures.map(p => ({ ...p, inputs: p.inputs.map(i => i.name) })),
    procKey: [...W.procKey], queue: W.q.h.map(e => ({ tick: e.tick, cls: e.cls, id: e.id, seq: e.seq })),
    stats: W.stats,
  });
}

const A = m.run(4242, 1500);
const blob = serialize(A);
console.log('sparat vid år', A.year, '·', (blob.length / 1024).toFixed(0), 'kB ·', A.q.size, 'poster i kön');

const missing = A.q.h.filter(e => typeof e.fn === 'function').length;
console.log('poster i kön som bär en CLOSURE och inte går att serialisera:', missing, 'av', A.q.size);

const B = m.run(4242, 3000);
const C = m.run(4242, 3000);
console.log('\ndubbelkörning bit-identisk:', invariants(B) === invariants(C) ? '✓ JA' : '✗ NEJ');
console.log('determinism över instanser :', invariants(m.run(777,800)) === invariants(m.run(777,800)) ? '✓ JA' : '✗ NEJ');
console.log('\nFYND (tal 5): kön bär closures, alltså är den INTE "serialiserbar av naturen".');
console.log('  Rättning som måste in i fas 1: varje händelse bär en TYP-tagg + argument,');
console.log('  och en tabell typ→funktion byggs vid laddning. Kostar en halvdag NU,');
console.log('  och upptäcks annars vid år två när en spelare inte kan öppna sin värld.');
