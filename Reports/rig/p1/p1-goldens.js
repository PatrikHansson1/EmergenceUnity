globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const _m=require('./engine-pace.js'); const E=globalThis.Emergence||_m;
require('./emergence-presentation.js'); const P=globalThis.EmergencePresentation; const crypto=require('crypto');
const YEAR=144, years=150, seeds=[97013,4242,20260718];
const out={presentationVersion:P.VERSION, presentationSha:require('fs').readFileSync('PRESENTATION-SHA.txt','utf8').trim(), engineSha:'d02bb1ee', years, step:50, goldens:{}, samples:{}};
for(const seed of seeds){
  const S=E.createWorld(seed); S.silent=true; while(S.tick<years*YEAR&&!S.ended)E.tickWorld(S);
  const d=P.reportDigest(S,50); out.goldens[seed]=crypto.createHash('sha256').update(d).digest('hex');
  out.samples[seed]=d.split('\n\n')[0];
  console.error('seed',seed,'klar', out.goldens[seed].slice(0,12));
}
console.log(JSON.stringify(out,null,1));
