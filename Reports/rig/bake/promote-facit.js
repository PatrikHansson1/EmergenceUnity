'use strict';
// promote-facit.js — Motorlås E, steg 2 (Option B, D-871).
// Befordrar de v25b deep-time-facit som kördes rent 2026-09-25 (bake-facit4.js, engine 5dd13837)
// från .v25.txt till .canon.txt. Byte-reproducerbarheten backas av golden 8640 GRÖN (byte-identisk
// jint==golden) + motorns determinism — därför INGEN andra körning (Patriks val "Kör B").
// Säkert: asserterar först v25-sha mot de loggade facit-sha; rör ingen fil om något avviker.
// Sparar den gamla v24-canon som .<sha8>.bak. Kör via RUN_LOCALNODE-vakten i rig\bake.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const dir = path.join(__dirname, '..', 'deeptime-facit');
const log = path.join(__dirname, 'promote-facit-DONE.txt');
const EXPECT = {
  4242: '3993255fc3730f65839f4e90b0397237abc1c72cccdf09b9523b20415deb08d4',
  2323: 'df9a1988b394d0d203196c9003b1aef1deb09601fdce470004ca7f27c447bade'
};
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(f, 'utf8'), 'utf8').digest('hex');
let out = '# promote-facit  .v25.txt -> .canon.txt  ' + new Date().toISOString() + '\n';
out += '# engine v25b 5dd13837 · Option B (enkel körning, golden-8640-backad determinism, D-871)\n';

// FAS 1 — assertera alla v25-sha innan NÅGON fil rörs
let abort = false;
for (const seed of [4242, 2323]) {
  const v25 = path.join(dir, `facit-seed-${seed}-t172800.v25.txt`);
  if (!fs.existsSync(v25)) { out += `seed ${seed}: ABORT — v25 saknas (${v25})\n`; abort = true; continue; }
  const s = sha(v25);
  out += `seed ${seed}: v25 sha=${s} bytes=${fs.statSync(v25).size} ${s === EXPECT[seed] ? 'OK (matchar facit-loggen)' : 'AVVIKER!'}\n`;
  if (s !== EXPECT[seed]) { out += `seed ${seed}: ABORT — v25-sha != loggad ${EXPECT[seed]}\n`; abort = true; }
}
if (abort) { out += '# ABORT — ingen fil rörd\n'; fs.writeFileSync(log, out); console.error('ABORT'); process.exit(1); }

// FAS 2 — backup v24-canon, befordra, verifiera
for (const seed of [4242, 2323]) {
  const v25 = path.join(dir, `facit-seed-${seed}-t172800.v25.txt`);
  const canon = path.join(dir, `facit-seed-${seed}-t172800.canon.txt`);
  if (fs.existsSync(canon)) {
    const csha = sha(canon);
    const bak = path.join(dir, `facit-seed-${seed}-t172800.${csha.slice(0, 8)}.bak`);
    if (!fs.existsSync(bak)) fs.copyFileSync(canon, bak);
    out += `seed ${seed}: v24-canon sha=${csha} -> backup ${path.basename(bak)}\n`;
  } else {
    out += `seed ${seed}: ingen tidigare canon att backa upp\n`;
  }
  fs.copyFileSync(v25, canon);
  const nsha = sha(canon);
  out += `seed ${seed}: canon <- v25 · ny canon sha=${nsha} ${nsha === EXPECT[seed] ? 'OK' : 'MISMATCH!'}\n`;
}
out += '# DONE ' + new Date().toISOString() + '\n';
fs.writeFileSync(log, out);
console.log('promote-facit klar');
