globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const _m=require('./engine-pace.js'); const E=globalThis.Emergence||_m;
require('./emergence-presentation.js'); const P=globalThis.EmergencePresentation; const crypto=require('crypto');
const YEAR=144, seed=Number(process.argv[2]), years=Number(process.argv[3]);
function run(){const S=E.createWorld(seed); S.silent=true; while(S.tick<years*YEAR&&!S.ended)E.tickWorld(S); return S;}
const S=run(); const d=P.reportDigest(S,50); const h=crypto.createHash('sha256').update(d).digest('hex');
const S2=run(); const h2=crypto.createHash('sha256').update(P.reportDigest(S2,50)).digest('hex');
console.log(JSON.stringify({seed,years,digest:h,deterministic:h===h2,len:d.length}));
console.log(d.split('\n\n').slice(1,3).join('\n\n'));
