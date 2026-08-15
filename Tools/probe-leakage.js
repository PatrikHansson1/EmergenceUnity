// EMERGENCE — RUN_LEAKAGE (Masterplan v2 §1.2). Detektorn förbundet aldrig haft.
//
// LAGEN: vi programmerar REGLER, aldrig UTFALL. Utfallsord får stå i krönikan och i
// mätvärden — ALDRIG i en villkorssats som producerar dem.
//
//   ✗  if (population > 50000) becomeEmpire()
//   ✗  if (huts >= 3) makeVillage()
//   ✗  births stop at < 42 agents
//   ✗  cap = ... + Math.min(fields, 24) * 1.8
//   ✓  människor söker resurser · samarbete har en kostnad · information sprids med förlust
//
// Vi har haft förbundet i ett år och ingen detektor, och det kostade oss två gånger:
// den hårdkodade 42:an, och `Math.min(S.fields.length, 24)` som klämmer varje värld till
// 101-113 själar och stänger av hela bronsåldern. Bägge överlevde för att ingen sökte.
//
// PRIMÄRDETEKTORN är inte ordlistan. Det är MÖNSTRET:
//   ett magiskt tal i ett villkor vars kropp skapar, namnger eller kapar en STRUKTUR.
// Ordlistan är sekundär och rapporteras separat, som en läslista för en människa.
//
// Kör: node Tools/probe-leakage.js [sökväg-till-motorn]
'use strict';
const fs = require('fs'), path = require('path');

const ENGINE = process.argv[2] || process.env.EMERGENCE_ENGINE ||
  path.join(__dirname, '..', 'Assets', 'StreamingAssets', 'Emergence', 'emergence-engine.js');
const src = fs.readFileSync(ENGINE, 'utf8');
const lines = src.split(/\r?\n/);

// Strukturord: saker som ÄR en samhällsform. Att skapa en sådan ur ett tal är läckage.
const STRUCTURE = /\b(village|city|town|empire|state|nation|kingdom|polity|settlement|tribe|clan|dynasty|institution|guild|market|temple|collapse|war|civilization)\b/i;
// Skapande/tilldelning av en struktur
const CREATES  = /\b(push|new |=\s*\{|\.add\(|create|make|found|become|spawn|form)\b/i;
// Tak/klämmor: det som sätter ett UTFALL i stället för att beskriva en regel
const CLAMP    = /Math\.(min|max)\s*\(/;
// Magiskt tal i villkor (0, 1, 2 och 100 är sällan utfall — de är aritmetik)
const MAGIC    = /[<>]=?\s*(\d{2,})|\b(\d{2,})\s*[<>]=?/;

const flags = [], notes = [];
let inBlockComment = false;

for (let i = 0; i < lines.length; i++) {
  const raw = lines[i];
  // grov kommentarsstrippning — kommentarer är dokumentation, inte regler
  let L = raw;
  if (inBlockComment) { if (L.includes('*/')) { L = L.slice(L.indexOf('*/') + 2); inBlockComment = false; } else continue; }
  if (L.includes('/*')) { const j = L.indexOf('/*'); if (!L.includes('*/', j)) inBlockComment = true; L = L.slice(0, j); }
  const c = L.indexOf('//'); if (c >= 0) L = L.slice(0, c);
  if (!L.trim()) continue;

  const ctx = (lines[i] + ' ' + (lines[i + 1] || '')).replace(/\/\/.*$/, '');
  const hasCond = /\bif\s*\(|\?\s*[^:]+:/.test(L);

  // ---- PRIMÄR: magiskt tal som gatar en struktur ----
  if (hasCond && MAGIC.test(L) && STRUCTURE.test(ctx) && CREATES.test(ctx))
    flags.push([i + 1, 'TRÖSKEL→STRUKTUR', raw.trim(), 'ett tal avgör om en samhällsform uppstår']);

  // ---- PRIMÄR: klämma som sätter ett tak på ett utfall ----
  if (CLAMP.test(L) && /\b(cap|capacity|limit|max[A-Z]|pop|births?)\b/i.test(L))
    flags.push([i + 1, 'TAK PÅ UTFALL', raw.trim(), 'Math.min/max som sätter ett resultat, inte en regel']);

  // ---- SEKUNDÄR: strukturord i kontrollflöde ----
  if (hasCond && STRUCTURE.test(L) && !CREATES.test(L))
    notes.push([i + 1, raw.trim()]);
}

// ---- rapport ----
const rel = path.basename(ENGINE);
console.log(`EMERGENCE — RUN_LEAKAGE   ${rel}   ${lines.length} rader\n`);

console.log('## FÄLLT — utfall skrivna som regler');
if (!flags.length) console.log('  inga ✓');
for (const [n, kind, txt, why] of flags) {
  console.log(`  ✗ rad ${String(n).padStart(4)}  [${kind}]  ${why}`);
  console.log(`      ${txt.slice(0, 150)}`);
}

console.log(`\n## LÄSLISTA — strukturord i villkor (${notes.length} st, inte fällda)`);
console.log('   En människa läser dessa. De flesta är legitima (en by ÄR en entitet);');
console.log('   frågan är om villkoret BESKRIVER en struktur eller PRODUCERAR den.');
for (const [n, txt] of notes.slice(0, 25)) console.log(`   rad ${String(n).padStart(4)}  ${txt.slice(0, 120)}`);
if (notes.length > 25) console.log(`   … och ${notes.length - 25} till`);

console.log('\n' + '='.repeat(90));
console.log(flags.length === 0
  ? 'GRÖNT — inga utfall funna i villkorssatser.'
  : `${flags.length} FÄLLDA. Varje rad ska antingen härledas ur en regel eller skrivas om.`);
console.log('Och det starkare provet, som den här filen INTE kan göra åt dig:');
console.log('  en mekanism är fri från läckage bara om den kan STÄNGAS AV utan att utfallet');
console.log('  försvinner — bara ändrar sannolikhet. Försvinner städer helt när en regel');
console.log('  stängs av, VAR den regeln en stad. Det provet är ablationen (v2 §4, T3).');
process.exitCode = flags.length ? 1 : 0;
