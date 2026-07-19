/* ============================================================================
   EMERGENCE — Civilization Engine v4 (Information Engine + Living World)
   Philosophy: We program rules — never outcomes.
   Research principle: Research inspires the rules — never dictates the outcomes.

   New in v2:
   - INVENTION v2: observation → hypothesis → experiment → failure → knowledge.
     Agents must OBSERVE phenomena in the world before they can hypothesize.
     Recipes support alternative material sets (property-based); which set was
     used is recorded and names the invention.
   - CULTURE ENGINE, built on the Knowledge Engine (customs are memories):
     Rule 1: people imitate those they respect (prestige-weighted spread).
     Rule 2: repeated habits become norms (village-wide traditions).
     Rule 3: norms are taught to children (inheritance).
     Plus: mutation on transmission, escalation to religion, extinction with
     the last carrier — the same life cycle as knowledge.
   - TABOOS AS SOCIAL COST, NOT LAW: norm -> social cost -> personality ->
     decision. Desperation can override anything; rebels, guilt and schisms
     follow. A fire cult observes heat phenomena faster. Culture and
     technology shape each other. Nothing is scripted.
   - BELIEF ENGINE: knowledge is "it works"; belief is "I think it works".
     Beliefs are born from trauma, spread like customs, and can harden into
     values and taboos. Same fire - different civilizations.
   - SLACK: agents experiment only with surplus (or divine inspiration).
     Innovation requires time, resources, and failures that do not kill.
   New in v3 — IDEAS COMPETE FOR SURVIVAL:
   - Unified information schema: every custom/belief carries slot, trust,
     utility. Contradictory information competes for the same slot in each
     mind; adopting one expels the other (conversion).
   - Worldview filters: The Knowing Eye (empiric) vs The Unseen Wills (faith)
     compete on fitness — successful experiments feed one, traditions feed
     the other. Scientific revolutions are filter competition, never script.
   - Reformation: when a village's norm falls below minority, a rival way
     can take its place. Broken taboos lose trust; famine breeds reform.
   New in v4 — THE LIVING WORLD:
   - Seasons: spring, summer, autumn, winter. Winters vary in severity
     (deterministic per seed); hard winters kill, and killing winters give
     fire, huts, pottery and the hunt their meaning.
   - Animals: deer herds graze, breed in spring and flee; wolf packs hunt
     deer — and lone humans on winter nights. The spear turns humans from
     prey to hunters. Hunting rites and wolf-beliefs emerge unscripted.
   ============================================================================ */
(function(root,factory){
  if(typeof module!=='undefined'&&module.exports)module.exports=factory();
  else root.Emergence=factory();
})(typeof self!=='undefined'?self:this,function(){
"use strict";

const W=64,H=44;

function mulberry32(a){return function(){a|=0;a=a+0x6D2B79F5|0;let t=Math.imul(a^a>>>15,1|a);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};}
const clamp=(v,a,b)=>v<a?a:(v>b?b:v);
const dist=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
function R(S,a,b){return a+S.rand()*(b-a);}
function RI(S,a,b){return Math.floor(R(S,a,b+1));}
function pick(S,arr){return arr[Math.floor(S.rand()*arr.length)];}

// ---------- observations: the world must be seen before it can be understood ----------
const OBS={
  frictionHeat:{txt:'that rubbing wood makes heat'},
  sharpShards:{txt:'that struck stone breaks into sharp shards'},
  fibersTwist:{txt:'that grass fibers hold when twisted'},
  heatHardens:{txt:'that fire makes clay hard'},
  oreMelts:{txt:'that fire makes the heavy ore sweat metal'},
  sandGlints:{txt:'that hot sand turns glassy'},
  logsRoll:{txt:'that logs roll'},
  stonesGrind:{txt:'that stones grind seed to flour'},
  branchBends:{txt:'that a bent branch fights to spring back'},
  fishGather:{txt:'that fish gather in the shallows'},
  seedsSprout:{txt:'that spilled seeds sprout where they fall'},
  marksRemain:{txt:'that marks pressed in clay remain'},
};

// ---------- knowledge catalog: rules, never outcomes ----------
// alts = alternative material sets (property thinking); the set used names the tool.
const TECHS=[
 {id:'fire',      icon:'🔥', base:'Fire',            alts:[{wood:2}],                              pre:[], insights:['frictionHeat'], var:['flame','ember','blaze'],
   flavor:'rubbed sticks together until a spark caught the tinder', effect:'Warmth at night. Opens the path to pottery and smithing.'},
 {id:'sharp',     icon:'🔪', base:'Sharpened stone', alts:[{stone:2}],                             pre:[], insights:['sharpShards'], var:['shard','edge','knife'],
   flavor:'struck two stones together and kept the sharp shard', effect:'Faster gathering of everything.'},
 {id:'rope',      icon:'🪢', base:'Rope',            alts:[{fiber:3}],                             pre:[], insights:['fibersTwist'], var:['braid','twine','cord'],
   flavor:'twisted grass fibers into something strong', effect:'Makes it possible to bind parts together.'},
 {id:'spear',     icon:'🗡️', base:'Spear',           alts:[{wood:1,stone:1,fiber:1}],              pre:['sharp','rope'], insights:[], var:['hunting spear','javelin','pike'],
   flavor:'lashed a sharp point to a long shaft', effect:'Humans are prey no longer. Deer can be hunted — food even in winter.'},
 {id:'axe',       icon:'🪓', base:'Axe',             alts:[{stone:1,wood:1,fiber:1},{iron:1,wood:1,fiber:1}], pre:['sharp','rope'], insights:[], var:['cleaver','feller','hewer'],
   flavor:'bound a hard edge to a wooden shaft', effect:'Twice as fast logging. Required to build huts.'},
 {id:'pottery',   icon:'🏺', base:'Pottery',         alts:[{clay:3}],                              pre:['fire'], insights:['heatHardens'], var:['vessel','jar','urn'],
   flavor:'placed shaped clay in the fire and found it hard as stone', effect:'Food can be stored — hunger bites less.'},
 {id:'hut',       icon:'🛖', base:'Hut',             alts:[{wood:8}],                              pre:['axe'], insights:[], var:['shelter','lodge','home'],
   flavor:'raised logs against each other into a shelter', effect:'Shelter and warmth at night. Three huts close together become a village.'},
 {id:'kiln',      icon:'🧱', base:'Kiln',            alts:[{stone:4,clay:2}],                      pre:['fire','pottery'], insights:[], var:['furnace','hearth','burnoven'],
   flavor:'built a stone chamber that gathered the heat of the fire', effect:'Heat enough to melt what hides in ore and sand.'},
 {id:'smithing',  icon:'⚒️', base:'Iron smithing',   alts:[{iron:2,wood:4}],                       pre:['kiln'], insights:['oreMelts'], var:['ironcraft','forgework','hammercraft'],
   flavor:'watched gleaming metal run from the glowing ore', effect:'Metal — an entirely new material.'},
 {id:'metaltools',icon:'⛏️', base:'Metal tools',     alts:[{iron:1,wood:2}],                       pre:['smithing'], insights:[], var:['iron axe','pickaxe','plough'],
   flavor:'forged the iron into an edge that never dulled', effect:'Three times faster work.'},
 {id:'glass',     icon:'🫙', base:'Glass',           alts:[{sand:3}],                              pre:['kiln'], insights:['sandGlints'], var:['clearstone','lenswork','flask'],
   flavor:'melted sand and found something transparent', effect:'A material no one could have imagined.'},
 {id:'wheel',     icon:'🛞', base:'The Wheel',       alts:[{wood:4}],                              pre:['axe'], insights:['logsRoll'], var:['roller','cartwheel','pulley'],
   flavor:'watched a log roll and understood something great', effect:'The foundation of mills, carts — and everything that spins.'},
 {id:'mill',      icon:'🌾', base:'Mill',            alts:[{wood:6,stone:2}],                      pre:['wheel'], insights:['stonesGrind'], var:['gristmill','millhouse','crusher'],
   flavor:'let the wheel drive stones that ground grain', effect:'The village can process food — famine recedes.'},
 {id:'bow',       icon:'🏹', base:'Bow',             alts:[{wood:2,fiber:2}],                      pre:['spear','rope'], insights:['branchBends'], var:['hunting bow','longbow','birchbow'],
   flavor:'bent a young branch with a cord and felt it fight back', effect:'The hunt no longer needs closeness. Deer fall at a distance.'},
 {id:'fishing',   icon:'🎣', base:'Fishing',         alts:[{fiber:3,wood:1}],                      pre:['rope'], insights:['fishGather'], var:['fishing line','the net','hookline'],
   flavor:'let a twisted line sink where the fish gather', effect:'The water becomes a larder — food even when the land gives little.'},
 {id:'farming',   icon:'🌱', base:'Farming',         alts:[{wood:1,fiber:2}],                      pre:['pottery'], insights:['seedsSprout'], var:['the first field','seedcraft','tilling'],
   flavor:'pressed spilled seeds into turned soil and waited', effect:'Food that returns every year. The village roots itself.'},
 {id:'writing',   icon:'📜', base:'Writing',         alts:[{clay:2}],                              pre:['pottery'], insights:['marksRemain'], var:['glyphs','memory-marks','script'],
   flavor:'carved signs into clay so knowledge could outlive its owner', effect:'Knowledge spreads to all — and to generations to come.'},
];
const TECH=Object.fromEntries(TECHS.map(t=>[t.id,t]));
const MATSOURCE={wood:'forest',stone:'stone',fiber:'grass',clay:'clay',iron:'iron',sand:'sand'};
// which observations can occur while gathering which material
const GATHER_OBS={wood:[['frictionHeat',.10],['logsRoll',.07],['branchBends',.08]],stone:[['sharpShards',.16],['stonesGrind',.06]],fiber:[['fibersTwist',.14]],sand:[['sandGlints',.14]],clay:[],iron:[]};
const NAMES=['Eira','Ask','Embla','Torv','Liv','Sten','Ylva','Bjorn','Saga','Rune','Freja','Kare','Idun','Halvar','Signe','Vidar','Tuva','Alve','Ronja','Sixten','Maja','Loke','Vera','Otto','Selma','Falk','Nanna','Ulv','Disa','Orm'];
const YEAR=144;
const SEASONS=['spring','summer','autumn','winter'];
function seasonOf(S){const t=S.tick%144;return t<40?'spring':t<84?'summer':t<116?'autumn':'winter';} // Engine 1.1 (D-049, EP): shorter winter (28, was 36), longer growing seasons — spring 40, summer 44, autumn 32

const CHAT={
  small:['What a day!','Did you see the sunrise?','The land is good here.','I dreamt last night.','The wind is turning.'],
  hungry:['I am so hungry...','Are there berries left?','My stomach growls.'],
  cold:['The cold bites tonight.','I am freezing...','We need warmth.'],
  discovery:['I have created something new!','Look what I made!','This changes everything!'],
  teach:['Let me show you something...','This is how it is done...','I just learned this.'],
  love:['I like being near you.','We are building something together, you and I.','Stay close.'],
  observe:['Did you see that?','Curious...','I must remember this.'],
  fail:['Not like that, then...','Almost. Almost!','Why will it not hold?'],
  ritual:['It felt right.','For those before us.','So we remember.'],
};

// ---------- culture templates (instantiated per world with the world's own randomness) ----------
const RIT_ACTS=['placing a stone on','laying flowers on','planting a seed by','pouring water over','burning a twig at'];
const RIT_TRIG={death:'the grave',discovery:'the place of discovery',child:'the newborn\'s doorstep',hunt:'the fallen prey'};
const GATHER_ACTS=['singing','humming','drumming','telling stories'];
const GATHER_PLACES=['by the fire','under the old tree','by the water'];
const MARK_ACTS=['painting a mark on','carving a sign into'];
const MARK_OBJS=['the hut wall','a flat stone','an old tree'];
const TABOOS=[{target:'wood',name:'The Ban of the Living Trees',txt:'never fells living trees'},
              {target:'iron',name:'The Ban of the Deep Iron',txt:'never digs the ancestors\' iron'}];
const VALUES=[{target:'fire',name:'The Undying Flame',txt:'keeps a flame burning at all times'}];
const RELIGION_WORDS={stone:'Stone',flowers:'Flowers',seed:'Seed',water:'Water',twig:'Ember',singing:'Song',humming:'Song',drumming:'Drum',stories:'Telling',painting:'Marks',carving:'Marks',wood:'Trees',iron:'Iron',fire:'Flame',wolf:'Wolf',observation:'Open Eye'};

// ---------- event log ----------
function ev(S,type,txt,data){
  const e=Object.assign({tick:S.tick,day:S.day,year:Math.floor(S.tick/YEAR)+1,type,txt},data||{});
  S.events.push(e);
  if(S.onEvent&&!S.silent)S.onEvent(e,S);
  return e;
}

// ---------- Knowledge Engine ----------
function gainKnowledge(S,a,id,via,altUsed){
  if(a.knows.has(id))return;
  a.knows.add(id);
  const t=TECH[id];
  let k=S.knowledge[id];
  if(!k){
    const mat=altUsed?Object.keys(altUsed)[0]:null;
    const name=`${a.name}'s ${mat==='iron'&&id==='axe'?'iron ':''}${pick(S,t.var)}`;
    k=S.knowledge[id]={id,name,status:'alive',inventedBy:a.name,yearBorn:Math.floor(S.tick/YEAR)+1,rediscoveries:0,losses:0,madeFrom:altUsed?Object.keys(altUsed).join('+'):''};
    ev(S,'tech',`${t.icon} <b>${a.name}</b> ${t.flavor} — <b>${name}</b> (${t.base}) has been invented! <i>${t.effect}</i>`,{tech:id,agent:a.id,x:a.x,y:a.y});
    speak(S,a,pick(S,CHAT.discovery));
    if(id==='fire')giveEpithet(S,a,'the Firebringer');
    else if(id==='writing')giveEpithet(S,a,'the Rememberer');
    else if(id==='spear')giveEpithet(S,a,'the Spearmaker');
    else if(id==='smithing')giveEpithet(S,a,'the Ironhand');
    if(S.rand()<.10)addCustom(S,a,{kind:'belief',lens:'empiric',target:'worldview',slot:'belief:worldview',name:'The Knowing Eye',txt:'believing the world repeats and can be understood',word:'observation'});
    for(const cid in S.customs){const c2=S.customs[cid];if(c2.lens==='empiric'&&c2.status==='alive')c2.trust=clamp(c2.trust+.02,0,1);}
    maybeEmergeCustom(S,a,'discovery');
  } else if(k.status==='extinct'){
    k.status='alive'; k.rediscoveries++;
    giveEpithet(S,a,'the Rekindler');
    ev(S,'rediscovered',`💡 <b>${disp(a)}</b> has rediscovered ${k.name} (${t.base}) — knowledge lost for ${Math.floor(S.tick/YEAR)+1-k.diedYear} years lives again!`,{tech:id,agent:a.id,x:a.x,y:a.y});
  }
}
function checkExtinct(S){
  for(const id in S.knowledge){
    const k=S.knowledge[id];
    if(k.status!=='alive')continue;
    if(!S.agents.some(a=>!a.dead&&a.knows.has(id))){
      k.status='extinct'; k.diedYear=Math.floor(S.tick/YEAR)+1; k.losses++;
      ev(S,'knowledgeLost',`🕯️ With <b>${k.lastKnownBy||'the last of them'}</b> died the last knowledge of ${k.name} (${TECH[id].base}). It is <b>extinct</b> — until someone rediscovers it.`,{tech:id});
    }
  }
  for(const id in S.customs){
    const c=S.customs[id];
    if(c.status==='gone')continue;
    if(!S.agents.some(a=>!a.dead&&a.customs.has(id))){
      c.status='gone'; c.diedYear=Math.floor(S.tick/YEAR)+1;
      if((c.everAdopters||0)<=1)ev(S,'customLost',`🍂 ${c.origin} ${c.txt} until the end. No one else ever took it up. It died quietly, as most things do.`,{custom:id});
      else ev(S,'customLost',`🕯️ No one remembers ${c.name} anymore. A piece of who they were is gone.`,{custom:id});
    }
  }
}

// ---------- Culture Engine (customs are memories — same life cycle as knowledge) ----------
function prestige(a){return clamp((a.knows.size+a.customs.size)*.08+a.age*.004,.1,1.2);}
// People are wonderfully imperfect. Quirks are not systems and give no bonuses —
// they are the small strangeness that makes a person a person. Some quirks, imitated,
// become customs: a beautiful mistake growing into a tradition through the ordinary culture engine.
const QUIRKS=[
 {id:'stones',born:'has begun collecting round stones. No one knows why.',txt:'Collects round stones. For nothing.',
  custom:{kind:'ritual',slot:'rite:quirk-stones',name:'The Round Stones',txt:'leaving a round stone at the door of every hut',word:'stone'}},
 {id:'longway',born:'always takes the long way home, even in rain.',txt:'Always takes the long way home.'},
 {id:'earlysleep',born:'is always the first to fall asleep, wherever they are.',txt:'Always the first to fall asleep.'},
 {id:'humdawn',born:'has started humming at first light.',txt:'Hums at first light.',
  custom:{kind:'gathering',slot:'rite:quirk-hum',name:'Humming at Dawn',txt:'humming together as the sun rises',word:'hum'}},
 {id:'sticks',born:'carries a stick everywhere. It is never used for anything.',txt:'Carries a stick. It is never used.',
  custom:{kind:'ritual',slot:'rite:quirk-sticks',name:'The Carried Stick',txt:'carrying a stick one never uses',word:'stick'}},
 {id:'stares',born:'stops at the same strange stone every day, and looks at it.',txt:'Visits the same strange stone daily.'},
 {id:'whistle',born:'whistles every morning, whether the morning deserves it or not.',txt:'Whistles every morning.',
  custom:{kind:'gathering',slot:'rite:quirk-whistle',name:'The Morning Whistle',txt:'whistling to greet the morning',word:'whistle'}},
 {id:'backwards',born:'walks the last few steps home backward. Always.',txt:'Walks the last steps home backward.'},
 {id:'namer',born:'gives names to things that do not need names.',txt:'Names things that need no names.'},
 {id:'fireshy',born:'always sits farthest from the fire.',txt:'Always sits farthest from the fire.'},
 {id:'flower',born:'picks a flower every spring and puts it somewhere no one sees.',txt:'Leaves a flower where no one sees.'},
 {id:'counter',born:'counts the others every evening before sleep.',txt:'Counts the others every evening.'},
];
const QUIRK={};for(const q of QUIRKS)QUIRK[q.id]=q;

function maybeEmergeCustom(S,a,trigger,traumaCause){
  if(a.age<14||a.dead)return;
  // trauma can birth taboos and values — culture is discovered, not designed
  // Knowledge is "it works". Belief is "I think it works". Same fire - different civilizations.
  if(traumaCause){
    if(traumaCause==='wolves'){
      const has=hasCustomKind(S,a,'belief','wolf');
      if(has&&S.rand()<.5){addCustom(S,a,{kind:'value',target:'night',slot:'norm:night',name:'The Rule of the Shared Fire',txt:'never letting anyone walk alone after dark',word:'fire'});return;}
      if(S.rand()<.4){addCustom(S,a,{kind:'belief',target:'wolf',slot:'belief:wolf',name:'The belief that wolves carry the dead\'s anger',txt:'believing the wolves carry the dead\'s anger',word:'wolf'});return;}
    }
    if(traumaCause==='cold'){
      const has=hasCustomKind(S,a,'belief','fire');
      if(has&&S.rand()<.5){addCustom(S,a,{kind:'value',target:'fire',slot:'norm:fire',name:VALUES[0].name,txt:VALUES[0].txt,word:'fire'});return;}
      if(S.rand()<.35){addCustom(S,a,{kind:'belief',target:'fire',slot:'belief:fire',name:'The belief that fire keeps death away',txt:'believing fire keeps death away',word:'fire'});return;}
    } else if(S.rand()<.08){
      addCustom(S,a,{kind:'belief',lens:'faith',target:'worldview',slot:'belief:worldview',name:'The Unseen Wills',txt:'believing unseen wills move all things',word:'stories'});return;
    } else if(S.rand()<.12){
      const omin=pick(S,[{target:'iron',t:TABOOS[1],name:'The belief that the deep iron angers the ancestors',txt:'believing the deep iron angers the ancestors'},
                         {target:'wood',t:TABOOS[0],name:'The belief that the living trees remember the axe',txt:'believing the living trees remember the axe'}]);
      const has=hasCustomKind(S,a,'belief',omin.target);
      if(has&&S.rand()<.4){addCustom(S,a,{kind:'taboo',target:omin.t.target,slot:'norm:'+omin.t.target,name:omin.t.name,txt:omin.t.txt,word:omin.t.target});return;}
      addCustom(S,a,{kind:'belief',target:omin.target,slot:'belief:'+omin.target,name:omin.name,txt:omin.txt,word:omin.target});return;
    }
  }
  const sat=clamp(12/(12+Object.values(S.customs).filter(c=>c.status==='alive').length),.15,1);
  const p=(trigger==='death'?.10:trigger==='discovery'?.06:trigger==='hunt'?.05:.04)*sat;
  if(S.rand()>=p*(.5+a.traits.social*.8))return;
  let c;
  if(trigger==='dusk'){
    const act=pick(S,GATHER_ACTS),place=pick(S,GATHER_PLACES);
    c={kind:'gathering',slot:'rite:dusk',name:`${act[0].toUpperCase()+act.slice(1)} ${place} at dusk`,txt:`${act} ${place} as the light fades`,word:act};
  }else if(trigger==='discovery'){
    const act=pick(S,MARK_ACTS),obj=pick(S,MARK_OBJS);
    c={kind:'ritual',slot:'rite:discovery',name:`${act[0].toUpperCase()+act.slice(1)} ${obj}`,txt:`${act} ${obj} when something new is made`,word:act.split(' ')[0]};
  }else{
    const act=pick(S,RIT_ACTS),obj=RIT_TRIG[trigger]||'the grave';
    c={kind:'ritual',slot:'rite:'+trigger,name:`${act[0].toUpperCase()+act.slice(1)} ${obj}`,txt:`${act} ${obj}`,word:act.split(' ')[1]||act.split(' ')[0]};
  }
  addCustom(S,a,c);
}
function adoptCustom(S,a,cu){
  cu.everAdopters=(cu.everAdopters||0)+1;
  // ideas compete for the same slot in each mind: adopting one expels its rival
  if(cu.slot){
    for(const oid of [...a.customs]){
      const oc=S.customs[oid];
      if(oc&&oid!==cu.id&&oc.slot===cu.slot){
        a.customs.delete(oid);
        S.stats.conversions++;
        if(S.rand()<.12)ev(S,'conversion',`<b>${a.name}</b> has left ${oc.name} for ${cu.name}. Minds change; so do worlds.`,{custom:cu.id,from:oid});
      }
    }
  }
  a.customs.add(cu.id);
}
function addCustom(S,a,c,mutatedFrom){
  // same custom may independently arise — find by name
  let ex=Object.values(S.customs).find(x=>x.name===c.name);
  if(ex){if(!a.customs.has(ex.id)){adoptCustom(S,a,ex);if(ex.status==='gone'){ex.status='alive';ev(S,'customBack',`🌱 <b>${a.name}</b> has revived ${ex.name} — an old way returns.`,{custom:ex.id});}}return ex;}
  const id='c'+(S.nextCustomId++);
  const cu=S.customs[id]={id,kind:c.kind,name:c.name,txt:c.txt,word:c.word,target:c.target||null,
    slot:c.slot||null,lens:c.lens||null,
    trust:.5,utility:c.kind==='value'?.7:c.kind==='taboo'?.3:.5,
    origin:a.name,yearBorn:Math.floor(S.tick/YEAR)+1,status:'alive',norm:false,religion:false,mutatedFrom:mutatedFrom||null};
  adoptCustom(S,a,cu);
  const label=c.lens?'A NEW WAY OF SEEING':c.kind==='taboo'?'A TABOO TAKES HOLD':c.kind==='value'?'A CONVICTION IS BORN':c.kind==='belief'?'A BELIEF TAKES ROOT':'A CUSTOM APPEARS';
  ev(S,'custom',`🌿 <b>${a.name}</b> began ${cu.txt} — no one told them to. Others may follow. <i>(${cu.name})</i>`,{custom:id,agent:a.id,x:a.x,y:a.y,label});
  speak(S,a,pick(S,CHAT.ritual));
  return cu;
}
function getLens(S,a){
  for(const id of a.customs){const c=S.customs[id];if(c&&c.slot==='belief:worldview')return c.lens;}
  return null;
}
function spreadCustoms(S,a,b){
  // Rule 1: people imitate those they respect. Rivals contest the slot.
  const faithBoost=getLens(S,a)==='faith'?1.3:1;
  for(const id of a.customs){
    const c=S.customs[id];
    if(!c||c.status!=='alive'||b.customs.has(id))continue;
    let rival=null;
    if(c.slot)for(const oid of b.customs){const oc=S.customs[oid];if(oc&&oc.slot===c.slot){rival=oc;break;}}
    let ch;
    if(rival){
      ch=.10*prestige(a)*faithBoost*(.5+(c.trust+c.utility)-(rival.trust+rival.utility)*.8)*(1-b.traits.conformity*.5)*(rival.norm?.5:1);
      ch=clamp(ch,0,.4);
    }else{
      ch=.30*prestige(a)*faithBoost;
    }
    if(S.rand()<ch){
      if(!rival&&S.rand()<.06){ // mutation: culture drifts as it spreads
        const m=mutateCustom(S,c,b);
        if(m)break;
      }
      adoptCustom(S,b,c);
      if(!rival&&S.rand()<.12)ev(S,'imitated',`<b>${b.name}</b> has begun ${c.txt}, the way ${a.name} does. The custom spreads.`,{custom:id});
      break;
    }
  }
  // observations travel as stories too
  for(const o of a.obs){
    if(!b.obs.has(o)&&S.rand()<.20){b.obs.add(o);break;}
  }
}
function mutateCustom(S,c,adopter){
  if(c.kind==='taboo'||c.kind==='value')return null;
  let nc=null;
  if(c.kind==='gathering'){const act=pick(S,GATHER_ACTS),place=pick(S,GATHER_PLACES);
    nc={kind:'gathering',slot:c.slot,name:`${act[0].toUpperCase()+act.slice(1)} ${place} at dusk`,txt:`${act} ${place} as the light fades`,word:act};}
  else{const act=pick(S,RIT_ACTS.concat(MARK_ACTS));const obj=c.name.includes('grave')?'the grave':pick(S,MARK_OBJS);
    nc={kind:'ritual',slot:c.slot,name:`${act[0].toUpperCase()+act.slice(1)} ${obj}`,txt:`${act} ${obj}`,word:act.split(' ')[1]||act.split(' ')[0]};}
  if(nc.name===c.name)return null;
  const cu=addCustom(S,adopter,nc,c.id);
  if(cu&&!cu.mutatedFrom)cu.mutatedFrom=c.id;
  if(S.rand()<.5)ev(S,'mutation',`🧬 <b>${adopter.name}</b> does it differently: ${cu.txt} instead of ${c.txt}. A tradition drifts.`,{custom:cu.id});
  return cu;
}
function hasCustomKind(S,a,kind,target){
  for(const id of a.customs){const c=S.customs[id];if(c&&c.status==='alive'&&c.kind===kind&&(!target||c.target===target))return c;}
  return null;
}
// Rule 2: repeated habits become norms. Norms can become religion.
function cultureYearTick(S){
  const year=Math.floor(S.tick/YEAR)+1;
  // WEAK TIES: now and then, someone walks to the other village. Two cultures touch; an idea jumps.
  if(S.villages.length>=2&&(S.season==='spring'||S.season==='summer')&&S.rand()<.6){
    const cands=S.agents.filter(x=>!x.dead&&x.age>=16&&x.age<50&&!x.visit&&x.hunger>75&&(x.traits.social+x.traits.curiosity)>1.1);
    if(cands.length){
      const a=cands[Math.floor(S.rand()*cands.length)];
      let hv=null,hd=1e9;for(const v of S.villages){const d=dist(a,v);if(d<hd){hd=d;hv=v;}}
      const others=S.villages.filter(v=>v!==hv);
      if(others.length){
        const v=others[Math.floor(S.rand()*others.length)];
        a.hunger=clamp(a.hunger+25,0,140);a.visit={x:v.x,y:v.y,name:v.name,t:15,k0:a.knows.size,cs:[...a.customs].sort().join()}; // they pack food for the road
        ev(S,'journey',`🚶 <b>${disp(a)}</b> set out for ${v.name}. Ideas travel on foot.`,{agent:a.id,x:a.x,y:a.y});
      }
    }
  }
  // Habits: personality -> habit -> tradition -> culture. A habit describes a life.
  for(const a of S.agents){
    if(a.dead||a.age<10)continue;
    if(!a.habit&&a.age>=14&&S.rand()<.006){a.habit=QUIRKS[Math.floor(S.rand()*QUIRKS.length)].id;a.habitOrigin=null;}
    if(!a.habit||a.habitShown)continue;
    if(S.rand()<.06){
      a.habitShown=true;
      const q=QUIRK[a.habit];
      const why=a.habitOrigin?
        (a.habitOrigin.kind==='grief'?` It began the year ${a.habitOrigin.from} died. It has not stopped since.`
        :` As ${a.habitOrigin.from} did before ${a.age<20?'them':'her'}. No one decided this.`)
        :' Once begun, never stopped.';
      ev(S,'quirk',`🙂 <b>${a.name}</b> ${q.born}${why}`,{agent:a.id,x:a.x,y:a.y});
      if(q.custom&&S.rand()<.3)addCustom(S,a,Object.assign({},q.custom));
    }
  }
  // Inside jokes: a habit-born tradition whose reason has died with its origin
  for(const id in S.customs){
    const c=S.customs[id];
    if(c.status!=='alive'||!c.norm||c.forgotten||!c.slot||String(c.slot).indexOf('rite:quirk-')!==0)continue;
    const originAlive=S.agents.some(x=>!x.dead&&x.name===c.origin);
    if(!originAlive&&year-c.yearBorn>=40&&S.rand()<.2){
      c.forgotten=true;
      ev(S,'legend',`🌳 In ${c.normVillage||'the village'}, no one remembers why one goes on ${c.txt}. Not even they do. One simply does.`,{custom:id});
    }
  }
  for(const v of S.villages){
    const adults=S.agents.filter(a=>!a.dead&&a.age>=14&&dist(a,v)<9);
    if(adults.length<3)continue;
    const counts={};
    for(const a of adults)for(const id of a.customs){const c=S.customs[id];if(c&&c.status==='alive')counts[id]=(counts[id]||0)+1;}
    // LEARNING TO READ THE WORLD — culture's first word. Before the old way falls,
    // its challengers can be SEEN finding each other. The sign precedes the event.
    if(!S.brewing||S.tick>=S.brewing.until){
      S.brewing=null;
      for(const id in counts){
        const c=S.customs[id];
        if(!c||!c.norm||c.normVillage!==v.name)continue;
        if(counts[id]/adults.length>=.55)continue; // the old way still stands firm
        for(const rid in counts){
          const rc=S.customs[rid];
          if(rc&&rid!==id&&rc.slot&&rc.slot===c.slot&&counts[rid]>=Math.ceil(adults.length*.35)){
            S.brewing={vx:v.x,vy:v.y,village:v.name,rival:rid,norm:id,until:S.tick+YEAR*3};
            break;
          }
        }
        if(S.brewing)break;
      }
    }
    for(const id in counts){
      const c=S.customs[id];
      if(!c.norm&&counts[id]>=Math.ceil(adults.length*.75)&&year-c.yearBorn>=10){
        c.norm=true;c.normYear=year;c.normVillage=v.name;
        ev(S,'tradition',`🏘️ In ${v.name}, ${c.txt} is now simply what one does. <b>${c.name}</b> has become a tradition.`,{custom:id,x:v.x,y:v.y});
        for(const cid in S.customs){const c2=S.customs[cid];if(c2.lens==='faith'&&c2.status==='alive')c2.trust=clamp(c2.trust+.02,0,1);}
      }
      if(c.norm&&c.normVillage===v.name&&(counts[id]||0)<Math.ceil(adults.length*.4)){
        c.norm=false;
        let heir=null;
        for(const rid in counts){const rc=S.customs[rid];if(rc&&rid!==id&&rc.slot&&rc.slot===c.slot&&counts[rid]>=Math.ceil(adults.length*.6)){heir=rc;break;}}
        if(S.brewing&&S.brewing.norm===id)S.brewing=null; // the storm has broken
        if(heir){heir.norm=true;heir.normYear=year;heir.normVillage=v.name;
          ev(S,'reformation',`⚡ In ${v.name}, the old way is set aside: <b>${heir.name}</b> replaces ${c.name}. The elders mutter; the young do not listen.`,{custom:heir.id,from:id,x:v.x,y:v.y});
        } else ev(S,'normFades',`In ${v.name}, fewer and fewer keep ${c.name}. An age is quietly ending.`,{custom:id});
      }
      if(c.norm&&!c.religion&&year-c.normYear>=40&&(c.mutatedFrom||Object.values(S.customs).some(x=>x.mutatedFrom===id))&&S.rand()<.15&&!Object.values(S.customs).some(x=>x.religion&&x.normVillage===c.normVillage)){
        c.religion=true;
        const word=RELIGION_WORDS[c.word]||'Old Ways';
        c.religionName=`The Way of the ${word}`;
        ev(S,'religion',`⛩️ Generations in ${c.normVillage||v.name} have kept ${c.name} so long that no one remembers why. It has become sacred: <b>${c.religionName}</b> is born.`,{custom:id,x:v.x,y:v.y});
      }
    }
  }
}

// ---------- world creation ----------
function makeWorld(S){
  S.tiles=[];
  for(let y=0;y<H;y++){S.tiles[y]=[];for(let x=0;x<W;x++)S.tiles[y][x]={t:'grass',n:0};}
  const blob=(type,count,rad)=>{
    for(let i=0;i<count;i++){
      const cx=RI(S,4,W-5),cy=RI(S,4,H-5),r=RI(S,2,rad);
      for(let y=cy-r;y<=cy+r;y++)for(let x=cx-r;x<=cx+r;x++){
        if(x<0||y<0||x>=W||y>=H)continue;
        if(Math.hypot(x-cx,y-cy)<=r*R(S,.6,1))S.tiles[y][x]={t:type,n:type==='water'?0:RI(S,4,9)};
      }
    }
  };
  blob('water',3,7);blob('forest',6,6);blob('stone',4,4);blob('berry',5,3);blob('sand',2,3);
  const scatter=(near,type,count)=>{
    let placed=0,guard=0;
    while(placed<count&&guard++<4000){
      const x=RI(S,1,W-2),y=RI(S,1,H-2);
      if(S.tiles[y][x].t!=='grass')continue;
      let ok=false;
      for(let dy=-2;dy<=2;dy++)for(let dx=-2;dx<=2;dx++){const t=(S.tiles[y+dy]||[])[x+dx];if(t&&t.t===near)ok=true;}
      if(ok){S.tiles[y][x]={t:type,n:RI(S,3,7)};placed++;}
    }
  };
  scatter('water','clay',14);scatter('stone','iron',8);
  // loose boulders and patches — the world invites curiosity everywhere
  let placed=0,guard=0;
  const loose=[['stone',10],['sand',5],['clay',4]];
  for(const[type,count]of loose){placed=0;guard=0;
    while(placed<count&&guard++<2000){
      const x=RI(S,2,W-3),y=RI(S,2,H-3);
      if(S.tiles[y][x].t==='grass'){S.tiles[y][x]={t:type,n:RI(S,2,4)};placed++;}
    }
  }
}
function makeAgent(S,x,y,parents){
  const mut=v=>clamp(v+R(S,-.15,.15),.05,1);
  const a={
    id:S.nextId++, name:NAMES[S.usedNames++%NAMES.length]+(S.usedNames>NAMES.length?' II':''),
    x,y, age:parents?0:RI(S,17,24), gen:parents?Math.max(parents[0].gen,parents[1].gen)+1:1,
    lifespan:RI(S,55,85), hunger:80,energy:90,warmth:90,social:70,
    inv:{},knows:new Set(),obs:new Set(),customs:new Set(),rel:{},task:'thinking',expT:0,expTech:null,expAlt:null,
    say:'',sayT:0,inspired:0,childCd:0,talkCd:0,phase:R(S,0,6.28),
    hue:parents?(parents[0].hue+parents[1].hue)/2+RI(S,-20,20):RI(S,0,360),
    traits:{
      curiosity:parents?mut((parents[0].traits.curiosity+parents[1].traits.curiosity)/2):R(S,.2,.95),
      social:parents?mut((parents[0].traits.social+parents[1].traits.social)/2):R(S,.2,.95),
      diligence:parents?mut((parents[0].traits.diligence+parents[1].traits.diligence)/2):R(S,.3,.95),
      conformity:parents?mut((parents[0].traits.conformity+parents[1].traits.conformity)/2):R(S,.15,.95),
    },
    parents:parents?[parents[0].name,parents[1].name]:null,
    habit:null,habitShown:false,habitOrigin:null,
  };
  // children imitate mistakes: a habit seen at home may simply continue
  if(parents)for(const p of parents){
    if(p.habitShown&&!a.habit&&S.rand()<.25){a.habit=p.habit;a.habitOrigin={kind:'inherited',from:p.name};break;}
  }
  if(parents){ // Rule 3: norms are taught to children (slot conflicts: one way per slot)
    const bySlot={};
    const inherit=(p)=>{for(const id of p.customs)if(S.rand()<.8){const c=S.customs[id];if(c&&c.slot){bySlot[c.slot]=id;}else a.customs.add(id);}};
    inherit(parents[0]);inherit(parents[1]);
    for(const sl in bySlot)a.customs.add(bySlot[sl]);
    for(const o of parents[0].obs)if(S.rand()<.4)a.obs.add(o);
  }
  S.maxGeneration=Math.max(S.maxGeneration,a.gen);
  S.traitSum.curiosity+=a.traits.curiosity;S.traitSum.social+=a.traits.social;S.traitSum.diligence+=a.traits.diligence;S.traitSum.n++;
  return a;
}

function createWorld(seed){
  const S={
    seed:seed>>>0, rand:mulberry32(seed>>>0),
    tick:0,hour:6,day:1,
    tiles:null,agents:[],fires:[],huts:[],villages:[],regrows:[],
    events:[],knowledge:{},customs:{},nextCustomId:1,
    nextId:1,maxGeneration:1,usedNames:0,ended:false,
    maxPop:4,stats:{talks:0,births:0,deaths:{starvation:0,cold:0,age:0,wolves:0},failedExperiments:0,observations:0,conversions:0,hunts:0,harshWinters:0},
    animals:[],nextAnimalId:1,fields:[],season:'spring',winterSeverity:1,seenWinter:false,brewing:null,
    traitSum:{curiosity:0,social:0,diligence:0,n:0},
    onEvent:null,silent:false,bgDirty:true,
  };
  makeWorld(S);
  for(let i=0;i<4;i++){
    let x,y;
    do{x=RI(S,20,44);y=RI(S,14,30);}while(S.tiles[y][x].t==='water');
    const a=makeAgent(S,x,y,null);a.born=-a.age;S.agents.push(a);
  }
  // the living world: deer herds and wolf packs
  for(let h=0;h<3;h++){
    let cx,cy;do{cx=RI(S,6,W-7);cy=RI(S,6,H-7);}while(S.tiles[cy][cx].t==='water');
    for(let i=0;i<RI(S,4,6);i++)S.animals.push({id:S.nextAnimalId++,type:'deer',x:clamp(cx+R(S,-2,2),1,W-2),y:clamp(cy+R(S,-2,2),1,H-2),herd:h,h:0});
  }
  for(let p=0;p<2;p++){
    let cx,cy;do{cx=RI(S,4,W-5);cy=RI(S,4,H-5);}while(S.tiles[cy][cx].t==='water');
    for(let i=0;i<2;i++)S.animals.push({id:S.nextAnimalId++,type:'wolf',x:clamp(cx+R(S,-1,1),1,W-2),y:clamp(cy+R(S,-1,1),1,H-2),pack:p,h:RI(S,0,200)});
  }
  ev(S,'start',`🌍 Four humans wake in an untouched world: <b>${S.agents.map(a=>a.name).join('</b>, <b>')}</b>. They know nothing — but they can observe everything. What will they create?`,{});
  return S;
}

// ---------- behavior ----------
function speak(S,a,txt){a.say=txt;a.sayT=40;}
function disp(a){return a.epithet?a.name+' '+a.epithet:a.name;}
function giveEpithet(S,a,ep){
  if(a.epithet||a.dead)return;
  a.epithet=ep;
  ev(S,'epithet',`✨ From that day on, <b>${a.name}</b> was known as <b>${a.name} ${ep}</b>.`,{agent:a.id,x:a.x,y:a.y});
}
function worldKnows(S,id){return !!S.knowledge[id]&&S.knowledge[id].status==='alive';}
function tryObserve(S,a,obsId,chance){
  if(a.obs.has(obsId))return;
  if(getLens(S,a)==='empiric')chance*=1.35;
  if(S.rand()<chance*(0.5+a.traits.curiosity)){
    a.obs.add(obsId);S.stats.observations++;
    if(S.rand()<.25)ev(S,'observed',`👁️ <b>${a.name}</b> noticed ${OBS[obsId].txt}. An idea begins to form.`,{obs:obsId,agent:a.id,x:a.x,y:a.y});
    if(S.rand()<.3)speak(S,a,pick(S,CHAT.observe));
  }
}
function pickAlt(S,a,t){
  // property thinking: any material set that satisfies the recipe works
  for(const alt of t.alts){
    if(Object.entries(alt).every(([m,q])=>(a.inv[m]||0)>=q))return alt;
  }
  return null;
}
function canAttempt(S,a,t){
  if(a.knows.has(t.id))return false;
  if(!t.pre.every(p=>a.knows.has(p)))return false;
  if(!t.insights.every(o=>a.obs.has(o)))return false;   // must have SEEN it before imagining it
  return !!pickAlt(S,a,t);
}
function neededMaterial(S,a){
  for(const t of TECHS){
    if(a.knows.has(t.id))continue;
    if(!t.pre.every(p=>a.knows.has(p)))continue;
    if(!t.insights.every(o=>a.obs.has(o)))continue;
    const alt=t.alts.find(alt=>!Object.entries(alt).some(([m])=>isTaboo(S,a,m)));
    if(!alt)continue;
    for(const[m,q]of Object.entries(alt)){if((a.inv[m]||0)<q)return m;}
  }
  return null;
}
function isTaboo(S,a,material){return hasCustomKind(S,a,'taboo',material);}
// TABOOS ARE SOCIAL COST, NOT LAW: norm -> social cost -> personality -> decision.
// Desperation can override anything; rebels, guilt and schisms follow.
function wouldBreakTaboo(S,a,c){
  const cost=(c.norm?1:.6)*a.traits.conformity;
  const desperation=(a.hunger<25||a.warmth<25)?1:(a.hunger<45?.45:.12);
  return S.rand()<Math.max(0,desperation-cost*.7);
}
function commitTabooBreak(S,a,c,material){
  a.social=clamp(a.social-30,0,100);
  c.trust=Math.max(.1,c.trust-.15); // broken bans lose their hold on minds
  ev(S,'tabooBroken',`⚡ <b>${disp(a)}</b> has broken ${c.name} — gathering ${material} to survive. Guilt follows; so may division.`,{custom:c.id,agent:a.id,x:a.x,y:a.y});
  for(const w of S.agents){if(!w.dead&&w!==a&&dist(w,a)<6&&w.customs.has(c.id))w.rel[a.id]=(w.rel[a.id]||20)-15;}
  giveEpithet(S,a,'the Oathbreaker');
  if(S.rand()<.3){a.customs.delete(c.id);ev(S,'rebel',`<b>${a.name}</b> no longer keeps ${c.name}. A rebel — or the first of a new way.`,{custom:c.id});}
}
function nearestOf(list,a){let b=null,bd=1e9;for(const o of list){const d=dist(o,a);if(d<bd){bd=d;b=o;}}return b;}
function nearWarmth(S,a){return S.fires.some(f=>dist(f,a)<2.5)||S.huts.some(h=>dist(h,a)<2);}
function findNearest(S,ag,type){
  let best=null,bd=1e9;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){
    const t=S.tiles[y][x];
    if(t.t===type&&t.n>0){const d=Math.hypot(x-ag.x,y-ag.y);if(d<bd){bd=d;best={x,y};}}
  }
  return best;
}
function moveToward(S,a,t){
  const d=Math.hypot(t.x-a.x,t.y-a.y)||1;
  const nx=a.x+(t.x-a.x)/d*.45,ny=a.y+(t.y-a.y)/d*.45;
  if(S.tiles[Math.round(clamp(ny,0,H-1))][Math.round(clamp(nx,0,W-1))].t!=='water'){a.x=nx;a.y=ny;}
  else wander(S,a);
  a.x=clamp(a.x,0,W-1);a.y=clamp(a.y,0,H-1);
}
function wander(S,a){
  const nx=clamp(a.x+R(S,-.6,.6),0,W-1),ny=clamp(a.y+R(S,-.6,.6),0,H-1);
  if(S.tiles[Math.round(ny)][Math.round(nx)].t!=='water'){a.x=nx;a.y=ny;}
}
function doSeek(S,a,tileType,onArrive){
  if(tileType==='grass'){
    let gx=Math.round(a.x),gy=Math.round(a.y);
    gx=clamp(gx,0,W-1);gy=clamp(gy,0,H-1);
    if(S.tiles[gy][gx].t==='grass'){a.tx0=gx;a.ty=gy;S.tiles[gy][gx].n=1;onArrive();return;}
    wander(S,a);return;
  }
  const t=findNearest(S,a,tileType);
  if(!t){wander(S,a);a.task='searching for '+tileType;return;}
  if(Math.hypot(t.x-a.x,t.y-a.y)<1.2){a.tx0=t.x;a.ty=t.y;onArrive();}
  else{moveToward(S,a,t);a.task='heading to '+tileType;}
}
function regrowLater(S,x,y,type){S.tiles[y][x].n=0;const mult=(type==='berry'&&seasonOf(S)==='winter')?2.5:1;S.regrows.push({x,y,type,at:S.tick+Math.floor(RI(S,150,350)*mult)});}

function talk(S,a,b){
  S.stats.talks++;
  a.social=100;b.social=100;a.talkCd=25;b.talkCd=25;
  a.rel[b.id]=(a.rel[b.id]||20)+10;b.rel[a.id]=(b.rel[a.id]||20)+10;
  a.task='talking';b.task='talking';
  // shared ways bind: communities with common rituals hold together
  let shared=0;
  for(const id of a.customs)if(b.customs.has(id)){const sc=S.customs[id];if(sc&&sc.status==='alive')shared++;}
  if(shared){
    const bond=Math.min(6,shared*2);
    a.rel[b.id]+=bond;b.rel[a.id]+=bond;
    // and they do not let each other starve — faith's evolutionary value
    if(b.hunger<30&&a.hunger>65){
      a.hunger-=15;b.hunger=clamp(b.hunger+25,0,140);
      if(S.rand()<.15)ev(S,'sharing',`🍞 <b>${a.name}</b> shared food with <b>${b.name}</b>. Those who keep the same ways do not let each other starve.`,{x:a.x,y:a.y});
    }else if(a.hunger<30&&b.hunger>65){
      b.hunger-=15;a.hunger=clamp(a.hunger+25,0,140);
      if(S.rand()<.15)ev(S,'sharing',`🍞 <b>${b.name}</b> shared food with <b>${a.name}</b>. Those who keep the same ways do not let each other starve.`,{x:a.x,y:a.y});
    }
  }
  let taught=false;
  for(const k of a.knows){
    if(!b.knows.has(k)&&S.rand()<.4){
      gainKnowledge(S,b,k,'taught');taught=true;
      if(S.rand()<.3)ev(S,'taught',`<b>${a.name}</b> taught <b>${b.name}</b> the secret of ${S.knowledge[k]?S.knowledge[k].name:TECH[k].base}. The knowledge spreads.`,{tech:k});
      break;
    }
  }
  spreadCustoms(S,a,b);
  if(taught)speak(S,a,pick(S,CHAT.teach));
  else if((a.rel[b.id]||0)>70)speak(S,a,pick(S,CHAT.love));
  else speak(S,a,pick(S,CHAT.small));
  if(a.rel[b.id]>60&&b.rel[a.id]>60&&a.age>16&&b.age>16&&a.age<50&&b.age<50
     &&a.childCd===0&&b.childCd===0&&a.hunger>40&&b.hunger>40&&S.agents.filter(x=>!x.dead).length<42
     &&S.rand()<(worldKnows(S,'farming')||worldKnows(S,'fishing')?0.20:0.15)){ // Engine 1.2 (D-050): food security emboldens families
    const child=makeAgent(S,a.x,a.y,[a,b]);child.born=S.tick/YEAR;
    if(worldKnows(S,'writing')){for(const k of a.knows)if(S.rand()<.5)child.knows.add(k);}
    S.agents.push(child);S.stats.births++;
    S.maxPop=Math.max(S.maxPop,S.agents.filter(x=>!x.dead).length);
    {const cd=(worldKnows(S,'farming')||worldKnows(S,'fishing'))?460:600;a.childCd=cd;b.childCd=cd;} // Engine 1.2: secure food shortens the wait
    ev(S,'child',`👶 <b>${a.name}</b> and <b>${b.name}</b> have had a child, <b>${child.name}</b> — generation ${child.gen}. They inherit traits from both.`,{agent:child.id,x:a.x,y:a.y});
    maybeEmergeCustom(S,a,'child');
  }
}

function tryBuildHut(S,a){
  if((a.inv.wood||0)<8)return;
  a.inv.wood-=8;
  const h={x:a.x,y:a.y,owner:a.name};S.huts.push(h);a.home=h;S.bgDirty=true;
  ev(S,'hut',`🛖 <b>${a.name}</b> built a hut.`,{x:a.x,y:a.y});
  const cluster=S.huts.filter(o=>dist(o,h)<7);
  if(cluster.length>=3&&!S.villages.some(v=>dist(v,h)<9)){
    giveEpithet(S,a,'the Founder');
    const vname=a.name.split(' ')[0]+pick(S,['stead','vik','heim','holm','haven']);
    S.villages.push({x:h.x,y:h.y,name:vname});
    ev(S,'village',`🏘️ Three huts have become more — <b>${vname}</b> has been founded! A village born of nothing but cooperation.`,{village:vname,x:h.x,y:h.y});
  }
}

function killAgent(S,a,causeKey,causeTxt){
  for(const kid of a.knows){const k=S.knowledge[kid];if(k)k.lastKnownBy=disp(a);}
  S.stats.deaths[causeKey]=(S.stats.deaths[causeKey]||0)+1;
  ev(S,'death',`<b>${disp(a)}</b> ${causeTxt}.${a.knows.size>3?' Much knowledge died with them — unless someone learned in time.':''}`,{agent:a.id,x:a.x,y:a.y,cause:causeKey});
  a.dead=true;if(a.home)a.home.free=true;S.someoneDied=true;
  let closest=null,cb=40;
  for(const w of S.agents){
    if(w.dead||w===a)continue;
    if((w.rel[a.id]||0)>cb){cb=w.rel[a.id];closest=w;}
    if(dist(w,a)<=5&&(w.rel[a.id]||0)>40)maybeEmergeCustom(S,w,'death',causeKey!=='age'?causeKey:null);
  }
  // grief plants habits: Torv does not whistle because he has a property — he whistles since his brother died
  if(closest&&!closest.habit&&closest.age>=10&&S.rand()<.3){
    closest.habit=QUIRKS[Math.floor(S.rand()*QUIRKS.length)].id;
    closest.habitOrigin={kind:'grief',from:disp(a)};
  }
  // a local legend: the habit outlives the person
  if(a.habitShown&&S.agents.some(w=>!w.dead&&w!==a&&w.habit===a.habit)){
    const q=QUIRK[a.habit];
    ev(S,'legend',`🕊️ ${disp(a)} is gone. But someone still ${q.txt.charAt(0).toLowerCase()+q.txt.slice(1).replace(/\.$/,'')}. No one asks why it began.`,{agent:a.id});
  }
}
function agentTick(S,a){
  const night=S.hour<5||S.hour>21;
  const winter=S.season==='winter';
  a.hunger-=0.35;a.energy-=night?0.5:0.25;a.social-=0.3;
  if(a.talkCd>0)a.talkCd--;
  const coldDrain=winter?2.2*S.winterSeverity+0.6:S.season==='summer'?1.6:2.2;
  if(night){
    if(nearWarmth(S,a))a.warmth+=2.5;
    else if(S.agents.some(o=>o!==a&&!o.dead&&dist(o,a)<1.4))a.warmth+=Math.max(0.2,0.9-(winter?0.4*(S.winterSeverity-1):0));
    else a.warmth-=coldDrain;
  } else a.warmth+=winter?0.6:1.5;
  a.hunger=clamp(a.hunger,0,100+(a.knows.has('pottery')?40:0));
  a.energy=clamp(a.energy,0,100);a.warmth=clamp(a.warmth,0,100);a.social=clamp(a.social,0,100);
  if(a.inspired>0)a.inspired--;
  if(a.childCd>0)a.childCd--;
  if(a.sayT>0)a.sayT--;
  a.age=S.tick/YEAR-(a.born||0);

  if(a.hunger<=0||a.warmth<=0||a.age>a.lifespan){
    const causeKey=a.hunger<=0?'starvation':a.warmth<=0?'cold':'age';
    const cause=a.hunger<=0?'starved to death':a.warmth<=0?(winter?'froze to death in the winter cold':'froze to death'):'died of old age at '+Math.floor(a.age);
    killAgent(S,a,causeKey,cause);
    return;
  }
  const child=a.age<14;

  // the world is observed simply by being lived in
  if(S.tick%3===0&&!a.dead){
    const tt=S.tiles[Math.round(a.y)][Math.round(a.x)].t, cu=a.traits.curiosity;
    if(tt==='stone'){tryObserve(S,a,'sharpShards',.012*cu+.004);tryObserve(S,a,'stonesGrind',.004*cu);}
    else if(tt==='grass')tryObserve(S,a,'fibersTwist',.006*cu+.002);
    else if(tt==='sand')tryObserve(S,a,'sandGlints',.010*cu);
    else if(tt==='forest'){tryObserve(S,a,'logsRoll',.008*cu);tryObserve(S,a,'frictionHeat',.006*cu);}
  }
  // heat observations: the fire cult sees more (culture drives technology)
  if(nearWarmth(S,a)){
    const boost=hasCustomKind(S,a,'value','fire')?2:hasCustomKind(S,a,'belief','fire')?1.5:1;
    if((a.inv.clay||0)>0)tryObserve(S,a,'heatHardens',.05*boost);
    if((a.inv.iron||0)>0)tryObserve(S,a,'oreMelts',.04*boost);
    if((a.inv.sand||0)>0)tryObserve(S,a,'sandGlints',.04*boost);
    if((a.inv.clay||0)>0)tryObserve(S,a,'marksRemain',.03*boost);
  }

  if(a.hunger<45){
    const b=findNearest(S,a,'berry');
    if(a.knows.has('spear')&&(!b||(winter&&Math.hypot(b.x-a.x,b.y-a.y)>6))){
      const deer=nearestOf(S.animals.filter(an=>an.type==='deer'),a);
      const bowR=a.knows.has('bow');if(deer&&dist(deer,a)<(bowR?20:14)){
        if(dist(deer,a)<(bowR?3.0:1.3)){
          S.animals.splice(S.animals.indexOf(deer),1);
          S.stats.hunts++;a.huntCount=(a.huntCount||0)+1;
          if(a.huntCount>=5)giveEpithet(S,a,'the Hunter');
          a.hunger=clamp(a.hunger+75,0,140);a.task='feasting on the hunt';
          if(S.rand()<.12)ev(S,'hunt',`🗡️ <b>${disp(a)}</b> brought down a deer${winter?' in the deep of winter':''}. Tonight, no one goes hungry.`,{agent:a.id,x:a.x,y:a.y});
          maybeEmergeCustom(S,a,'hunt');
          return;
        }
        moveToward(S,a,deer);a.task='hunting';return;
      }
    }
    // Engine 1.1 (D-049): FARMING — autumn harvest of your own ripe field beats foraging
    if(a.knows.has('farming')&&S.season==='autumn'){
      const f=S.fields.find(f2=>f2.owner===a.name&&f2.stage>=3);
      if(f){if(Math.hypot(f.x-a.x,f.y-a.y)<1.4){f.stage=-1;a.hunger=clamp(a.hunger+80,0,140);a.task='harvesting';S.stats.harvests=(S.stats.harvests||0)+1;if(S.rand()<.35)ev(S,'field',`🌾 <b>${disp(a)}</b> brings in the harvest from ${f.name||'the field'}. Winter holds less fear now.`,{agent:a.id,x:f.x,y:f.y});maybeEmergeCustom(S,a,'hunt');return;}moveToward(S,a,f);a.task='going to the harvest';return;}
    }
    // Engine 1.1: FISHING — open water feeds those who know the line (not in winter)
    if(a.knows.has('fishing')&&S.season!=='winter'){
      const w=findNearest(S,a,'water');
      if(w&&Math.hypot(w.x-a.x,w.y-a.y)<12){
        if(Math.hypot(w.x-a.x,w.y-a.y)<1.7){a.hunger=clamp(a.hunger+42,0,140);a.task='fishing';if(S.rand()<.03)ev(S,'hunt',`🎣 <b>${disp(a)}</b> pulled silver from the water.`,{agent:a.id,x:a.x,y:a.y});return;}
        moveToward(S,a,w);a.task='going fishing';return;
      }
    }
    doSeek(S,a,'berry',()=>{S.tiles[a.ty][a.tx0].n--;if(S.tiles[a.ty][a.tx0].n<=0)regrowLater(S,a.tx0,a.ty,'berry');a.hunger=clamp(a.hunger+45+(worldKnows(S,'mill')?20:0),0,140);a.task='eating';tryObserve(S,a,'seedsSprout',.09);{const w2=findNearest(S,a,'water');if(w2&&Math.hypot(w2.x-a.x,w2.y-a.y)<4)tryObserve(S,a,'fishGather',.12);}if(S.rand()<.1)speak(S,a,pick(S,CHAT.hungry));});return;
  }
  if(night&&a.warmth<70){
    const f=nearestOf(S.fires.concat(S.huts),a);
    if(f&&dist(f,a)<25){moveToward(S,a,f);a.task='seeking warmth';if(S.rand()<.05)speak(S,a,pick(S,CHAT.cold));return;}
    if(a.knows.has('fire')&&(a.inv.wood||0)>=2){a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='lighting a fire';S.bgDirty=true;return;}
    const buddy=nearestOf(S.agents.filter(o=>o!==a&&!o.dead),a);
    if(buddy&&dist(buddy,a)>1.2){moveToward(S,a,buddy);a.task='huddling for warmth';return;}
  }
  if(night&&hasCustomKind(S,a,'value','night')&&!S.agents.some(o=>o!==a&&!o.dead&&dist(o,a)<3)){
    const buddy2=nearestOf(S.agents.filter(o=>o!==a&&!o.dead),a);
    if(buddy2){moveToward(S,a,buddy2);a.task='keeping the rule of the shared fire';return;}
  }
  // the fire cult keeps a flame burning even when warm
  if(!night&&S.hour===19&&hasCustomKind(S,a,'value','fire')&&a.knows.has('fire')&&(a.inv.wood||0)>=2&&!S.fires.some(f=>dist(f,a)<6)){
    a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='tending the flame';S.bgDirty=true;return;
  }
  if(a.energy<15)a.sleeping=true;
  if(a.sleeping){a.task='sleeping';a.energy+=4;if(a.energy>=80)a.sleeping=false;else return;}
  if(S.tick%8===0&&a.hunger>45&&maybeTill(S,a))return; // Engine 1.1: spring tilling when not starving

  // dusk gathering customs: more talk, faster spread (culture drives knowledge)
  if(S.hour===20&&!child){
    const g=hasCustomKind(S,a,'gathering');
    if(g){
      const f=nearestOf(S.fires,a)||nearestOf(S.agents.filter(o=>o!==a&&!o.dead),a);
      if(f&&dist(f,a)>2){moveToward(S,a,f);a.task='joining the evening '+(g.word||'song');return;}
      a.talkCd=0;
    }else if(S.rand()<.004){maybeEmergeCustom(S,a,'dusk');}
  }

  if(!child){
    if(a.expTech){
      a.expT--;a.task='experimenting';
      if(a.expT<=0){
        const t=a.expTech;a.expTech=null;
        // OBSERVATION → HYPOTHESIS → EXPERIMENT → FAILURE → KNOWLEDGE
        // INNOVATION IS ALMOST NEVER CREATED — IT IS RECOMBINED.
        // Success rises with the stock of old ideas available for new combinations.
        const p=0.25+0.55*a.traits.curiosity+(a.inspired>0?0.35:0)+Math.min(.12,.012*a.knows.size);
        const alt=a.expAlt&&Object.entries(a.expAlt).every(([m,q])=>(a.inv[m]||0)>=q)?a.expAlt:pickAlt(S,a,t);
        if(alt&&S.rand()<p&&canAttempt(S,a,t)){
          Object.entries(alt).forEach(([m,q])=>a.inv[m]-=q);
          gainKnowledge(S,a,t.id,'invented',alt);
          if(t.id==='hut')tryBuildHut(S,a);
        }else{
          S.stats.failedExperiments++;
          if(S.rand()<.35)speak(S,a,pick(S,CHAT.fail));
          if(S.rand()<.15)ev(S,'failed',`<b>${a.name}</b> tried something with ${alt?Object.keys(alt).join(' and '):'what was at hand'} — and failed. But failure teaches.`,{agent:a.id}),a.obs.size&&null;
          if(alt&&S.rand()<.3&&a.inv[Object.keys(alt)[0]]>0)a.inv[Object.keys(alt)[0]]--;
        }
        a.expAlt=null;
      }
      return;
    }
    // When is it rational to try something new instead of surviving? Only with slack.
    const slack=a.hunger>55&&a.warmth>45&&a.energy>30;
    // INNOVATION NEEDS SURPLUS: surplus energy -> specialization -> experiments.
    // Not the smartest villages invent — the ones with food to spare.
    let fed=0;for(const o of S.agents){if(!o.dead&&o!==a&&o.hunger>60&&dist(o,a)<8&&++fed>=3)break;}
    const attempt=(slack||a.inspired>0)?TECHS.find(t=>canAttempt(S,a,t)&&!t.alts.every(alt=>Object.keys(alt).some(m=>isTaboo(S,a,m)))):null;
    if(attempt&&(S.rand()<(a.traits.curiosity*.5+(a.inspired>0?.5:0))*(1+.12*fed))){
      a.expTech=attempt;a.expAlt=pickAlt(S,a,attempt);a.expT=6;return;
    }
    if(a.knows.has('hut')&&!a.home){
      const free=S.huts.find(h=>h.free&&dist(h,a)<10);
      if(free){free.free=false;free.owner=a.name;a.home=free;ev(S,'moved',`<b>${a.name}</b> moved into an abandoned hut.`,{});}
      else if((a.inv.wood||0)>=8){const tc=isTaboo(S,a,'wood');if(!tc){tryBuildHut(S,a);return;}if(wouldBreakTaboo(S,a,tc)){commitTabooBreak(S,a,tc,'wood');tryBuildHut(S,a);return;}}
    }
  }

  if((a.talkCd||0)<=0){
    const other=S.agents.find(o=>o!==a&&!o.dead&&dist(o,a)<3.5&&(o.talkCd||0)<=0);
    if(other&&(a.social<70||S.rand()<.2)){talk(S,a,other);return;}
    if(a.social<55){
      const near=nearestOf(S.agents.filter(o=>o!==a&&!o.dead),a);
      if(near){moveToward(S,a,near);a.task='seeking company';return;}
    }
  }

  if(child){
    const p=S.agents.find(o=>!o.dead&&a.parents&&o.name===a.parents[0]);
    if(p&&dist(p,a)>3)moveToward(S,a,p);a.task='growing up';return;
  }

  let need=neededMaterial(S,a)||((a.inv.wood||0)<(a.knows.has('hut')&&!a.home?9:4)?'wood':null);
  if(need){const tc=isTaboo(S,a,need);if(tc){if(wouldBreakTaboo(S,a,tc))a._breakC=tc;else need=null;}}
  if(need&&S.rand()<a.traits.diligence){
    doSeek(S,a,MATSOURCE[need],()=>{
      S.tiles[a.ty][a.tx0].n--;
      if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[need]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;S.tiles[a.ty][a.tx0]={t:'grass',n:0};if(typ==='forest')regrowLater(S,a.tx0,a.ty,'forest');S.bgDirty=true;}
      const mult=a.knows.has('metaltools')?3:a.knows.has('axe')||a.knows.has('sharp')?2:1;
      a.inv[need]=(a.inv[need]||0)+mult;
      a.task='gathering '+need;
      if(a._breakC){commitTabooBreak(S,a,a._breakC,need);a._breakC=null;}
      for(const[o,ch]of(GATHER_OBS[need]||[]))tryObserve(S,a,o,ch);
    });
    return;
  }
  // WEAK TIES: the most important ideas arrive with acquaintances, not family.
  // A visitor's mere presence lets talk, teaching and custom-spread do the rest.
  if(a.visit){
    if(a.hunger<25||S.season==='winter'){a.visit=null;}
    else if(S.hour>=6&&S.hour<=20){
      if(Math.hypot(a.visit.x-a.x,a.visit.y-a.y)<3){
        a.task='visiting '+a.visit.name;
        if(!a.visit.arrived){a.visit.arrived=true;ev(S,'journey',`🔥 <b>${disp(a)}</b> reached ${a.visit.name}. Strangers shared a fire that night.`,{agent:a.id,x:a.x,y:a.y});}
        if(--a.visit.t<=0){
          if(a.knows.size>a.visit.k0||[...a.customs].sort().join()!==a.visit.cs)
            ev(S,'journey',`🚶 <b>${disp(a)}</b> came home from ${a.visit.name} carrying more than they left with.`,{agent:a.id,x:a.x,y:a.y});
          a.visit=null;
        }
      }else if(a.hunger>60){moveToward(S,a,a.visit);a.task='walking to '+a.visit.name;return;}
      // hungry travelers pause to eat — the journey waits, survival does not
    }
  }
  // the dissenters' dusk: those who carry the rival way seek each other, apart.
  // No banner, no text — a player who has seen one reformation will FEEL the next one coming.
  if(S.brewing&&S.tick<S.brewing.until&&S.hour>=17&&S.hour<=20&&a.age>=14&&a.hunger>45
     &&a.customs.has(S.brewing.rival)&&Math.hypot(a.x-S.brewing.vx,a.y-S.brewing.vy)<11){
    const mx=S.brewing.vx+3.5,my=S.brewing.vy+3.5;
    if(Math.hypot(a.x-mx,a.y-my)>1.6){moveToward(S,a,{x:mx,y:my});a.task='talking quietly, apart';return;}
    a.task='talking quietly, apart';
  }
  if(a.forage&&a.hunger<45)a.forage=null; // hunger cancels the expedition; night only pauses it
  if(!child&&((a.forage&&a.hunger>45)||(a.hunger>65&&S.hour>=7&&S.hour<18))){
    if(!a.forage&&S.rand()<a.traits.curiosity*.06){
      const cand=['stone','fiber','sand','clay','iron','wood'].filter(m=>!isTaboo(S,a,m));
      if(cand.length){
        const m=pick(S,cand);
        const t=findNearest(S,a,MATSOURCE[m]);
        if(m==='fiber'||t&&Math.hypot(t.x-a.x,t.y-a.y)<=25)a.forage=m;
      }
    }
    if(a.forage){
      const m=a.forage;
      doSeek(S,a,MATSOURCE[m],()=>{
        S.tiles[a.ty][a.tx0].n--;
        if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[m]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;S.tiles[a.ty][a.tx0]={t:'grass',n:0};if(typ==='forest')regrowLater(S,a.tx0,a.ty,'forest');S.bgDirty=true;}
        a.inv[m]=(a.inv[m]||0)+1;a.task='collecting curiosities';a.forage=null;
        for(const[o,ch]of(GATHER_OBS[m]||[]))tryObserve(S,a,o,ch*1.5);
      });
      return;
    }
  } else a.forage=null;
  a.task='wandering';wander(S,a);
}

function animalsTick(S){
  const winter=S.season==='winter', night=S.hour<5||S.hour>21;
  if(winter&&S.tick%144===0&&!S.animals.some(a=>a.type==='wolf')&&S.rand()<.25){
    const edge=RI(S,0,3),px=edge<2?RI(S,2,W-3):(edge===2?2:W-3),py=edge<2?(edge===0?2:H-3):RI(S,2,H-3);
    const pk2=S.nextAnimalId;
    for(let i=0;i<RI(S,2,3);i++)S.animals.push({id:S.nextAnimalId++,type:'wolf',x:clamp(px+R(S,-1,1),1,W-2),y:clamp(py+R(S,-1,1),1,H-2),pack:pk2,h:RI(S,200,400)});
    ev(S,'season',`🐺 In the dead of winter, a new wolf pack crossed into the world. The nights have teeth again.`,{});
  }
  const deer=S.animals.filter(an=>an.type==='deer');
  // spring breeding
  if((S.season==='spring'||(S.season==='summer'&&S.rand()<.3))&&S.tick%12===0&&deer.length>1&&deer.length<30&&S.rand()<.6){
    const m=pick(S,deer);
    S.animals.push({id:S.nextAnimalId++,type:'deer',x:m.x,y:m.y,herd:m.herd,h:0});
  }
  for(let i=S.animals.length-1;i>=0;i--){
    const an=S.animals[i];
    if(an.type==='deer'){
      const threat=nearestOf(S.agents.filter(a=>!a.dead),an);
      const wolfNear=nearestOf(S.animals.filter(w=>w.type==='wolf'),an);
      let dx=R(S,-.5,.5),dy=R(S,-.5,.5);
      if(threat&&dist(threat,an)<3.5){dx=(an.x-threat.x);dy=(an.y-threat.y);}
      else if(wolfNear&&dist(wolfNear,an)<3){dx=(an.x-wolfNear.x)*.8;dy=(an.y-wolfNear.y)*.8;}
      else{
        const mates=S.animals.filter(o=>o.type==='deer'&&o.herd===an.herd&&o!==an);
        if(mates.length){const cx2=mates.reduce((s2,o)=>s2+o.x,0)/mates.length,cy2=mates.reduce((s2,o)=>s2+o.y,0)/mates.length;dx+=(cx2-an.x)*.05;dy+=(cy2-an.y)*.05;}
      }
      const L=Math.hypot(dx,dy)||1;
      const nx=clamp(an.x+dx/L*.5,1,W-2),ny=clamp(an.y+dy/L*.5,1,H-2);
      if(S.tiles[Math.round(ny)][Math.round(nx)].t!=='water'){an.x=nx;an.y=ny;}
    }else{ // wolf
      an.h++;
      if(an.h>1500){S.animals.splice(i,1);continue;}
      let prey=null;
      if(an.h>250){const nd=nearestOf(deer,an);if(nd&&dist(nd,an)<14)prey=nd;}
      let human=null;
      if(!prey&&winter&&night&&an.h>300){
        const lone=S.agents.filter(a=>!a.dead&&a.age>=14&&!S.fires.some(f=>dist(f,a)<4)&&!S.agents.some(o=>o!==a&&!o.dead&&dist(o,a)<3));
        const nh=nearestOf(lone,an);
        if(nh&&dist(nh,an)<10)human=nh;
      }
      const tgt=prey||human;
      if(tgt){
        if(dist(tgt,an)<1.4){
          if(prey){
            S.animals.splice(S.animals.indexOf(prey),1);
            for(const w2 of S.animals)if(w2.type==='wolf'&&w2.pack===an.pack&&dist(w2,an)<5)w2.h=0;
            const wolves2=S.animals.filter(w2=>w2.type==='wolf');
            if(S.season==='spring'&&wolves2.length<6&&S.rand()<.15)S.animals.push({id:S.nextAnimalId++,type:'wolf',x:an.x,y:an.y,pack:an.pack,h:0});
          }else{
            ev(S,'wolfAttack',`🐺 Wolves found <b>${disp(human)}</b> alone in the winter night. ${S.rand()<.5?'They fled, bleeding, toward the firelight.':'Screams carried far across the snow.'}`,{agent:human.id,x:human.x,y:human.y});
            human.warmth=clamp(human.warmth-30,0,100);human.hunger=clamp(human.hunger-15,0,140);
            speak(S,human,'Wolves! WOLVES!');
            if(S.rand()<.6)giveEpithet(S,human,'the Wolf-Marked');
            if((human.warmth<15||human.hunger<15)&&S.rand()<.4)killAgent(S,human,'wolves','was taken by wolves in the dark of winter');
            an.h=Math.max(0,an.h-400);
          }
        }else{
          const d2=dist(tgt,an);
          const nx=clamp(an.x+(tgt.x-an.x)/d2*.72,1,W-2),ny=clamp(an.y+(tgt.y-an.y)/d2*.72,1,H-2);
          if(S.tiles[Math.round(ny)][Math.round(nx)].t!=='water'){an.x=nx;an.y=ny;}
        }
      }else{
        const nx=clamp(an.x+R(S,-.4,.4),1,W-2),ny=clamp(an.y+R(S,-.4,.4),1,H-2);
        if(S.tiles[Math.round(ny)][Math.round(nx)].t!=='water'){an.x=nx;an.y=ny;}
      }
    }
  }
}
function fieldsTick(S){
  if(S.tick%12!==0)return;
  for(const f of S.fields){
    if(S.season==='winter'){f.stage=0;continue;}
    if(f.stage===-1){if(S.season==='spring')f.stage=0;continue;}
    if((S.season==='spring'||S.season==='summer')&&f.stage<3&&S.tick%24===0)f.stage++;
  }
}
function maybeTill(S,a){
  if(!a.knows.has('farming')||S.season!=='spring')return false;
  if(S.fields.some(f=>f.owner===a.name))return false;
  if(S.fields.length>=Math.max(3,S.agents.filter(x=>!x.dead).length))return false;
  const ax=Math.round(a.x),ay=Math.round(a.y);
  for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const x=ax+dx,y=ay+dy;
    if(x<1||y<1||x>=W-1||y>=H-1)continue;const t=S.tiles[y][x];
    if(t.t!=='grass')continue;if(S.fields.some(f=>f.x===x&&f.y===y))continue;
    const f={x,y,owner:a.name,stage:0,name:a.name+"'s field"};S.fields.push(f);a.task='tilling the earth';
    ev(S,'field',`🌱 <b>${disp(a)}</b> has turned the soil — ${S.fields.length===1?"the valley's first field":'a new field'} lies open to the sky.`,{agent:a.id,x,y});
    return true;
  }
  return false;
}
function tickWorld(S){
  if(S.ended)return;
  S.tick++;S.hour=(S.hour+1)%24;if(S.hour===0)S.day++;
  fieldsTick(S);
  S.someoneDied=false;
  for(const a of S.agents)if(!a.dead)agentTick(S,a);
  if(S.someoneDied){
    S.agents=S.agents.filter(a=>!a.dead||a.keep);
    checkExtinct(S);
  }
  // seasons turn — Engine 1.2.1 (D-068; mainline order labeled it D-062): the %36 grid predates D-049's season lengths — the winter window (tick 116-143) was never sampled; the engine was behaviorally winterless. Season now evaluates EVERY tick; transition work is already gated by if(ns!==S.season).
  {
    const ns=seasonOf(S);
    if(ns!==S.season){
      S.season=ns;S.bgDirty=true;
      if(ns==='winter'){
        S.winterSeverity=0.8+S.rand()*0.7;
        if(!S.seenWinter){S.seenWinter=true;ev(S,'season',`❄️ The first winter has come. The world turns white and hard, and the nights grow long. Fire is no longer comfort — it is life.`,{});}
        else if(S.winterSeverity>1.35&&(Math.floor(S.tick/YEAR)+1)-(S.lastHarshYear||0)>=12){
          S.lastHarshYear=Math.floor(S.tick/YEAR)+1;S.stats.harshWinters++;
          ev(S,'season',`❄️ A hard winter grips the world. The old will remember it; not all the young will see spring.`,{});
        }
      }
      if(ns==='spring'&&S.seenWinter&&S.rand()<.3)ev(S,'season',`🌸 Spring returns. The survivors count each other, and begin again.`,{});
    }
  }
  animalsTick(S);
  if(S.tick%YEAR===0)cultureYearTick(S);
  for(const f of S.fires)f.fuel--;
  S.fires=S.fires.filter(f=>f.fuel>0);
  for(let i=S.regrows.length-1;i>=0;i--){
    const r=S.regrows[i];
    if(S.tick>=r.at){if(S.tiles[r.y][r.x].t===r.type||S.tiles[r.y][r.x].t==='grass'){S.tiles[r.y][r.x]={t:r.type,n:RI(S,3,6)};S.bgDirty=true;}S.regrows.splice(i,1);}
  }
  if(!S.agents.some(a=>!a.dead)&&!S.ended){
    S.ended=true;S.endedYear=Math.floor(S.tick/YEAR)+1;
    ev(S,'end',`🕯️ The last human is gone after ${S.endedYear} years. The world stands silent — but the chronicle remembers everything they created.`,{});
  }
}

// ---------- Civilization DNA ----------
function computeDNA(S){
  const years=Math.floor(S.tick/YEAR)+1;
  const n=Math.max(1,S.traitSum.n);
  const raw={Curious:S.traitSum.curiosity/n,Social:S.traitSum.social/n,Diligent:S.traitSum.diligence/n};
  const sum=raw.Curious+raw.Social+raw.Diligent;
  const dna=Object.fromEntries(Object.entries(raw).map(([k,v])=>[k,Math.round(v/sum*100)]));
  const firsts={};
  for(const e of S.events){
    if(e.type==='tech'&&!firsts['⚙️ '+TECH[e.tech].base])firsts['⚙️ '+TECH[e.tech].base]='year '+e.year;
    if(e.type==='village'&&!firsts['🏘️ First village'])firsts['🏘️ First village']='year '+e.year;
    if(e.type==='child'&&!firsts['👶 First child'])firsts['👶 First child']='year '+e.year;
    if(e.type==='tradition'&&!firsts['🌿 First tradition'])firsts['🌿 First tradition']='year '+e.year;
    if(e.type==='religion'&&!firsts['⛩️ First religion'])firsts['⛩️ First religion']='year '+e.year;
  }
  const losses=Object.values(S.knowledge).reduce((s,k)=>s+k.losses,0);
  const rediscoveries=Object.values(S.knowledge).reduce((s,k)=>s+k.rediscoveries,0);
  const customsAll=Object.values(S.customs);
  return {
    seed:S.seed,years,dna,
    population:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,generations:S.maxGeneration,
    knowledgeCount:Object.keys(S.knowledge).length,knowledgeMax:TECHS.length,
    villages:S.villages.map(v=>v.name),
    births:S.stats.births,talks:S.stats.talks,deaths:S.stats.deaths,
    knowledgeLosses:losses,rediscoveries,
    failedExperiments:S.stats.failedExperiments,observations:S.stats.observations,
    customsCount:customsAll.length,
    traditions:customsAll.filter(c=>c.norm).map(c=>c.name),
    taboos:customsAll.filter(c=>c.kind==='taboo'&&c.status==='alive').map(c=>c.name),
    beliefs:customsAll.filter(c=>c.kind==='belief'&&c.status==='alive').map(c=>c.name),
    conversions:S.stats.conversions,
    hunts:S.stats.hunts,harshWinters:S.stats.harshWinters,
    reformations:S.events.filter(e=>e.type==='reformation').length,
    worldview:(()=>{let f=0,em=0;for(const a2 of S.agents){if(a2.dead)continue;const l=getLens(S,a2);if(l==='faith')f++;else if(l==='empiric')em++;}return em>f?'empiric':f>em?'faith':f?'divided':'none';})(),
    tabooBreaks:S.events.filter(e=>e.type==='tabooBroken').length,
    religions:customsAll.filter(c=>c.religion).map(c=>c.religionName),
    legends:S.events.filter(e=>e.type==='epithet').map(e=>stripTags(e.txt).replace(/From that day on, |was known as |✨ /g,'').split('  ').pop()).slice(0,8),
    milestones:firsts,
    status:S.ended?`Extinct in year ${S.endedYear}`:`Alive in year ${years}`,
  };
}

// ---------- The History Book: the world writes its own saga ----------
// Pure function: reads the event log, never touches S.rand - determinism untouched.
function stripTags(t){return String(t).replace(/<[^>]*>/g,'');}
function writeHistory(S){
  const rng=mulberry32((S.seed^0x9E3779B9)>>>0);
  const pk=a=>a[Math.floor(rng()*a.length)];
  const totalYears=Math.floor(S.tick/YEAR)+1;
  const CH=30;
  const byCh=new Map();
  for(const e of S.events){const ci=Math.floor((e.year-1)/CH);if(!byCh.has(ci))byCh.set(ci,[]);byCh.get(ci).push(e);}
  const worldName=S.villages[0]?S.villages[0].name:('World #'+S.seed);
  const chapters=[];
  const cis=[...byCh.keys()].sort((a,b)=>a-b);
  for(const ci of cis){
    const evs=byCh.get(ci), y0=ci*CH+1, y1=Math.min((ci+1)*CH,totalYears);
    const text=[];
    let births=0,deaths={starvation:0,cold:0,age:0,wolves:0},techs=[],losses=[],redisc=[],vills=[],trads=[],rels=[],tabs=[],breaks=[],gods=0,hasStart=false,hasEnd=false,convs=0,reforms=[],fades=[],lenses=[],hard=[],wolves=[],hunts=[],sharing=0,epis=[],quirks=[],legends=[],journeys=[];
    for(const e of evs){
      if(e.type==='child')births++;
      else if(e.type==='death')deaths[e.cause||'age']=(deaths[e.cause||'age']||0)+1;
      else if(e.type==='tech')techs.push(e);
      else if(e.type==='knowledgeLost')losses.push(e);
      else if(e.type==='rediscovered')redisc.push(e);
      else if(e.type==='village')vills.push(e);
      else if(e.type==='tradition')trads.push(e);
      else if(e.type==='religion')rels.push(e);
      else if(e.type==='custom'&&(e.label==='A TABOO TAKES HOLD'||e.label==='A CONVICTION IS BORN'))tabs.push(e);
      else if(e.type==='custom'&&e.label==='A NEW WAY OF SEEING')lenses.push(e);
      else if(e.type==='conversion')convs++;
      else if(e.type==='reformation')reforms.push(e);
      else if(e.type==='normFades')fades.push(e);
      else if(e.type==='season'&&/hard winter/.test(e.txt))hard.push(e);
      else if(e.type==='wolfAttack')wolves.push(e);
      else if(e.type==='hunt')hunts.push(e);
      else if(e.type==='sharing')sharing++;
      else if(e.type==='epithet')epis.push(e);
      else if(e.type==='quirk')quirks.push(e);
      else if(e.type==='legend')legends.push(e);
      else if(e.type==='journey'&&/came home|shared a fire/.test(e.txt))journeys.push(e);
      else if(e.type==='tabooBroken')breaks.push(e);
      else if(e.type==='god')gods++;
      else if(e.type==='start'){hasStart=true;text.push(stripTags(e.txt));}
      else if(e.type==='end'){hasEnd=true;}
    }
    for(const e of techs){
      const t=TECH[e.tech], k=S.knowledge[e.tech];
      text.push(pk(['In the year ','It was in the year ','The chronicle marks the year '])+e.year+', when '+(k?k.inventedBy:'someone')+' '+t.flavor+'. '+(k?k.name:t.base)+' entered the world, and nothing was quite the same after.');
    }
    for(const e of vills)text.push('In the year '+e.year+', three huts grew into more, and '+e.village+' was founded - a place with a name, which is the beginning of belonging.');
    for(const e of tabs){const c=S.customs[e.custom];if(c)text.push('In the year '+e.year+', '+c.origin+' began '+c.txt+', and others watched, and followed. So '+c.name+' took hold.');}
    for(const e of trads){const c=S.customs[e.custom];if(c)text.push('By the year '+e.year+', '+c.txt+' had become simply what one does in '+(c.normVillage||'the village')+'. '+pk(['No one decided this.','No council voted.','It merely became true.']));}
    for(const e of rels){const c=S.customs[e.custom];if(c)text.push('And in the year '+e.year+', what had been habit became holy: '+c.religionName+' was born, though none could say when reverence had replaced routine.');}
    for(const e of losses){const t=TECH[e.tech],k2=S.knowledge[e.tech];text.push('Then came a quieter grief: in the year '+e.year+', '+(k2&&k2.lastKnownBy?k2.lastKnownBy+' — the last soul who understood '+t.base.toLowerCase()+' — died':'the last soul who understood '+t.base.toLowerCase()+' died')+', and the knowledge went dark.');}
    for(const e of redisc){const t=TECH[e.tech];text.push('Yet in the year '+e.year+' it returned - '+t.base.toLowerCase()+', rediscovered, as if the world refused to forget forever.');}
    for(const e of breaks){const c=S.customs[e.custom];text.push('The year '+e.year+' is remembered darkly: '+(c?c.name:'the old ban')+' was broken out of desperation, and the guilt of it did not wash away.');}
    for(const e of lenses){const c=S.customs[e.custom];if(c)text.push('In the year '+e.year+', '+c.origin+' began seeing the world differently - '+c.txt+'. '+(c.lens==='empiric'?'Where others saw spirits, they saw patterns.':'Where others saw patterns, they felt a will behind them.'));}
    for(const e of reforms){const c=S.customs[e.custom];const o=S.customs[e.from];text.push('The year '+e.year+' brought upheaval: in '+stripTags(e.txt).split(',')[0].replace('⚡ In ','')+', '+(o?o.name:'the old way')+' was set aside, and '+(c?c.name:'a new way')+' took its place. The elders muttered; the young did not listen.');}
    for(const e of fades)text.push(stripTags(e.txt));
    for(const e of hard)text.push('The winter of year '+e.year+' was a hard one. Food ran thin, the cold pressed close, and the fires were fed like gods.');
    if(wolves.length)text.push(wolves.length===1?'Once that age, wolves found a human alone in the winter dark. The tale was told at every fire for years.':'Wolves haunted the winters of this age — '+wolves.length+' times they came for those who walked alone.');
    if(hunts.length)text.push('The spear fed the people: hunters brought down deer when the bushes stood bare, and the winters lost some of their terror.');
    for(const e of epis)text.push(stripTags(e.txt).replace('✨ ',''));
    // Folklore: history tells what happened; folklore tells what people chose to remember.
    // The historian weighs, never counts: a legend may carry the whole chapter; in quiet years the small detail IS the story.
    for(const e of legends)text.push(stripTags(e.txt).replace(/^🕊️ |^🌳 /,''));
    if(journeys.length){const jj=journeys.find(e=>/came home/.test(e.txt))||journeys[0];text.push(stripTags(jj.txt).replace(/^🚶 |^🔥 /,'')+' So it has always gone: ideas travel on foot.');}
    if(quirks.length){
      const room=Math.max(1,Math.min(quirks.length,text.length<5?3:1));
      const qs=[...quirks];while(qs.length>room)qs.splice(Math.floor(rng()*qs.length),1);
      for(const e of qs)text.push(pk(['It is also remembered that ','The chronicle notes, without explanation, that ','And in those same years, '])+stripTags(e.txt).replace('🙂 ',''));
    }
    if(sharing)text.push('And when the food ran low, those who kept the same ways fed one another. The rituals, whatever the skeptics said, kept people alive.');
    if(convs)text.push(convs===1?'One soul, that age, quietly left an old way for a new one.':convs+' times in this age, someone left one way for another. Minds were changing, and with them, the world.');
    const dTot=deaths.starvation+deaths.cold+deaths.age;
    if(births||dTot){
      let sDeath='';
      if(dTot){
        const parts=[];
        if(deaths.age)parts.push(deaths.age+' to age');
        if(deaths.starvation)parts.push(deaths.starvation+' to hunger');
        if(deaths.cold)parts.push(deaths.cold+' to cold');
        if(deaths.wolves)parts.push(deaths.wolves+' to wolves');
        sDeath=' and '+dTot+' lives ended - '+parts.join(', ');
      }
      text.push(pk(['In these years ','Meanwhile ','Through it all '])+(births?births+(births===1?' child was':' children were')+' born under its skies':'no children were born')+sDeath+'.');
    }
    if(gods)text.push(gods===1?'Once in this age, the sky itself placed new bounty upon the land, and none could say why.':gods+' times in this age, the sky itself placed new bounty upon the land.');
    let title;
    if(hasStart)title='The Waking';
    else if(reforms.length)title='The Reformation';
    else if(wolves.length>=2)title='The Year of Wolves';
    else if(hard.length)title='The Hunger Winter';
    else if(rels.length){const c=S.customs[rels[0].custom];title='The Age of '+((c&&c.religionName)?c.religionName.replace('The Way of the ',''):'Faith');}
    else if(breaks.length)title='The Time of the Broken Ban';
    else if(vills.length)title='The Age of '+vills[0].village;
    else if(losses.length&&!techs.length)title='The Forgetting';
    else if(techs.length)title='The Years of '+TECH[techs[0].tech].base;
    else if(trads.length)title='The Settling of Ways';
    else if(lenses.length)title='The Age of New Eyes';
    else if(convs>=2)title='The Changing of Minds';
    else if(!births&&dTot)title='The Dwindling';
    else if(births&&!dTot)title='The Flourishing';
    else title=pk(['The Quiet Generations','The Long Middle Years','The Unrecorded Years']);
    if(hasEnd)title='The Last Chapter';
    if(hasEnd)text.push('Here the last human died, and the world fell silent. What follows is only memory.');
    if(text.length)chapters.push({title,from:y0,to:y1,text});
  }
  const alive=S.agents.filter(a=>!a.dead).length;
  return {
    worldName, seed:S.seed, years:totalYears,
    intro:'This is the chronicle of '+worldName+' - '+totalYears+' years, '+S.maxGeneration+' generations, written by no one. Every line below happened.',
    outro:S.ended?'Here the chronicle ends. The world stands silent, but it remembers.':'The chronicle is still being written. '+alive+' souls carry it onward.',
    chapters,
  };
}

function resimulate(seed,toTick){
  const S=createWorld(seed);
  S.silent=true;
  while(S.tick<toTick&&!S.ended)tickWorld(S);
  S.silent=false;
  return S;
}

return {createWorld,tickWorld,computeDNA,resimulate,writeHistory,TECHS,TECH,OBS,QUIRK,W,H,YEAR,SEASONS};
});
