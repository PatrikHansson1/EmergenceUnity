/* ============================================================================
   EMERGENCE — Civilization Engine — ENGINE 2.4.1 (Wave E1.5b: HARDENING, review conditions V1/V2/V6/V8, D-176)
   E1.5b closes the engine-side conditions of the E1.5 wave review (E15-WAVE-REVIEW-2026-07-25):
   V1 (I2): leaderScore REBUILT — the prestige clamp saturated every adult to the same value,
       reducing recognition to argmax(ambition) with a ratio margin that shrank monotonically
       with village size (leadership got HARDER as villages grew — inverted vs the anthropology
       it cites). Standing is now UNCLAMPED and accumulates over a life (what you know, the
       ways you carry, the years you have seen, the visible surplus at your door), and the
       margin is an ABSOLUTE lead (TUNE.leaderLead), not a percentage of the runner-up —
       a clear lead is a visible distance, and heavy-tailed wealth makes that distance GROW
       with village scale (the aggrandizer path; Flannery & Marcus, RESEARCH-FRONTIER 2.1).
   V2 (I6): the revenge trigger reads the SAME truth-source as the bookkeeping — a.grudges
       (in conjunction with still-hot rel), never rel alone. A feud can only fire on a BOOKED
       wrong, so every feud event carries a resolving ev: reference BY CONSTRUCTION.
   V6 (I1): theft chains to its DRIVE. Winter now enters the log at every onset (season event,
       plain text for ordinary winters), starvation deaths are bookkept per village, and a
       desperation steal cites the freshest of them (winter within half a year, village famine
       within a year). Raids cite the target's hoard-milestone (new rare 'hoard' event, once
       per life at TUNE.hoardMark) or the leadership event whose tribute fed the pile. The
       order's example chain "winter took the harvest -> Torv stole" now EXISTS in the log.
   V8 (I8): feud lines in writeHistory are capped by the same room law as the other categories.
   No RNG draws added anywhere (mulberry32 only; primary/secondary stream discipline unchanged).
   VERSION honestly bumped to 2.4.1.
   ============================================================================
   (2.4.0 header follows, still true:)
   EMERGENCE — Civilization Engine — ENGINE 2.4.0 (Wave E1.5: DRAMATIK-MINIMUM, D-166 B1)
   E1.5 (MOTOR-LANE-ORDER-E15-DRAMATIK, EP-sanctioned): causal human drama OUT
   OF the four forces (Resurser x Individer x Friktion x Handling) — never
   scripted. The merged tension spike is made HONEST and NARRATABLE:
   (1) friction from scarcity drives conflict (pressure(), cached per tick);
   (2) aspiration hoards surplus -> wealth/inequality (wealthOf exported);
   (3) causal violence, three rungs — steal (desperation) -> raid (greed) ->
       feud/blood-revenge (honour) — read from NEW DEDICATED inherited traits
       aggression / impulse(-control) / vindictiveness (the proto's proxies
       were dishonest); every act is an event with id + causes[] (R2 grammar)
       and a grudge BOOKKEEPS the wrong it answers (ev:-chains in the feud);
   (4) prestige hardens into a RECOGNIZED LEADER (RESEARCH-FRONTIER 2.1) with
       tribute flowing upward as a sim fact; (5) the existing ritual-kin food
       sharing is FORMALIZED as a named gift-way (first Polanyi step — NO
       markets, NO prices, NO currency). New sayActs: steal/raid/feud/mourn/
       submit/gift (body falls back to null tempo on unknown acts).
   VERSION honestly bumped to 2.4.0 (the 2.3.x string debt is paid).
   ============================================================================
   (2.0.1 header follows, still true:)
   EMERGENCE — Civilization Engine — ENGINE 2.0.1 (Wave E1: THE WIDER WORLD + THE WATER FIX)
   E1 bundles every known World-Code-breaking change into this ONE major
   (D-072/D-078): the wider valley (100x70, D-061 Stage-A) + world-gen v2
   (same grammar, scaled density) + THE FOUNDERS input (createWorld(seed,
   founders?) — names/traits for the first four; part of the World Code) +
   MALTHUS (the land's berry-sites + fields set the birth ceiling — the
   hardcoded 42 is gone) + SPEECH ACTS (phrase choice by position hash,
   never S.rand — pools can grow or be replaced by presentation without
   forking worlds; the engine emits acts). Engine 1.2.1 world codes are
   retired with this version per ENGINE-CONTRACT §3.5/§5.
   ============================================================================
   (v4 header follows, still true:)
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

const W=100,H=70; // ENGINE 2.0 (E1, D-061 Stage-A): 2.49x area — villages get distance, journeys become journeys

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
  copperGreen:{txt:'that green stone sweats a soft red metal in the fire'},
  bronzeHard:{txt:'that two soft metals poured together wake up hard'},
  coalBurns:{txt:'that the black stone burns hotter and longer than wood'},
  steelKeen:{txt:'that iron worked long in a hotter fire holds a keener edge'},
  pigmentStains:{txt:'that crushed coloured earth stains a lasting mark'},
  goldGleams:{txt:'that the yellow metal never dulls and all hands want it'},
  soundsRing:{txt:'that a struck string and a hollow log ring with a voice'},
  weightsBalance:{txt:'that things can be counted, matched and made equal'},
  starsWheel:{txt:'that the stars and seasons turn on a fixed wheel'},
  herbsHeal:{txt:'that certain leaves ease pain and close a wound'},
  archStands:{txt:'that stones leant just so hold up more than their own weight'},
  lensBends:{txt:'that clear glass bends light and makes the small large'},
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
   flavor:'carved signs into clay so knowledge could outlive its owner', effect:'Knowledge spreads to all — and to generations to come.', era:2, branch:'cul'},
 {id:'brick',     icon:'🧱', base:'Brick',           alts:[{clay:3}],                pre:['pottery','kiln'], insights:['heatHardens'], var:['fired brick','mudbrick','block'], era:1, branch:'mat',
   flavor:'fired shaped clay into blocks that stack true', effect:'Walls that outlast weather — the start of building.'},
 {id:'well',      icon:'🕳️', base:'Well',            alts:[{stone:3,clay:2}],        pre:['brick'], insights:[], var:['stone well','cistern','draw-well'], era:1, branch:'mat',
   flavor:'lined a deep pit with stone to hold clean water', effect:'Water without walking to the river — a village can grow inland.'},
 {id:'granary',   icon:'🏚️', base:'Granary',         alts:[{wood:4,clay:2}],         pre:['farming','pottery'], insights:[], var:['grain store','silo','store-house'], era:1, branch:'mat',
   flavor:'built a dry raised store so the harvest kept till spring', effect:'Surplus survives winter — the first true wealth.'},
 {id:'weaving',   icon:'🧶', base:'Weaving',          alts:[{fiber:4}],               pre:['rope'], insights:['fibersTwist'], var:['the loom','woven cloth','warp-craft'], era:2, branch:'mat',
   flavor:'crossed threads over threads until cloth grew on the frame', effect:'Cloth for warmth and trade — and hands that specialize.'},
 {id:'copper',    icon:'🟤', base:'Copper',           alts:[{copper:2}],              pre:['kiln'], insights:['copperGreen'], var:['red metal','copperwork','the soft metal'], era:2, branch:'mat',
   flavor:'coaxed a soft red metal out of green stone in the kiln', effect:'The first metal — soft, but the door to bronze.'},
 {id:'tin',       icon:'⚪', base:'Tin',              alts:[{tin:2}],                 pre:['kiln'], insights:[], var:['tinwork','the pale metal','white metal'], era:2, branch:'mat',
   flavor:'smelted a pale metal too soft to be worth much alone', effect:'Useless alone — everything when married to copper.'},
 {id:'bronze',    icon:'🔶', base:'Bronze',           alts:[{copper:2,tin:1}],        pre:['copper','tin'], insights:['bronzeHard'], var:['bronzecraft','the hard alloy','bell-metal'], era:2, branch:'mat',
   flavor:'poured copper and tin together and woke a hard new metal', effect:'The Bronze Age. Tools, blades and bells no stone could match.'},
 {id:'bronzetools',icon:'🛠️', base:'Bronze tools',   alts:[{bronze:1,wood:2}],       pre:['bronze'], insights:[], var:['bronze axe','bronze plough','castings'], era:2, branch:'mat',
   flavor:'cast bronze into edges that held far longer than stone', effect:'Faster everything — clearing, tilling, building.'},
 {id:'sailing',   icon:'⛵', base:'Sailing',          alts:[{wood:6,fiber:3}],        pre:['weaving','axe'], insights:[], var:['the boat','the sail','river-craft'], era:2, branch:'mat',
   flavor:'stretched cloth on a hull and let the wind do the work', effect:'Rivers and coasts become roads — trade and migration by water.'},
 {id:'coal',      icon:'⚫', base:'Coal',             alts:[{coal:2}],                pre:['kiln'], insights:['coalBurns'], var:['coalcraft','black-stone fire','fuel'], era:3, branch:'mat',
   flavor:'found the black stone burns hotter than any wood', effect:'Heat enough for steel and, one day, engines.'},
 {id:'steel',     icon:'🗡️', base:'Steel',           alts:[{iron:2,coal:2}],         pre:['smithing','coal'], insights:['steelKeen'], var:['steelcraft','the keen edge','tempered blade'], era:3, branch:'mat',
   flavor:'worked iron in a coal-hot fire until it took a keener soul', effect:'The hardest working metal — blades, springs, tools that endure.'},
 {id:'masonry',   icon:'🧱', base:'Masonry',          alts:[{stone:6}],               pre:['brick'], insights:['archStands'], var:['stonecraft','dry-stone','the mason'], era:3, branch:'mat',
   flavor:'dressed and fitted stone so it stood without mortar', effect:'Buildings that outlive their builders — the seed of monuments.'},
 {id:'architecture',icon:'🏛️', base:'Architecture',  alts:[{stone:8,wood:4}],        pre:['masonry'], insights:['archStands'], var:['the arch','the vault','great halls'], era:3, branch:'mat',
   flavor:'raised the arch, and space opened under stone', effect:'Temples, halls and towers — the shape of a civilization made visible.'},
 {id:'road',      icon:'🛤️', base:'Road',            alts:[{stone:5}],               pre:['wheel','masonry'], insights:[], var:['paved road','the highway','trackway'], era:3, branch:'mat',
   flavor:'laid stone in a bed so carts ran true in any weather', effect:'Villages bound together — trade, news and armies move.'},
 {id:'aqueduct',  icon:'🌉', base:'Aqueduct',         alts:[{stone:8}],               pre:['architecture','well'], insights:[], var:['the channel','water-bridge','conduit'], era:3, branch:'mat',
   flavor:'carried water across the valley on arches of stone', effect:'Cities freed from the river — population without limit of the well.'},
 {id:'glassblowing',icon:'🫧', base:'Glassblowing',  alts:[{sand:4}],                pre:['glass','kiln'], insights:[], var:['blown glass','the vessel','clearware'], era:4, branch:'mat',
   flavor:'blew molten sand into clear vessels thin as a bubble', effect:'Windows, flasks and, one day, lenses.'},
 {id:'windmill',  icon:'🌬️', base:'Windmill',        alts:[{wood:8,stone:2}],        pre:['mill','architecture'], insights:[], var:['the windmill','sail-mill','tower-mill'], era:4, branch:'mat',
   flavor:'set great sails to catch the wind and turn the stones', effect:'Grinding and pumping without river or muscle.'},
 {id:'clock',     icon:'⏳', base:'Mechanical clock', alts:[{bronze:2,wood:2}],       pre:['bronzetools','architecture'], insights:['weightsBalance'], var:['the clockwork','the escapement','tower-clock'], era:4, branch:'mat',
   flavor:'tamed a falling weight into steady, counted time', effect:'Time itself measured — the first true machine.'},
 {id:'optics',    icon:'🔎', base:'Optics',           alts:[{sand:3}],                pre:['glassblowing'], insights:['lensBends'], var:['the lens','spectacles','the glass eye'], era:5, branch:'mat',
   flavor:'ground clear glass until it bent light to the eye', effect:'The small made large, the far made near — the door to science.'},
 {id:'printpress',icon:'🖨️', base:'Printing press',  alts:[{steel:1,wood:4}],        pre:['writing','bronzetools'], insights:[], var:['the press','movable type','the print-shop'], era:5, branch:'mat',
   flavor:'set movable letters and pressed a page in a heartbeat', effect:'Knowledge copied a thousandfold — no idea dies again.'},
 {id:'steam',     icon:'♨️', base:'Steam engine',     alts:[{steel:2,coal:3}],        pre:['steel','clock','optics'], insights:[], var:['the engine','the steam-mill','ironhorse'], era:6, branch:'mat',
   flavor:'boiled water to force and set iron to move by fire', effect:'Muscle unbound — the Industrial age begins.'},
 {id:'storytelling',icon:'📖', base:'Storytelling',   alts:[{}],                      pre:[], insights:[], var:['the tale','oral saga','the telling'], era:1, branch:'cul', needsLeisure:true,
   flavor:'told the day back as a story worth remembering', effect:'The first culture — memory shaped into meaning.'},
 {id:'song',      icon:'🎵', base:'Music',            alts:[{}],                      pre:['storytelling'], insights:['soundsRing'], var:['the song','the drum','melody'], era:1, branch:'cul', needsLeisure:true,
   flavor:'found that a struck string and a voice could ring together', effect:'Song at the fire — the root of every art to come.'},
 {id:'numbers',   icon:'🔢', base:'Counting',         alts:[{}],                      pre:[], insights:['weightsBalance'], var:['tally','number','the count'], era:2, branch:'cul', needsLeisure:true,
   flavor:'matched pebble to sheep and found number itself', effect:'The world made countable — trade, calendars and mathematics begin.'},
 {id:'painting',  icon:'🎨', base:'Painting',         alts:[{pigment:2}],             pre:['storytelling'], insights:['pigmentStains'], var:['cave-marks','the mural','pigmentwork'], era:2, branch:'cul', needsLeisure:true,
   flavor:'ground coloured earth and left an image on the wall', effect:'The world seen, kept and shared — visual art is born.'},
 {id:'calendar',  icon:'📅', base:'Calendar',         alts:[{}],                      pre:['numbers'], insights:['starsWheel'], var:['the calendar','star-count','the year-wheel'], era:2, branch:'cul', needsLeisure:true,
   flavor:'counted the stars and seasons into a fixed wheel of days', effect:'The future made plannable — harvest, festival and rite find their day.'},
 {id:'medicine',  icon:'🌿', base:'Medicine',         alts:[{fiber:2}],               pre:[], insights:['herbsHeal'], var:['herb-lore','the healer','remedies'], era:3, branch:'cul', needsLeisure:true,
   flavor:'learned which leaves ease pain and close a wound', effect:'Death held off a while — the healer becomes needed.'},
 {id:'philosophy',icon:'🧠', base:'Philosophy',       alts:[{}],                      pre:['writing'], insights:[], var:['the question','reasoned thought','the argument'], era:3, branch:'cul', needsLeisure:true,
   flavor:'asked not what the world is but why, and wrote it down', effect:'Reasoned thought — the seed of law, science and doubt.'},
 {id:'law',       icon:'⚖️', base:'Law',             alts:[{}],                      pre:['writing','numbers'], insights:[], var:['the code','written law','the judgment'], era:3, branch:'cul', needsLeisure:true,
   flavor:'wrote the customs down so all were judged by one measure', effect:'Custom hardened into law — the institution of justice.'},
 {id:'coinage',   icon:'🪙', base:'Coinage',          alts:[{gold:1}],                pre:['metaltools','numbers'], insights:['goldGleams'], var:['the coin','minted money','currency'], era:3, branch:'cul', needsLeisure:true,
   flavor:'stamped a fixed weight of metal all agreed to trust', effect:'Money — value made portable; markets and wealth take shape.'},
 {id:'school',    icon:'🏫', base:'School',           alts:[{}],                      pre:['writing','numbers'], insights:[], var:['the school','teaching','the lesson'], era:4, branch:'cul', needsLeisure:true,
   flavor:'set the knowing to teach the young in one place', effect:'Knowledge taught on purpose — learning outlives the teacher.'},
 {id:'temple',    icon:'⛩️', base:'Temple',           alts:[{stone:6}],               pre:['masonry'], insights:[], var:['the temple','the shrine-hall','sacred house'], era:4, branch:'cul', needsLeisure:true,
   flavor:'raised a house for the sacred and gathered the faithful in it', effect:'Belief given a home and a keeper — organized religion.'},
 {id:'scholarship',icon:'📚', base:'Scholarship',     alts:[{}],                      pre:['philosophy','school'], insights:[], var:['the library','learned study','the scholar'], era:4, branch:'cul', needsLeisure:true,
   flavor:'gathered writings and studied them as a life\'s work', effect:'Knowledge accumulated and compared — the scholar emerges.'},
 {id:'composition',icon:'🎼', base:'Composition',     alts:[{}],                      pre:['song','writing'], insights:[], var:['written music','the score','the composer'], era:4, branch:'cul', needsLeisure:true,
   flavor:'wrote music down so a song could outlive its singer', effect:'Music made lasting and grand — the composer, the choir, the hall.'},
 {id:'university',icon:'🎓', base:'University',        alts:[{stone:8}],               pre:['scholarship','school'], insights:[], var:['the university','the academy','halls of learning'], era:5, branch:'cul', needsLeisure:true,
   flavor:'joined scholars and students into a lasting house of learning', effect:'Learning institutionalized — the engine of every later age.'},
 {id:'science',   icon:'🔬', base:'Science',          alts:[{}],                      pre:['philosophy','numbers','optics'], insights:[], var:['the method','experiment','natural philosophy'], era:6, branch:'cul', needsLeisure:true,
   flavor:'tested the guess against the world and kept only what held', effect:'The method that unlocks everything after — the modern mind.'},
];
const TECH=Object.fromEntries(TECHS.map(t=>[t.id,t]));
const MATSOURCE={wood:'forest',stone:'stone',fiber:'grass',clay:'clay',iron:'iron',sand:'sand',copper:'copper',tin:'tin',coal:'coal',gold:'gold',pigment:'pigment'};
// F1.2a (D-383): EGENSKAPSRYMDEN. Varje material bär en vektor; dimensionerna är
// utbyggbara, inte en fast lista. INGENTING LÄSER DEN ÄNNU — steget ska lämna
// guldmastern grön. `fuel` finns med för att motorn själv namnger den i sin
// insiktstabell (coalBurns, frictionHeat): thresh är den värme ett material TÅL,
// fuel är den värme det AVGER. Utan den skillnaden kan sten ersätta kol (D-372).
const MATDIM={
  stone:  {fuel:0,hard:6,plastic:0,cohesive:1,thresh:6,dense:6,rough:4,conduct:0,clear:0,elastic:0},
  wood:   {fuel:4,hard:3,plastic:1,cohesive:3,thresh:2,dense:2,rough:2,conduct:0,clear:0,elastic:4},
  clay:   {fuel:0,hard:1,plastic:6,cohesive:3,thresh:3,dense:4,rough:1,conduct:0,clear:0,elastic:0},
  fiber:  {fuel:2,hard:0,plastic:2,cohesive:6,thresh:2,dense:1,rough:1,conduct:0,clear:0,elastic:3},
  sand:   {fuel:0,hard:2,plastic:1,cohesive:0,thresh:5,dense:3,rough:5,conduct:0,clear:2,elastic:0},
  iron:   {fuel:0,hard:8,plastic:2,cohesive:4,thresh:6,dense:7,rough:3,conduct:4,clear:0,elastic:2},
  copper: {fuel:0,hard:4,plastic:3,cohesive:4,thresh:4,dense:6,rough:1,conduct:6,clear:0,elastic:2},
  tin:    {fuel:0,hard:2,plastic:4,cohesive:3,thresh:2,dense:5,rough:1,conduct:4,clear:0,elastic:1},
  coal:   {fuel:8,hard:2,plastic:0,cohesive:1,thresh:7,dense:2,rough:3,conduct:1,clear:0,elastic:0},
  gold:   {fuel:0,hard:2,plastic:5,cohesive:4,thresh:5,dense:8,rough:0,conduct:7,clear:0,elastic:2},
  pigment:{fuel:0,hard:0,plastic:3,cohesive:2,thresh:1,dense:1,rough:1,conduct:0,clear:0,elastic:0},
  bronze: {fuel:0,hard:6,plastic:2,cohesive:5,thresh:4,dense:6,rough:2,conduct:5,clear:0,elastic:2},
  steel:  {fuel:0,hard:9,plastic:1,cohesive:5,thresh:7,dense:7,rough:3,conduct:4,clear:0,elastic:3},
};
// F1.0c (D-360): material som INTE plockas ur marken utan ARBETAS FRAM. Motorns
// tekniker har inget utdatafält, så brons och stål fanns som TEKNIK men aldrig som
// TING — och de fyra tekniker som kräver dem (bronzetools, clock, printpress, steam)
// var onåbara i varje värld. Receptet är teknikens eget; det uppfinns inte här.
const SMELT={bronze:{copper:2,tin:1},steel:{iron:2,coal:2}};
// which observations can occur while gathering which material
const GATHER_OBS={wood:[['frictionHeat',.10],['logsRoll',.07],['branchBends',.08],['soundsRing',.05]],stone:[['sharpShards',.16],['stonesGrind',.06],['archStands',.05]],fiber:[['fibersTwist',.14],['soundsRing',.04]],sand:[['sandGlints',.14],['lensBends',.05]],clay:[['pigmentStains',.06]],iron:[['oreMelts',.10],['steelKeen',.05]],copper:[['copperGreen',.16],['bronzeHard',.06]],tin:[['bronzeHard',.10]],coal:[['coalBurns',.16]],gold:[['goldGleams',.16]],pigment:[['pigmentStains',.18]]};
const NAMES=['Eira','Ask','Embla','Torv','Liv','Sten','Ylva','Bjorn','Saga','Rune','Freja','Kare','Idun','Halvar','Signe','Vidar','Tuva','Alve','Ronja','Sixten','Maja','Loke','Vera','Otto','Selma','Falk','Nanna','Ulv','Disa','Orm'];
const YEAR=144;
const SEASONS=['spring','summer','autumn','winter'];
function seasonOf(S){const t=S.tick%144;return t<40?'spring':t<84?'summer':t<116?'autumn':'winter';} // Engine 1.1 (D-049, EP): shorter winter (28, was 36), longer growing seasons — spring 40, summer 44, autumn 32

const CHAT={
  small:['What a day!','Did you see the sunrise?','The land is good here.','I dreamt last night.','The wind is turning.','The birds are back.','My mother knew this weather.','Quiet today. I like it.','The valley smells of rain.','Have you ever counted the stars?'],
  hungry:['I am so hungry...','Are there berries left?','My stomach growls.','I dreamt of food again.','Winter eats first, we eat after.','My hands shake. I need to eat.'],
  cold:['The cold bites tonight.','I am freezing...','We need warmth.','My fingers have gone stiff.','Closer to the fire. Closer.'],
  discovery:['I have created something new!','Look what I made!','This changes everything!','It works. It actually works!','My hands knew before I did.'],
  teach:['Let me show you something...','This is how it is done...','I just learned this.','Watch my hands, not my face.','Slowly. The way matters.','My teacher showed me. Now I show you.'],
  love:['I like being near you.','We are building something together, you and I.','Stay close.','The day is better when you are in it.','I saved you the sweetest berries.'],
  observe:['Did you see that?','Curious...','I must remember this.','Again. It happened again.','Why does it do that?'],
  fail:['Not like that, then...','Almost. Almost!','Why will it not hold?','The idea was right. The hands were wrong.','Tomorrow it will work.'],
  ritual:['It felt right.','For those before us.','So we remember.','The old ones would approve.','This is how we stay ourselves.'],
  // E1.5 (D-166 B1): the drama speaks. New sayAct pools — same position-hash law as all speech
  // (never S.rand); the body falls back to null tempo on any act it does not know.
  steal:['Forgive me. Hunger owns me tonight.','You had more than you needed.','I could not watch them starve.','The winter left me nothing.','I will not die politely.'],
  raid:['The strong take. The rest remember.','You have plenty. Now less.','This is mine now.','Stop me, then.'],
  feud:['For the one you took.','Blood remembers.','You thought the years would bury it.','My kin cry from the ground.','This debt is old.'],
  mourn:['They took what cannot be given back.','The fire is colder now.','I will not forget this.','Grief is a long road.','Someone will answer for this.'],
  submit:['We follow you.','Lead us, then.','Your word carries now.','Speak, and it is done.'],
  gift:['Take it. No one starves at my fire.',"What is mine is the hearth's.",'We keep the same ways, you and I.','Eat. The gift binds us.'],
};
// ENGINE 2.0 SPEECH ACTS (E1, TD-012/D-078): the engine emits an ACT; the phrase
// is chosen by a deterministic position hash — NEVER by S.rand — so pools can
// grow, be localized, or be replaced by the presentation layer without forking
// worlds. Contract §4: visual/text variety never touches the randomness stream.
function sayHash(a,S){let h=(a.id*2654435761+S.tick*40503)>>>0;h^=h>>>13;h=(h*2246822519)>>>0;h^=h>>>16;return h;}
function pickSay(S,a,cat){const p=CHAT[cat];return p[sayHash(a,S)%p.length];}

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
// R2 INK1 (MOTOR-LANE-ORDER-R2-FAS4 §writeHistory, causes[] substrate): every event carries a
// stable id = its index in S.events at emission. Emitters may pass an additive causes[] array of
// references ('ev:<eventId>' | 'agent:<agentId>' | 'tech:<techId>' | 'cause:<key>') to the events
// that caused this one (depth 1 — the chain is traversed in UI). Pure read-side bookkeeping:
// consumes no S.rand, changes no behavior, removes nothing from the schema.
function ev(S,type,txt,data){
  const e=Object.assign({id:S.events.length,tick:S.tick,day:S.day,year:Math.floor(S.tick/YEAR)+1,type,txt},data||{});
  S.events.push(e);
  if(S.onEvent&&!S.silent)S.onEvent(e,S);
  return e;
}

// ---------- Knowledge Engine ----------
function gainKnowledge(S,a,id,via,altUsed){
  if(a.knows.has(id))return;
  // ENGINE 2.1 (D-086) SPECIALIZATION: no one masters everything. A deep craft (smithing, glass, mill,
  // metal tools) is a life's dedication — a person carries at most DEEP_CAP of them. This caps only
  // TEACHING (invention is always allowed, so the tree still climbs); it means a craft spreads only to
  // those with room to master it, so a small hamlet cannot field enough specialists to hold the deep
  // crafts a town can. Grounded: division of labour needs surplus population (ECONOMY.md; Henrich).
  if(via==='taught' && DEEP_CRAFTS.has(id)){
    let deep=0; for(const t of a.knows)if(DEEP_CRAFTS.has(t))deep++;
    // TRAIT-DRIVEN mastery (EP philosophy): a person's craft capacity EMERGES from aptitude
    // (diligence = focus/skill, curiosity = breadth), not a hardcoded number. A gifted, curious
    // person masters several crafts; a low-aptitude one, just one. "Smith" is then a label for whoever
    // ends up dominated by metalwork — never a role the engine assigns.
    const cap = 1 + Math.round((a.traits.diligence + a.traits.curiosity) * 1.4); // ~2-4 by aptitude
    if(deep>=cap)return; // no room to master another craft
  }
  a.knows.add(id);
  const t=TECH[id];
  let k=S.knowledge[id];
  if(!k){
    const mat=altUsed?Object.keys(altUsed)[0]:null;
    const name=`${a.name}'s ${mat==='iron'&&id==='axe'?'iron ':''}${pick(S,t.var)}`;
    k=S.knowledge[id]={id,name,status:'alive',inventedBy:a.name,yearBorn:Math.floor(S.tick/YEAR)+1,rediscoveries:0,losses:0,madeFrom:altUsed?Object.keys(altUsed).join('+'):''};
    // R2 INK1 causes: an invention is caused by its prerequisites — reference each pre's own
    // invention event where the world recorded one, else the tech id as a state reference.
    const te=ev(S,'tech',`${t.icon} <b>${a.name}</b> ${t.flavor} — <b>${name}</b> (${t.base}) has been invented! <i>${t.effect}</i>`,{tech:id,agent:a.id,x:a.x,y:a.y,causes:(t.pre||[]).map(p=>S.knowledge[p]&&S.knowledge[p].evId!==undefined?'ev:'+S.knowledge[p].evId:'tech:'+p)});
    k.evId=te.id;
    speak(S,a,pickSay(S,a,'discovery'),'discovery');
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
    // R2 INK1 causes: a rediscovery is caused by the loss it undoes.
    ev(S,'rediscovered',`💡 <b>${disp(a)}</b> has rediscovered ${k.name} (${t.base}) — knowledge lost for ${Math.floor(S.tick/YEAR)+1-k.diedYear} years lives again!`,{tech:id,agent:a.id,x:a.x,y:a.y,causes:k.evLost!==undefined?['ev:'+k.evLost]:['tech:'+id]});
  }
}
function checkExtinct(S){
  for(const id in S.knowledge){
    const k=S.knowledge[id];
    if(k.status!=='alive')continue;
    if(!S.agents.some(a=>!a.dead&&a.knows.has(id))){
      k.status='extinct'; k.diedYear=Math.floor(S.tick/YEAR)+1; k.losses++;
      // R2 INK1: record the loss event's id so a future rediscovery can reference the loss it undoes.
      k.evLost=ev(S,'knowledgeLost',`🕯️ With <b>${k.lastKnownBy||'the last of them'}</b> died the last knowledge of ${k.name} (${TECH[id].base}). It is <b>extinct</b> — until someone rediscovers it.`,{tech:id}).id;
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
  speak(S,a,pickSay(S,a,'ritual'),'ritual');
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
  // world-gen v2 (E1): same grammar, two registers — the WILD LAND (uniform,
  // sparser: distance is real) and the HEARTLAND (the central ~30x21 — the old
  // valley's density, embedded): the first four begin here; the wide land is
  // where later generations' journeys, exoduses and far villages belong.
  blob('water',5,9);blob('forest',11,6);blob('stone',8,4);blob('berry',7,3);blob('sand',5,3);
  const hb=(type,count,rad)=>{ // heartland blob: center-biased placement
    for(let i=0;i<count;i++){
      const cx=RI(S,(W*0.35)|0,(W*0.65)|0),cy=RI(S,(H*0.35)|0,(H*0.65)|0),r=RI(S,2,rad);
      for(let y=cy-r;y<=cy+r;y++)for(let x=cx-r;x<=cx+r;x++){
        if(x<0||y<0||x>=W||y>=H)continue;
        if(Math.hypot(x-cx,y-cy)<=r*R(S,.6,1))S.tiles[y][x]={t:type,n:type==='water'?0:RI(S,4,9)};
      }
    }
  };
  hb('water',1,5);hb('forest',5,5);hb('stone',3,3);hb('berry',6,3);
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
  scatter('water','clay',35);scatter('stone','iron',20);
  // deeper geology (ENGINE 2.1, D-086): ores are LOCAL and scarcer the deeper the era — this is the
  // world-motor that gates bronze/steel/coin, so worlds differentiate by what the land beneath offers.
  scatter('stone','copper',14);scatter('stone','tin',9);scatter('stone','coal',12);scatter('stone','gold',5);scatter('clay','pigment',10);
  // loose boulders and patches — the world invites curiosity everywhere
  let placed=0,guard=0;
  const loose=[['stone',25],['sand',12],['clay',10]];
  for(const[type,count]of loose){placed=0;guard=0;
    while(placed<count&&guard++<2000){
      const x=RI(S,2,W-3),y=RI(S,2,H-3);
      if(S.tiles[y][x].t==='grass'){S.tiles[y][x]={t:type,n:RI(S,2,4)};placed++;}
    }
  }
}
// E1.5: the secondary trait stream — mulberry32 ONLY (hard req §1), seeded from the world seed,
// consumed EXCLUSIVELY for the three new conflict traits at birth. This keeps the primary
// stream's draw order at agent creation byte-identical to 2.3.2 (no reroll of canon souls).
function R2T(S,a,b){return a+S.rand2()*(b-a);}
function mut2(S,v){return clamp(v+R2T(S,-.15,.15),.05,1);}
function makeAgent(S,x,y,parents,founder){
  const mut=v=>clamp(v+R(S,-.15,.15),.05,1);
  const a={
    id:S.nextId++, name:NAMES[S.usedNames++%NAMES.length]+(S.usedNames>NAMES.length?' II':''),
    x,y, age:parents?0:RI(S,17,24), gen:parents?Math.max(parents[0].gen,parents[1].gen)+1:1,
    lifespan:RI(S,55,85), hunger:80,energy:90,warmth:90,social:70,
    inv:{},knows:new Set(),obs:new Set(),customs:new Set(),rel:{},task:'thinking',expT:0,expTech:null,expAlt:null,
    say:'',sayT:0,sayAct:null,inspired:0,childCd:0,talkCd:0,phase:R(S,0,6.28),
    hue:parents?(parents[0].hue+parents[1].hue)/2+RI(S,-20,20):RI(S,0,360),
    traits:{
      curiosity:parents?mut((parents[0].traits.curiosity+parents[1].traits.curiosity)/2):R(S,.2,.95),
      social:parents?mut((parents[0].traits.social+parents[1].traits.social)/2):R(S,.2,.95),
      diligence:parents?mut((parents[0].traits.diligence+parents[1].traits.diligence)/2):R(S,.3,.95),
      conformity:parents?mut((parents[0].traits.conformity+parents[1].traits.conformity)/2):R(S,.15,.95),
      dexterity:parents?mut((parents[0].traits.dexterity+parents[1].traits.dexterity)/2):R(S,.15,.95),
      creativity:parents?mut((parents[0].traits.creativity+parents[1].traits.creativity)/2):R(S,.1,.95),
      musicality:parents?mut((parents[0].traits.musicality+parents[1].traits.musicality)/2):R(S,.05,.95),
      empathy:parents?mut((parents[0].traits.empathy+parents[1].traits.empathy)/2):R(S,.1,.95),
      ambition:parents?mut((parents[0].traits.ambition+parents[1].traits.ambition)/2):R(S,.1,.95),
      // E1.5 (D-166 B1): DEDICATED conflict traits, inherited like all others — the tension
      // proto read these as proxies of empathy/ambition/conformity, which was dishonest.
      // Violence-proneness itself now evolves across generations (additive in DNA/arv).
      // They draw from the SECONDARY stream (S.rand2, mulberry32 — see createWorld): the primary
      // stream's consumption at agent creation stays byte-identical to 2.3.2, so every canon
      // soul KEEPS its canon identity (Eira stays Eira); the temperament layer is added on top.
      aggression:parents?mut2(S,(parents[0].traits.aggression+parents[1].traits.aggression)/2):R2T(S,.05,.9),
      impulse:parents?mut2(S,(parents[0].traits.impulse+parents[1].traits.impulse)/2):R2T(S,.15,.95),
      vindictiveness:parents?mut2(S,(parents[0].traits.vindictiveness+parents[1].traits.vindictiveness)/2):R2T(S,.05,.9),
    },
    grudges:{}, // E1.5: wrong -> memory. targetId -> event id of the wrong (steal/raid/killing) — the feud's causes[] chain
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
  // ENGINE 2.0 FOUNDERS (E1, TD-009): player-shaped birth variables for the
  // first four — recorded input, part of the World Code (Contract §1 used as designed).
  if(founder){
    if(founder.name)a.name=String(founder.name).slice(0,24);
    if(founder.traits)for(const tk of ['curiosity','social','diligence','conformity','dexterity','creativity','musicality','empathy','ambition','aggression','impulse','vindictiveness'])
      if(typeof founder.traits[tk]==='number'&&isFinite(founder.traits[tk]))a.traits[tk]=clamp(founder.traits[tk],0.05,0.95);
  }
  S.maxGeneration=Math.max(S.maxGeneration,a.gen);
  S.traitSum.curiosity+=a.traits.curiosity;S.traitSum.social+=a.traits.social;S.traitSum.diligence+=a.traits.diligence;S.traitSum.n++;
  return a;
}

function createWorld(seed,founders){
  const S={
    seed:seed>>>0, rand:mulberry32(seed>>>0),
    rand2:mulberry32((seed^0x00E15DA7)>>>0), // E1.5: secondary mulberry32 stream — birth-time conflict traits ONLY (never in the tick path)
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
    do{x=RI(S,(W*0.43)|0,(W*0.57)|0);y=RI(S,(H*0.43)|0,(H*0.57)|0);}while(S.tiles[y][x].t==='water'); // E1: the first four begin TOGETHER in the heartland's core
    const a=makeAgent(S,x,y,null,founders&&founders[i]);a.born=-a.age;S.agents.push(a);
  }
  // the living world: deer herds and wolf packs
  for(let h=0;h<7;h++){ // E1: herds scaled with the land
    let cx,cy;do{cx=RI(S,6,W-7);cy=RI(S,6,H-7);}while(S.tiles[cy][cx].t==='water');
    for(let i=0;i<RI(S,4,6);i++)S.animals.push({id:S.nextAnimalId++,type:'deer',x:clamp(cx+R(S,-2,2),1,W-2),y:clamp(cy+R(S,-2,2),1,H-2),herd:h,h:0});
  }
  for(let p=0;p<5;p++){ // E1: packs scaled with the land
    let cx,cy;do{cx=RI(S,4,W-5);cy=RI(S,4,H-5);}while(S.tiles[cy][cx].t==='water');
    for(let i=0;i<2;i++)S.animals.push({id:S.nextAnimalId++,type:'wolf',x:clamp(cx+R(S,-1,1),1,W-2),y:clamp(cy+R(S,-1,1),1,H-2),pack:p,h:RI(S,0,200)});
  }
  ev(S,'start',`🌍 Four humans wake in an untouched world: <b>${S.agents.map(a=>a.name).join('</b>, <b>')}</b>. They know nothing — but they can observe everything. What will they create?`,{});
  return S;
}

// ---------- behavior ----------
function speak(S,a,txt,act){a.say=txt;a.sayT=40;a.sayAct=act||null;}
function disp(a){return a.epithet?a.name+' '+a.epithet:a.name;}
function giveEpithet(S,a,ep){
  if(a.epithet||a.dead)return;
  a.epithet=ep;
  ev(S,'epithet',`✨ From that day on, <b>${a.name}</b> was known as <b>${a.name} ${ep}</b>.`,{agent:a.id,x:a.x,y:a.y});
}
// ENGINE 2.0 MALTHUS (E1, audit §4.2): the land, not a number, sets the birth
// ceiling. Berry SITES (grown or regrowing — the land's capacity, not this
// hour's stock, so winter does not crater it) + tended fields + fishing water.
function carryingCapacity(S){
  // Engine 2.3 (D-089): cache the O(W*H) berry census per tick, and BOUND the field contribution so
  // population plateaus at a stable band (fields no longer track population forever = no runaway).
  if(S._capTick!==S.tick){
    let sites=0;
    for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(S.tiles[y][x].t==='berry')sites++;
    for(const r of S.regrows)if(r.type==='berry')sites++;
    S._capSites=sites; S._capFish=S.agents.some(a=>!a.dead&&a.knows.has('fishing')); S._capTick=S.tick;
  }
  // A STABLE, BOUNDED band (Director call): populous enough to feel alive (~80–140), low enough that
  // the sim stays fast and can run toward the 6000-year horizon. Land-bounded — no runaway.
  let cap=10+S._capSites*0.28+Math.min(S.fields.length,24)*1.8;
  if(S._capFish)cap+=8;
  return Math.floor(cap);
}
function worldKnows(S,id){return !!S.knowledge[id]&&S.knowledge[id].status==='alive';}
// ---------- ENGINE 2.1 (D-086, World-Model Phase 1): knowledge is per COMMUNITY, not global ----------
// The saturation fix. Two global loopholes made every village converge into one all-knowing state:
// writing made knowledge heritable EVERYWHERE the moment ANY village wrote, and loss only ever fired
// when a tech died out GLOBALLY. Re-scoping both to the community makes villages diverge, lets a craft
// die with its last local carrier (the smith), and makes WRITING the pivotal discovery that defeats
// mortality. Grounded: Henrich (retention ~ carriers x connectivity) + Rogers (bounded carrier-borne
// spread). The census below is a pure readout — it consumes no S.rand, so it never forks histories;
// only the one guard change at birth-inheritance does (a World-Code break, bundled into E2 per D-078).
const VILLAGE_RADIUS=18; // an agent farther than this from any village is unaffiliated (a wanderer/hermit)
function villageRaw(S,a){
  if(!S.villages.length)return null;
  const px=(a.home?a.home.x:a.x),py=(a.home?a.home.y:a.y);
  let hv=null,hd=1e9;for(const v of S.villages){const d=Math.hypot(px-v.x,py-v.y);if(d<hd){hd=d;hv=v;}}
  return hd<=VILLAGE_RADIUS?hv:null;
}
// per-tick cache: village assignment is recomputed once per tick (assignVillages) and read O(1)
function villageOf(S,a){ return a._vil!==undefined ? a._vil : villageRaw(S,a); }
function assignVillages(S){ for(const a of S.agents){ if(a.dead){a._vil=null;continue;} a._vil=villageRaw(S,a); } }
// does the agent's community hold this knowledge? (pre-village: the whole small world is one community)
function groupKnows(S,a,id){
  const v=villageOf(S,a);
  if(!v)return worldKnows(S,id);
  for(const x of S.agents){if(x.dead)continue;if(x.knows.has(id)&&villageOf(S,x)===v)return true;}
  return false;
}
// tech complexity = prerequisite depth (Rope=1 ... Writing and deeper cost more to sustain).
const _TDEPTH={};
function techDepth(id){ if(_TDEPTH[id])return _TDEPTH[id]; const t=TECH[id]; return _TDEPTH[id]=1+((t&&t.pre&&t.pre.length)?Math.max(...t.pre.map(techDepth)):0); }
// ENGINE 2.1 knobs — the Tasmania mechanism (Henrich): a complex craft needs a minimum pool of skilled
// carriers to survive imperfect transmission; below it, the craft DRIFTS OUT. Writing (external memory)
// lowers the pool a craft needs — a literate town holds depth a small illiterate hamlet cannot. Tuned
// on the farm to differentiate WITHOUT collapsing worlds (kill-criterion (d)).
const KM_BASE=1;         // carriers a depth-1 craft needs to persist in an ILLITERATE community
const KM_PERDEPTH=1.3;   // extra carriers per depth level (a deep craft needs a real pool of hands)
const KM_LOSS=0.06;      // per-TICK chance an under-supported craft drifts out of living memory
const LIT_MIN=3;         // literate members needed to SUSTAIN literacy (scribes teach scribes; below this, a dark age)
const DEEP_CRAFTS=new Set(["writing","kiln","smithing","glass","mill","metaltools"]); // the workshop crafts a person specializes in — being a scribe competes with being a smith
// (craft capacity is now trait-driven per agent — see gainKnowledge; DEEP_CRAFTS still names the specialist crafts)
function carrierNeed(k){ return Math.max(1, Math.round(KM_BASE+(techDepth(k)-1)*KM_PERDEPTH)); }

// per-community knowledge state + LOCAL loss/rediscovery (the smith dies here; the grandchild brings it
// back). The Memory Engine, made mechanical and DEATH-driven (not random — far less flicker):
//   A LITERATE town (>= LIT_MIN living scribes) = writing is the library; its knowledge is what it has
//   EVER held — the record outlives any single carrier (this is what writing DOES).
//   An ILLITERATE village = its knowledge is only what LIVING members still carry; when the last carrier
//   of a craft dies, the craft is lost here (the smith), until a visitor reteaches it or it's rediscovered.
//   Literacy must itself be sustained: below LIT_MIN scribes a town slips into a DARK AGE — it reverts to
//   living-memory only and its deep crafts (which few hands sustain) drift out. Deep crafts are taught
//   slowly (transmission fidelity, above), so illiterate/small communities cannot hold the top of the
//   tree — the Tasmania result. Grounded: Henrich (carriers x connectivity) + Rogers (bounded spread).
// Pure readout (consumes no S.rand): the only histories-forking changes are the teach-fidelity and the
// birth-inheritance guard. Run once per year.
// ENGINE 2.1 (D-086): the WORLD gates the craft (EP's three-motor philosophy — individual x world x
// society). A craft that consumes a SCARCE resource (iron/sand/clay) can only be SUSTAINED where that
// resource is locally accessible: no iron in reach -> no smithing here, however clever the people. This
// is the deepest, most grounded differentiator — geography. Derived from each tech's own material recipe.
const _SCARCE=new Set(['iron','sand','clay','copper','tin','coal','gold','pigment']);
// a craft's scarce material need (from its recipe). null = needs no scarce resource (survival/culture tech).
function techScarceRes(id){ const t=TECH[id]; if(!t||!t.alts||!t.alts.length)return null; for(const m in t.alts[0])if(_SCARCE.has(m))return m; return null; }
// ENGINE 2.6.0 (M3 of D-226/D-234): the reach is not a fixed circle. What a people can FETCH grows
// with what they have learned about MOVING things -- a load on your back, then a cart, then water,
// then a road. Before this, a village not born within 16 tiles of copper could never hold bronze
// however clever or old it became, and copper is 14 tiles of ~7000. Geography decided everything and
// nothing could answer it. Now geography sets the STARTING difficulty and technology answers it,
// which is the actual history of the Bronze Age.
function reachOf(S,v){
  let R=16;                                     // a day out and back with a load on your back
  const held=new Set(); for(const a of (v._mem||[])){ if(a.dead)continue; for(const k of a.knows)held.add(k); }
  if(held.has('wheel'))R=24;                    // a cart carries ore a back cannot
  if(held.has('sailing'))R=36;                  // water is the first highway
  if(held.has('road'))R=44;                     // and a road makes distance cheap in any weather
  return R;
}
function resWithin(S,cx,cy,R){
  const res=new Set();
  for(let y=Math.max(0,Math.round(cy-R));y<Math.min(H,cy+R);y++)for(let x=Math.max(0,Math.round(cx-R));x<Math.min(W,cx+R);x++){
    const t=S.tiles[y][x].t; if(_SCARCE.has(t)&&Math.hypot(x-cx,y-cy)<=R)res.add(t);
  }
  return res;
}
function villageResAccess(S,v){
  const R=reachOf(S,v);
  const res=resWithin(S,v.x,v.y,R); // recomputed yearly: captures resource depletion (mined-out ore -> the craft declines)
  // TRADE: a people who can reach another people can reach that people's ground. The tin trade is the
  // oldest long-distance trade there is, and it is the reason bronze exists at all -- almost nowhere
  // are copper and tin found together. A partner's OWN reach is the plain walking one: what they can
  // pass on is what they can pick up, not what they in turn import.
  const held=new Set(); for(const a of (v._mem||[])){ if(a.dead)continue; for(const k of a.knows)held.add(k); }
  if(held.has('road')||held.has('sailing')){
    for(const w of S.villages){ if(w===v)continue;
      if(Math.hypot(w.x-v.x,w.y-v.y)<=R) for(const t of resWithin(S,w.x,w.y,16))res.add(t);
    }
  }
  return res;
}
// which crafts can a village SUSTAIN, given its local geology? A craft needs its own scarce material
// accessible AND all its prerequisite crafts sustainable (prerequisite closure — you cannot hold steel
// where you cannot hold smithing). Resolved in depth order so prereqs settle first.
function sustainableCrafts(S,v){
  const res=villageResAccess(S,v), ok=new Set();
  const ordered=[...TECHS].sort((a,b)=>techDepth(a.id)-techDepth(b.id));
  for(const t of ordered){
    const needRes=techScarceRes(t.id);
    if(needRes&&!res.has(needRes))continue;               // the land here cannot supply it
    if(!t.pre.every(p=>ok.has(p)))continue;               // a prerequisite craft is itself unsustainable here
    ok.add(t.id);
  }
  return ok;
}
// ENGINE 2.1 (D-086) — the CAUSAL world-gate + per-community memory. Runs yearly. Unlike a census, this
// MUTATES what people actually know: a village whose land cannot supply a craft (no copper -> no bronze;
// no coal -> no steel) truly LOSES it from living memory. That is real differentiation, grounded in the
// world-motor (EP's philosophy: simulate causes, not outcomes). The census/event pass then narrates what
// was lost/rediscovered. Consumes no S.rand for the readout; the mutation is deterministic.
function knowledgeRetentionTick(S){
  for(const v of S.villages){ if(!v.everHeld)v.everHeld=new Set(); if(!v.lostNow)v.lostNow=new Set(); v._mem=[]; v._scribes=0; }
  for(const a of S.agents){ if(a.dead)continue; const v=villageOf(S,a); if(!v)continue; v._mem.push(a); }
  for(const v of S.villages){
    // CAUSAL GATE: the land actually takes the craft out of people's hands. Only ADVANCED crafts
    // (era >= 2 — bronze, steel, glass, coin...) are world-gated; survival infrastructure (era <= 1:
    // fire, pottery, farming, hut, mill) is never stripped, so a resource-poor village stays alive and
    // simple rather than starving. Geography decides how FAR a place climbs, not whether it survives.
    const sustain=sustainableCrafts(S,v);
    for(const a of v._mem)for(const k of [...a.knows]) if(techScarceRes(k)!==null && (TECH[k].era||0)>=2 && !sustain.has(k)) a.knows.delete(k);
    // now recount what living hands hold, and let writing preserve the record
    for(const a of v._mem)if(a.knows.has('writing'))v._scribes++;
    const living=new Set(); for(const a of v._mem)for(const k of a.knows)living.add(k);
    for(const k of living)v.everHeld.add(k);
    const literate = v._scribes>=LIT_MIN;
    const holds = literate ? new Set([...v.everHeld].filter(k=>techScarceRes(k)===null||sustain.has(k))) : living;
    v.holds=[...holds]; v.literate=literate;
    for(const k of [...v.lostNow]) if(holds.has(k)){ // rediscovered / relearned / re-literate
      v.lostNow.delete(k); const k2=S.knowledge[k];
      ev(S,'rediscovered',`💡 In <b>${v.name}</b>, ${k2?k2.name:(TECH[k]?TECH[k].base:k)} is known again — a craft the village had lost returns to living hands.`,{tech:k,x:v.x,y:v.y,village:v.name});
    }
    for(const k of v.everHeld) if(!holds.has(k)&&!v.lostNow.has(k)){ // lost here (no local material, or the last carrier died)
      v.lostNow.add(k); const k2=S.knowledge[k];
      ev(S,'knowledgeLost',`🕯️ In <b>${v.name}</b>, ${k2?k2.name:(TECH[k]?TECH[k].base:k)} is lost — the land cannot feed the craft, or the last who knew it is gone. Until it is brought back or rediscovered.`,{tech:k,x:v.x,y:v.y,village:v.name});
    }
  }
}
function tryObserve(S,a,obsId,chance){
  if(a.obs.has(obsId))return;
  if(getLens(S,a)==='empiric')chance*=1.35;
  if(S.rand()<chance*(0.5+a.traits.curiosity)){
    a.obs.add(obsId);S.stats.observations++;
    if(S.rand()<.25)ev(S,'observed',`👁️ <b>${a.name}</b> noticed ${OBS[obsId].txt}. An idea begins to form.`,{obs:obsId,agent:a.id,x:a.x,y:a.y});
    if(S.rand()<.3)speak(S,a,pickSay(S,a,'observe'),'observe');
  }
}
// F1.2b (D-373): KRAVET HÄRLEDS UR TABELLEN, INTE UR EN SKRIVEN LISTA.
// Ett materials KRAVDIMENSIONER är de där det sticker ut mot materialtabellens egen
// spridning; ett annat material duger om det ligger NÄRA i just dem. Band, inte golv:
// ett golv säger "minst lika hett" och kan därför föreslå stål i stället för kol.
const _DIMS=Object.keys(MATDIM.stone), _MATKEYS=Object.keys(MATDIM), _STAT={}, _KEY={}, _DOM={};
for(const d of _DIMS){
  const v=_MATKEYS.map(k=>MATDIM[k][d]||0);
  const mu=v.reduce((a,b)=>a+b,0)/v.length;
  const sd=Math.sqrt(v.reduce((a,b)=>a+(b-mu)*(b-mu),0)/v.length)||1;
  _STAT[d]={mu,sd};
}
function keyDims(m){
  if(_KEY[m])return _KEY[m];
  const V=MATDIM[m]; if(!V)return _KEY[m]=[];
  const z=_DIMS.map(d=>[d,((V[d]||0)-_STAT[d].mu)/_STAT[d].sd]).sort((a,b)=>b[1]-a[1]);
  let out=z.filter(p=>p[1]>=0.8).map(p=>p[0]);
  if(out.length<2)out=z.slice(0,2).map(p=>p[0]);   // alltid minst två — ett materials identitet är aldrig en enda siffra
  return _KEY[m]=out;
}
// G-REVIEW I1: likheten prövas i UNIONEN av bägge materialens särskiljande dimensioner.
// Gamla regeln (bara originalets) släppte in guld som lera och koppar som tenn — ett
// material med EN nyckeldimension ignorerade hela ersättarens identitet. Matrisen är
// utskriven och dömd med öga i MATRIS-DOMD-MED-OGA-2026-08-16.md.
function suits(c,m){
  const k=c+'>'+m; if(_DOM[k]!==undefined)return _DOM[k];
  const A=MATDIM[c],B=MATDIM[m];
  let r=!!(A&&B);
  if(r){
    const dims=keyDims(m).concat(keyDims(c).filter(d=>keyDims(m).indexOf(d)<0));
    for(const d of dims){
      const tol=Math.max(1,0.6*_STAT[d].sd);
      if(Math.abs((A[d]||0)-(B[d]||0))>tol){ r=false; break; }
    }
  }
  return _DOM[k]=r;
}
function pickAlt(S,a,t){
  // exakt recept först — dagens beteende bevaras där det går
  for(const alt of t.alts){
    if(Object.entries(alt).every(([m,q])=>(a.inv[m]||0)>=q))return alt;
  }
  // property thinking, på riktigt: vilken materialuppsättning som helst som
  // uppfyller receptets KRAV duger. Materialen prövas i tabellordning, aldrig
  // slumpat — inget rand-drag konsumeras här.
  for(const alt of t.alts){
    const out={}; let ok=true;
    for(const e of Object.entries(alt)){
      const m=e[0], q=e[1];
      let found=null;
      if((a.inv[m]||0)>=(out[m]||0)+q) found=m;
      else for(const c of _MATKEYS){
        // G-REVIEW I2: tabu prövas mot det som FAKTISKT konsumeras, inte mot originalnamnet
        if(c===m||isTaboo(S,a,c)||!suits(c,m))continue;
        if((a.inv[c]||0)>=(out[c]||0)+q){ found=c; break; }
      }
      if(!found){ ok=false; break; }
      out[found]=(out[found]||0)+q;
    }
    if(ok)return out;
  }
  return null;
}
function canAttempt(S,a,t){
  if(a.knows.has(t.id))return false;
  if(!t.pre.every(p=>a.knows.has(p)))return false;
  if(!t.insights.every(o=>a.obs.has(o)))return false;   // must have SEEN it before imagining it
  // CULTURE NEEDS LEISURE (ENGINE 2.1, D-086, EP's philosophy): art, music, philosophy, faith-houses,
  // scholarship cannot arise in a starving village — they need SURPLUS. Only a well-fed person in a
  // food-secure community has the free time to make them. This is why culture blooms only where the
  // material base can spare the hands — the society-motor gating the culture branch.
  if(t.needsLeisure && !hasLeisure(S,a))return false;
  return !!pickAlt(S,a,t);
}
// leisure = personal slack AND a surrounding community with food to spare (the surplus that frees hands)
function hasLeisure(S,a){
  if(a.hunger<70||a.energy<45||a.warmth<45)return false;
  let fed=0;for(const o of nearby(S,a,9)){if(o.hunger>65&&++fed>=2)return true;}
  return false;
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
// ---------- SPATIAL GRID (Engine 2.3 optimization, D-089) — O(n^2) neighbour scans -> O(local) ----------
// Built once per tick (agents move <=~1 tile/tick, cells are wide, so tick-stale is negligible AND
// fully deterministic). nearby()/nearestAgent() return id-SORTED results so every "first-match" and
// "max/min with first-tie" stays deterministic regardless of grid layout. This is what lets the world
// hold hundreds of souls (and, with a bounded ceiling, run toward the 6000-year horizon) at speed.
const CELL=5;
// FLAT-ARRAY buckets (Engine 2.3.1, D-094 Jint-perf): Jint's Map is ~orders slower than V8's, so the
// grid uses a plain reused array indexed by cell (Jint handles integer array indexing fast). Output is
// BYTE-IDENTICAL to the Map version — nearby() finds the same agents and returns them id-sorted — so
// the goldens/SHA of the SIM are unchanged; only the engine file's own SHA shifts.
function buildGrid(S){
  const GW=Math.ceil(W/CELL), GH=Math.ceil(H/CELL);
  let cells=S._cells;
  if(!cells||S._gw!==GW||S._gh!==GH){ cells=new Array(GW*GH); S._cells=cells; }
  for(let i=0;i<cells.length;i++)cells[i]=null;
  let n=0;
  for(const a of S.agents){ if(a.dead)continue; n++;
    const cx=Math.min(GW-1,Math.max(0,Math.floor(a.x/CELL))), cy=Math.min(GH-1,Math.max(0,Math.floor(a.y/CELL)));
    const key=cy*GW+cx; let arr=cells[key]; if(!arr){arr=[];cells[key]=arr;} arr.push(a); }
  S._grid=cells; S._gw=GW; S._gh=GH; S._aliveN=n;
}
function nearby(S,a,r){
  const g=S._grid; if(!g){const out=[];for(const o of S.agents)if(o!==a&&!o.dead&&dist(o,a)<r)out.push(o);out.sort((x,y)=>x.id-y.id);return out;}
  const GW=S._gw, GH=S._gh, ring=Math.ceil(r/CELL), cx=Math.floor(a.x/CELL), cy=Math.floor(a.y/CELL), out=[];
  for(let dy=-ring;dy<=ring;dy++)for(let dx=-ring;dx<=ring;dx++){
    const nx=cx+dx, ny=cy+dy; if(nx<0||ny<0||nx>=GW||ny>=GH)continue;
    const arr=g[ny*GW+nx]; if(!arr)continue;
    for(const o of arr){ if(o===a||o.dead)continue; if(dist(o,a)<r)out.push(o); } }
  out.sort((x,y)=>x.id-y.id); return out;
}
function nearestAgent(S,a,pred,maxR){ let b=null,bd=1e9;
  for(const o of nearby(S,a,maxR||30)){ if(pred&&!pred(o))continue; const d=dist(o,a); if(d<bd){bd=d;b=o;} } return b; }
function nearWarmth(S,a){return S.fires.some(f=>dist(f,a)<2.5)||S.huts.some(h=>dist(h,a)<2);}
function findNearest(S,ag,type){
  let best=null,bd=1e9;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){
    const t=S.tiles[y][x];
    if(t.t===type&&(type==='water'||t.n>0)){const d=Math.hypot(x-ag.x,y-ag.y);if(d<bd){bd=d;best={x,y};}} // ENGINE 2.0.1 (THE WATER FIX, D-081): water tiles carry n=0 — the n>0 guard made water unfindable, so fishing could neither be DISCOVERED (fishGather obs unreachable) nor PRACTICED (fishers found no water). Found by the EP's field report ("aldrig såg någon använda vattnet"), confirmed 0/20 worlds x 120y in both 1.2.1 and 2.0.0.
  }
  return best;
}
function moveToward(S,a,t){
  const d=Math.hypot(t.x-a.x,t.y-a.y)||1;
  const nx=a.x+(t.x-a.x)/d*.45,ny=a.y+(t.y-a.y)/d*.45;
  if(S.tiles[Math.round(clamp(ny,0,H-1))][Math.round(clamp(nx,0,W-1))].t!=='water'){a.x=nx;a.y=ny;}
  else wander(S,a);
  a.x=clamp(a.x,0,W-1);a.y=clamp(a.y,0,H-1);
  // R2 INK1 (MOTOR-LANE-ORDER-R2-FAS4 §pathUse): cumulative footfall per tile for every DIRECTED
  // step (all 13 call sites are human souls; animals move inline in animalsTick and are excluded —
  // desire lines are human). Read-side tally: consumes no S.rand, never feeds back into behavior.
  // Lazily allocated (resimulation rebuilds it identically); row-major y*W+x like tileN.
  if(!S.pathUse)S.pathUse=new Array(W*H).fill(0);
  S.pathUse[Math.round(a.y)*W+Math.round(a.x)]++;
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
function regrowLater(S,x,y,type){S.tiles[y][x].n=0;const mult=(type==='berry'&&seasonOf(S)==='winter')?2.5:((type==='clay'||type==='sand')?4:1);S.regrows.push({x,y,type,at:S.tick+Math.floor(RI(S,150,350)*mult)});}

function talk(S,a,b){
  S.stats.talks++;
  a.social=100;b.social=100;a.talkCd=25;b.talkCd=25;
  a.rel[b.id]=(a.rel[b.id]||20)+10;b.rel[a.id]=(b.rel[a.id]||20)+10;
  a.task='talking';b.task='talking';
  // shared ways bind: communities with common rituals hold together
  let shared=0;
  for(const id of a.customs)if(b.customs.has(id)){const sc=S.customs[id];if(sc&&sc.status==='alive')shared++;}
  let gaveGift=false;
  if(shared){
    const bond=Math.min(6,shared*2);
    a.rel[b.id]+=bond;b.rel[a.id]+=bond;
    // and they do not let each other starve — faith's evolutionary value.
    // E1.5 (D-166 B1 §5, the §10.2 correction): this EXISTING sharing IS the first Polanyi step —
    // the gift economy. It is kept exactly as it was, counted per village, and when it has
    // recurred enough it receives a NAME (a named way, an event) — NO prices, NO market, NO
    // currency: the gift itself binds. The Almanac/chronicle can now narrate it.
    let giver=null,taker=null;
    if(b.hunger<30&&a.hunger>65){giver=a;taker=b;}
    else if(a.hunger<30&&b.hunger>65){giver=b;taker=a;}
    if(giver){
      giver.hunger-=15;taker.hunger=clamp(taker.hunger+25,0,140);
      S.stats.gifts=(S.stats.gifts||0)+1;tryObserve(S,giver,'weightsBalance',.07);tryObserve(S,taker,'weightsBalance',.05);
      const gv=villageOf(S,giver);
      if(gv){
        gv.shareN=(gv.shareN||0)+1;
        if(!gv.giftName&&gv.shareN>=6){
          gv.giftName=pick(S,['The Open Hand','The Shared Bowl','The Hearth-Gift','The Full Bowl Custom']);
          ev(S,'giftway',`🍞 In <b>${gv.name}</b>, the giving has become a way with a name: <b>${gv.giftName}</b> — those who keep the same ways do not let each other starve. No price is asked; the gift itself binds.`,{village:gv.name,x:gv.x,y:gv.y,label:'A GIFT-WAY IS NAMED',causes:['agent:'+giver.id]});
        }
      }
      if(S.rand()<.15)ev(S,'sharing',`🍞 <b>${giver.name}</b> shared food with <b>${taker.name}</b>${gv&&gv.giftName?' — '+gv.giftName+' holds':''}. Those who keep the same ways do not let each other starve.`,{x:a.x,y:a.y,agent:giver.id,causes:['agent:'+giver.id,'agent:'+taker.id]});
      speak(S,giver,pickSay(S,giver,'gift'),'gift');
      if(giver===a)gaveGift=true; // the giver's words stand — the end-of-talk speech must not overwrite them
    }
  }
  let taught=false;
  // ENGINE 2.1 (D-086): knowledge spreads readily WITHIN a community but rarely crosses to a stranger's
  // village — a passing wanderer trades a few words, not a craft (Rogers: diffusion needs sustained
  // contact + homophily; Henrich: connectivity bounds spread). This is what lets distant villages
  // develop, lose and rediscover INDEPENDENTLY — the source of lasting differentiation. Combined with
  // transmission fidelity falling by complexity (a deep craft is apprenticeship, not a sentence).
  const sameVillage = (villageOf(S,a)===villageOf(S,b));
  const reach = sameVillage ? 1 : 0.06;
  for(const k of a.knows){
    if(!b.knows.has(k)&&S.rand()<(0.5/techDepth(k))*reach){
      gainKnowledge(S,b,k,'taught');taught=true;
      if(S.rand()<.3)ev(S,'taught',`<b>${a.name}</b> taught <b>${b.name}</b> the secret of ${S.knowledge[k]?S.knowledge[k].name:TECH[k].base}. The knowledge spreads.`,{tech:k});
      break;
    }
  }
  spreadCustoms(S,a,b);
  if(gaveGift){} // E1.5: the gift line already spoken
  else if(taught)speak(S,a,pickSay(S,a,'teach'),'teach');
  else if((a.rel[b.id]||0)>70)speak(S,a,pickSay(S,a,'love'),'love');
  else speak(S,a,pickSay(S,a,'small'),'small');
  if(a.rel[b.id]>45&&b.rel[a.id]>45&&a.age>16&&b.age>16&&a.age<52&&b.age<52
     &&a.childCd===0&&b.childCd===0&&a.hunger>40&&b.hunger>40&&(S._aliveN||S.agents.filter(x=>!x.dead).length)<carryingCapacity(S) // E1 MALTHUS: the land sets the ceiling (cached alive count)
     &&S.rand()<(worldKnows(S,'farming')||worldKnows(S,'fishing')?0.26:0.20)){ // TENSION PROTO: births eased for a more populous world (EP request) — rel bar 60→45, fertile window +2y, rate up
    const child=makeAgent(S,a.x,a.y,[a,b]);child.born=S.tick/YEAR;
    // ENGINE 2.1 (D-086): writing's durability is now COMMUNITY-local, not global — a child inherits
    // its parent's knowledge only where the parent's village is literate. Illiterate communities must
    // re-teach every generation, so a craft can die with its last carrier. This is the differentiation
    // engine and it makes writing the pivotal discovery (the Memory Engine, D-072's "writing fix").
    if(groupKnows(S,a,'writing')){for(const k of a.knows)if(S.rand()<.5)child.knows.add(k);}
    S.agents.push(child);S.stats.births++;
    S.maxPop=Math.max(S.maxPop,S.agents.filter(x=>!x.dead).length);
    {const cd=(worldKnows(S,'farming')||worldKnows(S,'fishing'))?380:520;a.childCd=cd;b.childCd=cd;} // TENSION PROTO: shorter birth spacing for a more populous world (EP request)
    ev(S,'child',`👶 <b>${a.name}</b> and <b>${b.name}</b> have had a child, <b>${child.name}</b> — generation ${child.gen}. They inherit traits from both.`,{agent:child.id,x:a.x,y:a.y,causes:['agent:'+a.id,'agent:'+b.id]}); // R2 INK1 causes: a birth is caused by its parents
    maybeEmergeCustom(S,a,'child');
  }
}

function tryBuildHut(S,a){
  if((a.inv.wood||0)<8)return;
  a.inv.wood-=8;
  const h={x:a.x,y:a.y,owner:a.name};S.huts.push(h);a.home=h;S.bgDirty=true;
  ev(S,'hut',`🛖 <b>${a.name}</b> built a hut.`,{x:a.x,y:a.y});
  const cluster=S.huts.filter(o=>dist(o,h)<7);
  if(cluster.length>=3&&!S.villages.some(v=>dist(v,h)<12)){ // E1: village spacing widened with the map
    giveEpithet(S,a,'the Founder');
    const vname=a.name.split(' ')[0]+pick(S,['stead','vik','heim','holm','haven']);
    S.villages.push({x:h.x,y:h.y,name:vname});
    // R2 INK1 causes: a founding is caused by its founder (v1 depth-1; hut owners are stored by
    // NAME, not id, and origin/exodus tracking does not exist — the cheap honest reference is the
    // soul whose hut completed the cluster). Full exodus lineage = a later wave.
    ev(S,'village',`🏘️ Three huts have become more — <b>${vname}</b> has been founded! A village born of nothing but cooperation.`,{village:vname,x:h.x,y:h.y,causes:['agent:'+a.id]});
  }
}

function killAgent(S,a,causeKey,causeTxt,extraCauses){
  for(const kid of a.knows){const k=S.knowledge[kid];if(k)k.lastKnownBy=disp(a);}
  S.stats.deaths[causeKey]=(S.stats.deaths[causeKey]||0)+1;
  // R2 INK1 causes: a death is caused by its cause key (starvation/cold/age/wolves/violence).
  // E1.5: a VIOLENT death may additionally chain the act that dealt it + the hand that held the
  // blade (extraCauses, R2 grammar) — and the death event is returned so grief can reference it.
  const de=ev(S,'death',`<b>${disp(a)}</b> ${causeTxt}.${a.knows.size>3?' Much knowledge died with them — unless someone learned in time.':''}`,{agent:a.id,x:a.x,y:a.y,cause:causeKey,causes:['cause:'+causeKey].concat(extraCauses||[])});
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
  return de;
}
// ===== STAR OBSERVATION -> COSMOLOGY -> RELIGION (EP directive, TD-030 prototype ported into 2.2, D-088) =====
// A curious soul awake in the dark, away from the fire's glow, sees the lights that do not fall. Watching
// accrues; writing turns it into star-marks (the 'starsWheel' insight, so the sky-watcher unlocks the
// CALENDAR); enough watching gives the sky MEANING -> a cosmology belief that spreads + hardens through
// the culture engine into a sky-faith (and, with masonry, a TEMPLE). RNG-clean (S.rand is sim logic).
const SKY_NAMES=['the Watchers','the First Fires','the Ancestor-Lights','the Cold Lanterns','the Sky-Wheel','the Star-Herd','the Ever-Watching','the Night-Weavers'];
const SKY_TXTS=['believing the lights above are the fires of the first people, still watching',
 'believing the stars are the eyes of the ancestors, so the dead are never wholly gone',
 'believing the sky-wheel turns with the seasons and can be read like tracks in snow',
 'believing a great herd walks the night sky, and the living must follow its path'];
function skyHash(a){let h=2166136261>>>0;const s=(a.name||'')+'|'+a.id;for(let i=0;i<s.length;i++){h^=s.charCodeAt(i);h=Math.imul(h,16777619)>>>0;}return h>>>0;}
function worldHasTarget(S,t){for(const id in S.customs){const c=S.customs[id];if(c&&c.target===t&&c.status!=='gone')return true;}return false;}
function customByTarget(S,t){for(const id in S.customs){const c=S.customs[id];if(c&&c.target===t&&c.status==='alive')return c;}return null;}
// Engine 2.3 (D-089): per-tick cache of the cosmos-target check — starTick calls it for every gazing
// soul every night, and it was O(customs) each time (customs grow over time = a hidden creeping cost).
function hasCosmosCached(S){ if(S._cosmoTick!==S.tick){S._cosmo=worldHasTarget(S,'cosmos');S._cosmoTick=S.tick;} return S._cosmo; }
function starTick(S,a){
  const night=S.hour>=20||S.hour<4;
  if(!night||a.sleeping||a.age<12)return;
  // leisure to look up: a content person (warm, not starving) gazes — a campfire on a clear
  // night is the image, not someone freezing away from the fire. Surplus enables culture.
  if(a.warmth<40||a.hunger<35)return;
  if(S.season==='winter'&&S.winterSeverity>1.6)return;
  const curio=(a.traits&&a.traits.curiosity)||0.5;
  if(S.rand()<0.13*curio){
    a.starGaze=(a.starGaze||0)+1;
    if(a.starGaze===1)ev(S,'star','✨ <b>'+disp(a)+'</b> lay awake in the dark and watched the lights that do not fall.',{agent:a.id,x:a.x,y:a.y});
    else if(a.starGaze===4&&S.rand()<.6)ev(S,'star','✨ <b>'+disp(a)+'</b> watches the stars again, night after night. Something is taking shape.',{agent:a.id});
    if(S.rand()<.25)speak(S,a,'Have you ever counted the stars?','observe');
    if(a.starGaze>=4)tryObserve(S,a,'starsWheel',0.4);
    if(a.starGaze>=5&&a.knows.has('writing')&&(a.inv.clay||0)>0&&!a.starMarks){
      a.starMarks=true;
      ev(S,'star','📜✨ <b>'+disp(a)+'</b> pressed the pattern of the stars into clay — the first star-marks. Now the sky can be remembered.',{agent:a.id});
    }
    if(a.starGaze>=6&&!hasCustomKind(S,a,'belief','cosmos')&&!hasCosmosCached(S)){
      const h=skyHash(a);
      const cu=addCustom(S,a,{kind:'belief',lens:'sky',target:'cosmos',slot:'belief:cosmos',name:SKY_NAMES[h%SKY_NAMES.length],txt:SKY_TXTS[(h>>>3)%SKY_TXTS.length],word:'star'});
      if(cu){cu.religion=true;ev(S,'star','🌌 <b>'+disp(a)+'</b> saw a pattern in the stars and gave it a name — <b>'+cu.name+'</b>. From watching, a cosmology is born: the first faith of the sky.',{agent:a.id,label:'A RELIGION OF THE SKY'});}
    }
  }
  if(hasCustomKind(S,a,'belief','cosmos')&&S.hour===22&&S.rand()<.02){
    a.task='watching the sky with the others';
    const nm=(customByTarget(S,'cosmos')||{}).name||'the Watchers';
    if(S.rand()<.12)ev(S,'star','🌌 Under '+nm+', the people gather in the dark and turn their faces to the stars.',{agent:a.id,x:a.x,y:a.y});
  }
}

// ============================================================================
// TENSION / FRICTION PROTOTYPE (design spike, 2026-07-20) — NOT canon.
// The EP's four-force model: Resurser × Individer × FRIKTION × Handling.
// An individual acts to reduce the GAP between how the world IS and how they
// WISH it were — extended from survival (hunger/cold) to STRIVING (wealth/status).
// FRICTION rises when goals collide or resources run short (population pressure);
// where a legitimate path to close the gap is unavailable or costlier than force,
// coercion becomes an OPTION: theft, brawl, revenge, feud. Violence is a solution
// to a gap, never a scripted event. Trade and violence are two answers to the same
// friction — which one depends on personality + scarcity + restraint. Deterministic.
// Personality is read as PROXIES from the canon traits (a real build would add
// dedicated aggression/impulse/vindictiveness traits; this spike derives them):
//   aggression  ~ (1-empathy)*.6 + ambition*.4     greed ~ ambition
//   impulse-ctrl ~ conformity                       vindictiveness ~ ambition + grudge
//   fear/avoid   ~ empathy                          honor/prestige ~ ambition
// ============================================================================
// TUNING (EP 2026-07-20: "våld ska dyka upp ibland och väldigt sällan wipa hela civilisationer").
// One place to dial the whole thing up or down. Lower = rarer + less lethal.
const TUNE={
  theftRate:      0.025, // per-tick chance multiplier that a ripe DESPERATION becomes a theft (was 0.16; E1.5 gate-tuned)
  // E1.5 BALANCE GATE (order §3, measured over canon seeds y0-y120): theft must stay occasional-per-
  // decade and the feud a generational event. The proto's single rate let GREED fire as often as
  // hunger — measured 277 raids/120y on seed 4242. Each rung now carries its own rarity:
  raidRate:       0.006, // greed is patient — predation on the richer is RARE, not a lifestyle
  feudRate:       0.025, // honour moves when it moves — between theft and raid in rarity (E1.5b: probed at 0.020/0.015 — no reliable leverage on single-seed counts, fork variance dominates, and lower settings starve the other seeds of feuds entirely; kept at the E1.5 calibration, band deviation on 4242 reported honestly)
  brawlLethalBase:0.010, // chance a resisted theft/raid turns deadly, fists (was 0.03)
  brawlLethalArm: 0.045, // + this × best weapon (was 0.13) — steel still bites, just far less often
  warChance:      0.22,  // chance a ripe village tension actually breaks into a raid that year (was 0.50)
  warHostility:   0.62,  // how ripe it must be first (was 0.50)
  warParty:       3,     // raiders per war (was 4)
  warLethalBase:  0.05,  // per-clash death chance in a raid, fists (was 0.15)
  warLethalArm:   0.08,  // + this × best weapon (was 0.20)
  warMaxDead:     2,     // a single raid can cost at most this many lives total — no wipeouts
  // E1.5 (D-166 B1): leadership + tribute knobs (RESEARCH-FRONTIER 2.1 — smallest honest rule)
  // E1.5b (V1, review I2): the ratio margin (leaderMargin 1.04) is RETIRED — a percentage of the
  // runner-up shrinks monotonically as villages grow, which inverted the rule against its own
  // anthropology. The lead is now ABSOLUTE (score units), read against the unclamped standing.
  leaderMin:      2.30,  // accumulated standing a soul must clear to be RECOGNIZED at all (calibrated E1.5b)
  leaderLead:     0.55,  // ...and how far above the second-best they must stand, in ABSOLUTE score units at the leaderVillage floor; eased by sqrt(leaderVillage/adults) as the group grows (calibrated E1.5b)
  leaderVillage:  5,     // adults a village needs before "who decides" is even a question
  tributeMin:     8,     // wealth below which no one pays tribute — no one starves for a leader
  // E1.5b (V6, review I1): the hoard milestone — the one-per-life event that makes surplus VISIBLE
  // in the log, so greed's raid can cite the pile it preys on (calibrated E1.5b).
  hoardMark:      12
};
function wealth(a){let w=0;for(const k in a.inv)w+=a.inv[k]||0;return w;} // E1.5: exported as wealthOf — the Almanac's wealth sort feeds on this
// means of force: a weapon or metal makes coercion viable AND lethal (ties violence to the tech tree).
function forceMeans(a){
  if(a.knows.has('steel'))return 1.0;
  if(a.knows.has('bronze'))return 0.8;
  if(a.knows.has('spear')||a.knows.has('bow'))return 0.55;
  if(a.knows.has('sharp'))return 0.3;
  return 0.12; // fists and stones
}
// E1.5 (D-166 B1): the proxies are RETIRED — aggression is now a first-class inherited trait.
function aggression(a){return a.traits.aggression!==undefined?a.traits.aggression:clamp((1-a.traits.empathy)*0.6+a.traits.ambition*0.4,0,1);}
// social restraint: impulse CONTROL (dedicated trait) + empathy (fear of harming) + any emerged law/peace-norm.
function restraintOf(S,a){
  let r=(a.traits.impulse!==undefined?a.traits.impulse:a.traits.conformity)*0.6+a.traits.empathy*0.4;
  if(hasCustomKind(S,a,'taboo','harm')||hasCustomKind(S,a,'value','peace'))r+=0.4; // an emerged law bites
  return clamp(r,0,1.3);
}
// FRICTION from scarcity: population pressure against the land's ceiling (cached per tick).
// This is where the D-089 population pressure becomes a DRIVER, not just a perf cost.
function pressure(S){
  if(S._pscTick===S.tick)return S._psc;
  const alive=S._aliveN||S.agents.filter(x=>!x.dead).length, cap=Math.max(1,carryingCapacity(S));
  S._psc=clamp(alive/cap,0,2); S._pscTick=S.tick; return S._psc;
}
// ASPIRATION: an ambitious, satisfied soul hoards a surplus beyond need — the seed of wealth + inequality.
function aspireTick(S,a){
  if(a.age<14||a.traits.ambition<0.5)return;
  if(a.hunger>60&&a.warmth>50&&a.energy>35&&S.rand()<0.05*a.traits.ambition){
    const kinds=['wood','clay','stone','fiber'];const m=kinds[Math.floor(S.rand()*kinds.length)];
    a.inv[m]=(a.inv[m]||0)+1; a.hoard=(a.hoard||0)+1; a.task='adding to their store'; tryObserve(S,a,'weightsBalance',.05);
    // E1.5b (V6, review I1): the hoard becomes VISIBLE once per life — a rare milestone event
    // (no rand consumed: fires exactly when the count crosses TUNE.hoardMark) that greed's
    // raid can later cite as the surplus it preys on. The pile enters the chronicle of causes.
    if(a.hoard===TUNE.hoardMark&&a.hoardEv===undefined)
      a.hoardEv=ev(S,'hoard',`🏺 <b>${disp(a)}</b> has begun laying up stores beyond any need. The pile grows — and eyes have begun to follow it.`,{agent:a.id,x:a.x,y:a.y,causes:['agent:'+a.id]}).id;
  }
}
// CONFLICT: theft / brawl / raid / revenge — a gap closed by force. Returns true if it consumed the tick.
function conflictTick(S,a){
  if(a.age<12)return false;
  const desp=a.hunger<24?1:(a.hunger<32?0.35:0); // E1.5 gate-tuning: desperation is DESPERATION — the peckish do not rob
  const myW=wealth(a), fric=pressure(S);
  let tgt=null,best=0,revenge=false;
  for(const b of nearby(S,a,4.5)){
    if(b.age<10)continue;
    // E1.5b (V2, review I6): ONE truth-source. The revenge trigger reads the grudge BOOKKEEPING
    // (a.grudges — wrong -> event id), in conjunction with still-hot anger (rel), never rel alone.
    // rel can sink for non-violent reasons (taboo-breaking) with no booked wrong behind it — the
    // old trigger could then fire a feud with no citable cause. Now a feud NEEDS a booked wrong,
    // so its causes[] carries a resolving ev: reference BY CONSTRUCTION.
    const grudge=(a.grudges&&a.grudges[b.id]!==undefined&&(a.rel[b.id]||0)<-55)?1:0;
    const foodGap=desp&&b.hunger>62?1:0;
    const wGap=wealth(b)-myW;
    const score=foodGap*70+(wGap>8?wGap:0)+grudge*85; // E1.5 gate-tuning: greed preys on the visibly RICH (hoarders, leaders), not on anyone slightly better off
    if(score>best){best=score;tgt=b;revenge=grudge&&!foodGap&&wGap<=4;}
  }
  if(!tgt)return false;
  const slack=a.hunger>55&&a.warmth>45&&a.energy>30;
  const greed=slack&&a.traits.ambition>0.55?(a.traits.ambition-0.5):0;
  const restraint=restraintOf(S,a), agg=aggression(a);
  // GROUP IDENTITY: kin (same village) are shielded by the Peace of Kin; a stranger is fair game.
  const kin=villageOf(S,a)===villageOf(S,tgt)?0.22:-0.06; // raiding your own is far harder than raiding "them"
  // drive to use force = pull of the gap, lifted by aggression/means/friction, minus restraint.
  // E1.5: each rung reads its HONEST trait — desperation is hunger's own force, greed rides
  // aggression+means, and revenge is VINDICTIVENESS (the blood-memory trait), not ambition.
  let drive=0;
  if(desp)          drive=0.85*desp+agg*0.2-restraint*0.45-kin;
  else if(revenge)  drive=0.30+a.traits.vindictiveness*0.6-restraint*0.35-kin;   // honor: the grudge burns by disposition
  else if(greed>0)  drive=greed*0.6+agg*0.35+forceMeans(a)*0.2-restraint*0.7-kin; // predation on the richer
  drive*=(0.7+0.6*fric); // FRICTION: scarcity makes every gap sharper (population pressure as DRIVER, D-089->D-166)
  if(drive<=0)return false;
  const rate=desp?TUNE.theftRate:revenge?TUNE.feudRate:TUNE.raidRate;         // E1.5: each rung has its own rarity
  if(S.rand()>clamp(drive,0,1)*rate)return false;                             // occasional, weighted by drive
  // --- THE ACT (E1.5: each rung is its OWN event type, id + causes[] per the R2 grammar).
  // The kind follows the DRIVING branch (the proto let a desperation act on an unfed target
  // masquerade as a greed-raid — dishonest bookkeeping AND it leaked theft's rate into raids). ---
  const kind=desp?(tgt.hunger>62?'steal-food':'steal-goods'):revenge?'revenge':'raid';
  // E1.5b (V6, review I1): theft chains to its DRIVE — the freshest hunger-event the log holds.
  // Winter within half a year (the onset event now exists every winter — see tickWorld), else a
  // starvation death in the thief's own village within a year. Honest: if the log holds no
  // drive-event in near-history, the steal carries labels only — no chain is invented.
  let driveEv;
  if(desp){
    if(S.lastWinterEv&&S.tick-S.lastWinterEv.tick<=YEAR/2)driveEv=S.lastWinterEv.id;
    else if(S.lastStarve&&S.tick-S.lastStarve.tick<=YEAR){const mv=villageOf(S,a);if(mv&&mv.name===S.lastStarve.vil)driveEv=S.lastStarve.id;}
  }
  // E1.5b (V6): greed's ev-link — the raid preys on VISIBLE surplus; when the log booked the
  // target's hoard-milestone (or the leadership whose tribute fed the pile), the raid cites it.
  let greedEv=tgt.hoardEv;
  if(greedEv===undefined){const tv=villageOf(S,tgt);if(tv&&tv.leader===tgt.id&&tv.leaderEv!==undefined)greedEv=tv.leaderEv;}
  const armedMe=forceMeans(a), armedYou=forceMeans(tgt);
  const meStr=armedMe+a.traits.dexterity*0.5, youStr=armedYou+tgt.traits.dexterity*0.5;
  let outcome, actEv;
  if(kind==='steal-food'){
    const took=Math.min(28,Math.max(10,tgt.hunger-30));
    tgt.hunger=clamp(tgt.hunger-took,0,140); a.hunger=clamp(a.hunger+took*0.8,0,140);
    a.task='taking food by force'; outcome='took food';
    S.stats.steals=(S.stats.steals||0)+1;
    actEv=ev(S,'steal',`🥀 Hunger owned the hand: <b>${disp(a)}</b> wrenched food from <b>${disp(tgt)}</b>.`,{agent:a.id,victim:tgt.id,x:a.x,y:a.y,cause:'desperation',causes:(driveEv!==undefined?['ev:'+driveEv]:[]).concat(['agent:'+tgt.id,'cause:desperation'],S.season==='winter'?['cause:winter']:[])});
    speak(S,a,pickSay(S,a,'steal'),'steal');
  } else {
    const kinds=Object.keys(tgt.inv).filter(k=>tgt.inv[k]>0);
    if(kinds.length){const m=kinds[Math.floor(S.rand()*kinds.length)];const q=Math.min(tgt.inv[m],1+Math.floor(S.rand()*3));tgt.inv[m]-=q;a.inv[m]=(a.inv[m]||0)+q;outcome='seized goods';}
    else outcome='found little';
    if(kind==='steal-goods'){
      a.task='taking food by force';
      S.stats.steals=(S.stats.steals||0)+1;
      actEv=ev(S,'steal',`🥀 Need drove the hand: <b>${disp(a)}</b> ${outcome==='seized goods'?'took what could be carried from':'went through the store of'} <b>${disp(tgt)}</b>.`,{agent:a.id,victim:tgt.id,x:a.x,y:a.y,cause:'desperation',causes:(driveEv!==undefined?['ev:'+driveEv]:[]).concat(['agent:'+tgt.id,'cause:desperation'],S.season==='winter'?['cause:winter']:[])});
      speak(S,a,pickSay(S,a,'steal'),'steal');
    }else if(kind==='revenge'){
      a.task='settling a score';
      S.stats.feuds=(S.stats.feuds||0)+1; S.stats.revenges=(S.stats.revenges||0)+1;
      // the feud CHAINS: causes[] carries the remembered wrong (the very event that lit the grudge).
      // E1.5b (V2): src is defined BY CONSTRUCTION — the trigger above only fires on a booked
      // grudge, so every feud resolves to its wrong. The guard below is kept as belt-and-braces.
      const src=a.grudges?a.grudges[tgt.id]:undefined;
      actEv=ev(S,'feud',`🩸 <b>${disp(a)}</b> came for <b>${disp(tgt)}</b> — an old wrong, not forgotten, ${outcome==='seized goods'?'paid back in goods and bruises':'answered at last'}.`,{agent:a.id,victim:tgt.id,x:a.x,y:a.y,cause:'honor',causes:(src!==undefined?['ev:'+src]:[]).concat(['agent:'+tgt.id,'cause:grudge'])});
      speak(S,a,pickSay(S,a,'feud'),'feud');
    }else{
      a.task='raiding a neighbour';
      S.stats.raids=(S.stats.raids||0)+1;
      actEv=ev(S,'raid',`⚔️ <b>${disp(a)}</b> set upon <b>${disp(tgt)}</b> and ${outcome} — the gap was cheaper to close by force.`,{agent:a.id,victim:tgt.id,x:a.x,y:a.y,cause:'greed',causes:(greedEv!==undefined?['ev:'+greedEv]:[]).concat(['agent:'+tgt.id,'cause:greed'])});
      speak(S,a,pickSay(S,a,'raid'),'raid');
    }
  }
  // the victim remembers — and HOW MUCH depends on the wrong: hunger is half-forgiven (a stolen
  // meal stays below the feud threshold), GREED is not (a raid burns deep enough to demand answer).
  tgt.rel[a.id]=(tgt.rel[a.id]||0)-(kind==='raid'?60:45);
  if(tgt.grudges)tgt.grudges[a.id]=actEv.id;                                // ...and remembers WHICH wrong (the feud's future evidence)
  if(kind==='revenge'){a.rel[tgt.id]=-30;if(a.grudges)delete a.grudges[tgt.id];} // E1.5: blood ANSWERED settles the score (no endless ping-pong — the counter-grudge may still chain it, generationally)
  else a.rel[tgt.id]=(a.rel[tgt.id]||0)-10;
  S.stats.thefts=(S.stats.thefts||0)+1; // legacy total across all three rungs
  // resistance -> a real fight; the MEANS decides lethality (a stone-age scuffle rarely kills; steel does).
  const resists=(kind!=='steal-food')||tgt.traits.ambition>0.5||S.rand()<0.5;
  if(resists){
    S.stats.brawls=(S.stats.brawls||0)+1;
    a.energy=clamp(a.energy-12,0,100); tgt.energy=clamp(tgt.energy-12,0,100);
    const lethal=TUNE.brawlLethalBase+TUNE.brawlLethalArm*Math.max(armedMe,armedYou);
    if(S.rand()<lethal){
      const loser=(meStr+S.rand()*0.4)<(youStr+S.rand()*0.4)?a:tgt, killer=loser===a?tgt:a;
      const healer=S.agents.some(h=>!h.dead&&h!==loser&&dist(h,loser)<5&&h.knows.has('medicine'));
      if(healer&&S.rand()<0.5){
        loser.energy=clamp(loser.energy-25,0,100);
        ev(S,'violence',`🩹 <b>${disp(killer)}</b> left <b>${disp(loser)}</b> bleeding — a healer's hands held death off.`,{agent:loser.id,x:loser.x,y:loser.y,causes:['ev:'+actEv.id]});
      }else{
        const deathEv=killAgent(S,loser,'violence',`was killed by <b>${disp(killer)}</b> in a fight`,['ev:'+actEv.id,'agent:'+killer.id]);
        killer.kills=(killer.kills||0)+1;
        // the killing seeds a FEUD: everyone who loved the fallen now hates the killer (blodshämnd),
        // and each mourner BOOKKEEPS the killing event — the future revenge will cite it (E1.5).
        let mourner=null;
        for(const w of S.agents){if(w.dead||w===killer)continue;if(dist(w,loser)<7&&(w.rel[loser.id]||0)>30){w.rel[killer.id]=(w.rel[killer.id]||0)-75;if(w.grudges)w.grudges[killer.id]=deathEv.id;if(!mourner)mourner=w;}}
        if(mourner){
          ev(S,'mourn',`🕯️ <b>${disp(mourner)}</b> mourns <b>${disp(loser)}</b> — and does not forget whose hand it was.`,{agent:mourner.id,victim:loser.id,x:mourner.x,y:mourner.y,causes:['ev:'+deathEv.id]});
          speak(S,mourner,pickSay(S,mourner,'mourn'),'mourn');
        }
        S.stats.killings=(S.stats.killings||0)+1;
        // INSTITUTION AS RESPONSE: recurring blood in one place breeds a norm against harm (proto-law).
        maybeEmergeCustom(S,killer,'death','violence');
        if((S.stats.killings||0)>=3&&S.rand()<0.5)seedHarmTaboo(S,killer,deathEv.id);
      }
    }
  }
  return true;
}
// justice emerges as an answer to recurring violence: a village-borne taboo against harming your own.
function seedHarmTaboo(S,a,evId){
  if(hasCustomKind(S,a,'taboo','harm'))return;
  const c=addCustom(S,a,{kind:'taboo',lens:'law',target:'harm',slot:'taboo:harm',name:'The Peace of Kin',txt:'that spilling the blood of your own is forbidden',word:'peace'});
  if(c)ev(S,'violence',`⚖️ After too much blood, the people of <b>${disp(a)}</b>'s village bind themselves to a rule: <b>The Peace of Kin</b> — no more killing your own.`,{agent:a.id,x:a.x,y:a.y,label:'A LAW IS BORN',causes:evId!==undefined?['ev:'+evId]:[]});
}

// ===== E1.5 (D-166 B1 §4, RESEARCH-FRONTIER 2.1): PRESTIGE -> RECOGNIZED LEADER =====
// Prestige-weighted imitation existed since the culture engine; what was missing is prestige
// ACCUMULATING into recognized authority over others. Smallest honest rule: in a village of
// enough adults, a soul whose standing (prestige + ambition) clears a threshold AND clearly
// outshines every rival is RECOGNIZED — a sim fact (village.leader), never an assignment.
// Redistribution UPWARD follows: households with surplus lay a share at the leader's door
// (tribute — the gift-obligation face of hierarchy; Flannery & Marcus). Runs yearly.
// E1.5b (V1, review I2): REBUILT. prestige() clamps at 1.2 and every adult saturates — the old
// score reduced to argmax(ambition), and the ratio margin made recognition monotonically HARDER
// as villages grew (inverted vs Flannery & Marcus, where authority institutionalizes as groups
// GROW). Standing is now UNCLAMPED and accumulates over a life: what you know, the ways you
// carry, the years you have seen, and the visible surplus at your door (wealth = hoard + trade +
// tribute, heavy-tailed — in a larger village the top pile is larger in ABSOLUTE terms, so the
// absolute lead below is EASIER to clear, not harder). prestige() itself (imitation weights in
// the culture engine) is untouched — this is the leader rule's own reading of standing.
function leaderScore(a){return (a.knows.size+a.customs.size)*0.08+a.age*0.004+wealth(a)*0.010+a.traits.ambition*0.5;}
function leaderTick(S){
  for(const v of S.villages){
    const adults=[];
    for(const a of S.agents){if(!a.dead&&a.age>=16&&villageOf(S,a)===v)adults.push(a);}
    // a dead (or departed) leader is laid down first — the village remembers, the chronicle tells
    if(v.leader!=null&&!adults.some(a=>a.id===v.leader)){
      ev(S,'leader',`🕯️ <b>${v.name}</b> is without a leader — <b>${v.leaderName}</b>'s voice is gone, and no one yet speaks for all.`,{village:v.name,x:v.x,y:v.y,causes:v.leaderEv!==undefined?['ev:'+v.leaderEv]:[]});
      v.leader=null;v.leaderName=null;v.leaderEv=undefined;
    }
    if(v.leader!=null||adults.length<TUNE.leaderVillage)continue;
    let top=null,second=null;
    for(const a of adults){const s=leaderScore(a);
      if(!top||s>top.s){second=top;top={a,s};}
      else if(!second||s>second.s)second={a,s};}
    if(!top||top.s<TUNE.leaderMin)continue;
    // E1.5b (V1): recognition needs a CLEAR lead in ABSOLUTE terms — a visible distance, not a
    // percentage of the runner-up (which shrank with village size). The required distance eases
    // as the group grows (sqrt law): scalar stress — the more people must coordinate, the sooner
    // a standing lead is accepted as authority (Johnson & Earle; Flannery & Marcus).
    if(second&&top.s-second.s<TUNE.leaderLead*Math.sqrt(TUNE.leaderVillage/adults.length))continue;
    v.leader=top.a.id;v.leaderName=top.a.name;
    const le=ev(S,'leader',`👑 In <b>${v.name}</b>, the people have begun to listen when <b>${disp(top.a)}</b> speaks — prestige has hardened into leadership. No one voted; it is simply so.`,{village:v.name,agent:top.a.id,x:v.x,y:v.y,label:'A LEADER IS RECOGNIZED',causes:['agent:'+top.a.id]});
    v.leaderEv=le.id;
    giveEpithet(S,top.a,'the Voice of '+v.name);
    let n=0;for(const a of adults){if(a!==top.a&&n<2){speak(S,a,pickSay(S,a,'submit'),'submit');n++;}} // the first followers speak their submission (id order — deterministic)
    S.stats.leaders=(S.stats.leaders||0)+1;
  }
  // TRIBUTE: redistribution upward as a sim fact — surplus flows toward standing.
  for(const v of S.villages){
    if(v.leader==null)continue;
    const L=S.agents.find(a=>!a.dead&&a.id===v.leader); if(!L)continue;
    let given=0;
    for(const a of S.agents){
      if(a.dead||a===L||a.age<16||villageOf(S,a)!==v)continue;
      if(wealth(a)<TUNE.tributeMin)continue;
      let bk=null,bq=0;for(const m in a.inv){if(a.inv[m]>bq){bq=a.inv[m];bk=m;}}
      if(bk&&bq>2){a.inv[bk]--;L.inv[bk]=(L.inv[bk]||0)+1;given++;}
    }
    if(given&&S.rand()<0.12)ev(S,'tribute',`🧺 In <b>${v.name}</b>, ${given} household${given>1?'s':''} laid a share at <b>${disp(L)}</b>'s door. So surplus flows upward, and standing becomes wealth.`,{village:v.name,agent:L.id,x:v.x,y:v.y,causes:['agent:'+L.id].concat(v.leaderEv!==undefined?['ev:'+v.leaderEv]:[])});
  }
}

// TRADE — the peaceful twin of the raid: the SAME friction (I lack X, you hold it) resolved by
// EXCHANGE instead of force. Cooperation wins when personality + ties + low scarcity favour it.
// Positive-sum: both leave better, a bond forms (rel up), goods cross village lines. The fork
// "cooperate vs fight" is now real — both are answers to the same gap.
function tryTrade(S,a){
  if(a.age<14)return false;
  const need=neededMaterial(S,a); if(!need||(a.inv[need]||0)>=2)return false;
  let b=null;
  for(const o of nearby(S,a,5)){ if(o.age<12)continue; if((o.inv[need]||0)>=2){b=o;break;} }
  if(!b)return false;
  let give=null;
  for(const m in a.inv){ if(m!==need&&a.inv[m]>=2&&(b.inv[m]||0)<2){give=m;break;} }
  const offerFood=!give&&a.hunger>85&&b.hunger<70;
  if(!give&&!offerFood)return false;
  const cross=villageOf(S,a)!==villageOf(S,b);
  const coop=a.traits.empathy*0.4+a.traits.social*0.35+((a.rel[b.id]||0)>0?0.2:0)-aggression(a)*0.3-pressure(S)*0.2;
  if(coop<=0.15||S.rand()>clamp(coop,0,1)*0.5)return false;
  const q=1+Math.floor(S.rand()*2);
  if(give){const gq=Math.min(a.inv[give],q);a.inv[give]-=gq;b.inv[give]=(b.inv[give]||0)+gq;}
  else{a.hunger=clamp(a.hunger-15,0,140);b.hunger=clamp(b.hunger+15,0,140);}
  const tq=Math.min(b.inv[need],q);b.inv[need]-=tq;a.inv[need]=(a.inv[need]||0)+tq;
  a.rel[b.id]=(a.rel[b.id]||0)+15;b.rel[a.id]=(b.rel[a.id]||0)+15; // a bond forms — trade partners rarely raid each other
  a.task='trading';S.stats.trades=(S.stats.trades||0)+1;
  if(S.rand()<0.05)ev(S,'trade',`🤝 <b>${disp(a)}</b> and <b>${disp(b)}</b> struck a fair trade${cross?' across village lines':''}.`,{agent:a.id,x:a.x,y:a.y});
  return true;
}
// WAR — the top rung. Individual violence flows along identity lines (kin vs stranger); when a
// village is FOOD-STRESSED, a neighbour holds a surplus, and grievance across the line has piled
// up, its armed cross the border as ONE. Organised violence needs what a brawl doesn't: group
// identity (village), a contested need (scarcity vs surplus), and the means to organise. Yearly.
function warTick(S){
  // feuds COOL with time — grudges fade toward forgiveness so vendettas don't spiral forever — AND we
  // FORGET THE DEAD here: rel dicts otherwise fill with dead ids forever (unbounded memory = a hidden
  // per-year cost that grew even at constant population). Pruning keeps them ~O(living), bounding time.
  const aliveIds=new Set(); for(const a of S.agents)if(!a.dead)aliveIds.add(a.id);
  for(const a of S.agents){ if(a.dead)continue;
    const kk=Object.keys(a.rel);                              // snapshot keys — deleting during for-in
    for(let i=0;i<kk.length;i++){ const k=kk[i];              // is engine-defined; this stays Node≡Jint
      if(!aliveIds.has(+k)){ delete a.rel[k]; continue; }     // the dead are forgotten
      if(a.rel[k]<0)a.rel[k]=Math.min(0,a.rel[k]+4);          // and feuds cool toward peace
    }
    // E1.5: grudge bookkeeping follows the same law — the dead are beyond revenge, and a wrong
    // whose anger has cooled (rel back above -20) is FORGIVEN: its event reference is released.
    if(a.grudges){ const gk=Object.keys(a.grudges);
      for(let i=0;i<gk.length;i++){ const k=gk[i]; if(!aliveIds.has(+k)||(a.rel[k]||0)>-20)delete a.grudges[k]; } } }
  const vs=S.villages.filter(v=>!v.dead); if(vs.length<2)return;
  const info=new Map();
  for(const v of vs)info.set(v,{ppl:[],hungry:0,armed:0,wealth:0});
  for(const a of S.agents){ if(a.dead||a.age<14)continue; const v=villageOf(S,a); if(!v||!info.has(v))continue;
    const I=info.get(v); I.ppl.push(a); if(a.hunger<45)I.hungry++; if(forceMeans(a)>=0.5)I.armed++; I.wealth+=wealth(a); }
  const fric=pressure(S); // scarcity: the land contested. This is where war is born.
  for(const A of vs){ const IA=info.get(A); if(!IA||IA.ppl.length<3)continue;
    const stress=Math.max(IA.hungry/IA.ppl.length, fric-0.5); // hungry now, OR the land is over-full
    for(const B of vs){ if(B===A)continue; const IB=info.get(B); if(!IB||IB.ppl.length<1)continue; if(dist(A,B)>55)continue;
      let grud=0; for(const x of IA.ppl)for(const y of IB.ppl)if((x.rel[y.id]||0)<-40)grud++;
      const surplus=IB.wealth-IA.wealth, might=IA.armed-IB.armed;
      // war = a hungry/crowded village + a neighbour worth taking + the means, OR a blood-feud boiling over
      const hostility=stress*0.7+(surplus>6?0.3:0)+(grud>1?0.35:0);
      if(IA.armed<2||might<-2||hostility<TUNE.warHostility)continue;
      if(S.rand()>TUNE.warChance)continue; // ripe tension only sometimes breaks into a raid
      const party=IA.ppl.filter(a=>forceMeans(a)>=0.4).slice(0,TUNE.warParty);
      const defenders=IB.ppl.slice().sort((x,y)=>forceMeans(y)-forceMeans(x));
      let deadA=0,deadB=0,loot=0;
      for(const raider of party){
        const def=defenders.find(d=>!d.dead)||null;
        if(def){ for(const m in def.inv){ if(def.inv[m]>0){ raider.inv[m]=(raider.inv[m]||0)+def.inv[m]; loot+=def.inv[m]; def.inv[m]=0; } }
          const rs=forceMeans(raider)+raider.traits.dexterity*0.5, ds=forceMeans(def)+def.traits.dexterity*0.5;
          const lethal=TUNE.warLethalBase+TUNE.warLethalArm*Math.max(forceMeans(raider),forceMeans(def));
          if(S.rand()<lethal){ const loser=(rs+S.rand()*0.4)<(ds+S.rand()*0.4)?raider:def, enemy=loser===raider?def:raider;
            const wde=killAgent(S,loser,'violence',`fell in the raid on ${B.name}`,['agent:'+enemy.id,'cause:war']); if(loser===raider)deadA++;else deadB++;
            for(const w of S.agents){if(w.dead)continue;if((w.rel[loser.id]||0)>30){w.rel[enemy.id]=(w.rel[enemy.id]||0)-60;if(w.grudges)w.grudges[enemy.id]=wde.id;}} } } // war deepens the feud — and E1.5 books WHICH death each survivor holds against whom
      }
      S.stats.wars=(S.stats.wars||0)+1;
      ev(S,'violence',`🔥⚔️ Driven by a failing harvest, the people of <b>${A.name}</b> fell upon <b>${B.name}</b> — a raid for grain and goods.${(deadA+deadB)>0?' '+(deadA+deadB)+' lay dead ('+deadB+' of '+B.name+', '+deadA+' of '+A.name+').':' They took what they could carry.'}`,{x:A.x,y:A.y,label:'WAR',causes:(grud>1?['cause:grudge']:[]).concat(['cause:scarcity'])});
      return; // one war per year keeps it momentous
    }
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
    else if(nearby(S,a,1.4).length>0)a.warmth+=Math.max(0.2,0.9-(winter?0.4*(S.winterSeverity-1):0));
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
    const de=killAgent(S,a,causeKey,cause);
    // E1.5b (V6, review I1): a starvation death is the village's famine-mark — bookkept so a
    // desperation steal in the same village within a year can cite the hunger that drove it.
    if(causeKey==='starvation'){const v=villageOf(S,a);S.lastStarve={id:de.id,tick:S.tick,vil:v?v.name:null};}
    return;
  }
  const child=a.age<14;

  // TENSION PROTO: the SAME friction resolves as trade OR force. Try the peaceful path first
  // (cooperation-inclined souls trade), then force (conflictTick); aspiration hoards a surplus.
  if(!child){ if(tryTrade(S,a))return; if(conflictTick(S,a))return; aspireTick(S,a); }

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
    doSeek(S,a,'berry',()=>{S.tiles[a.ty][a.tx0].n--;if(S.tiles[a.ty][a.tx0].n<=0)regrowLater(S,a.tx0,a.ty,'berry');a.hunger=clamp(a.hunger+45+(worldKnows(S,'mill')?20:0),0,140);a.task='eating';tryObserve(S,a,'seedsSprout',.09);tryObserve(S,a,'herbsHeal',a.hunger<60?.06:.025);/*D-239 nit, declared rather than silently carried: hunger is read AFTER the +45 the meal just gave, so this is the .025 branch almost always. The hook is reachable either way and that was the point; re-ordering it moves the sim stream, so it waits for the next engine wave instead of buying a re-baseline for a rate tweak.*/{const w2=findNearest(S,a,'water');if(w2&&Math.hypot(w2.x-a.x,w2.y-a.y)<4)tryObserve(S,a,'fishGather',.12);}if(S.rand()<.1)speak(S,a,pickSay(S,a,'hungry'),'hungry');});return;
  }
  if(night&&a.warmth<70){
    const f=nearestOf(S.fires.concat(S.huts),a);
    if(f&&dist(f,a)<25){moveToward(S,a,f);a.task='seeking warmth';if(S.rand()<.05)speak(S,a,pickSay(S,a,'cold'),'cold');return;}
    if(a.knows.has('fire')&&(a.inv.wood||0)>=2){a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='lighting a fire';S.bgDirty=true;return;}
    const buddy=nearestAgent(S,a,null,30);
    if(buddy&&dist(buddy,a)>1.2){moveToward(S,a,buddy);a.task='huddling for warmth';return;}
  }
  if(night&&hasCustomKind(S,a,'value','night')&&nearby(S,a,3).length===0){
    const buddy2=nearestAgent(S,a,null,30);
    if(buddy2){moveToward(S,a,buddy2);a.task='keeping the rule of the shared fire';return;}
  }
  // the fire cult keeps a flame burning even when warm
  if(!night&&S.hour===19&&hasCustomKind(S,a,'value','fire')&&a.knows.has('fire')&&(a.inv.wood||0)>=2&&!S.fires.some(f=>dist(f,a)<6)){
    a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='tending the flame';S.bgDirty=true;return;
  }
  if(a.energy<15)a.sleeping=true;
  if(a.sleeping){a.task='sleeping';a.energy+=4;if(a.energy>=80)a.sleeping=false;else return;}
  starTick(S,a); // ENGINE 2.2 (D-088): stars -> cosmology -> sky-faith; star-gazing unlocks the calendar
  if(S.tick%8===0&&a.hunger>45&&maybeTill(S,a))return; // Engine 1.1: spring tilling when not starving

  // dusk gathering customs: more talk, faster spread (culture drives knowledge)
  if(S.hour===20&&!child){
    const g=hasCustomKind(S,a,'gathering');
    if(g){
      const f=nearestOf(S.fires,a)||nearestAgent(S,a,null,40);
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
          if(S.rand()<.35)speak(S,a,pickSay(S,a,'fail'),'fail');
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
    let fed=0;for(const o of nearby(S,a,8)){if(o.hunger>60&&++fed>=3)break;}
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
      const near=nearestAgent(S,a,null,40);
      if(near){moveToward(S,a,near);a.task='seeking company';return;}
    }
  }

  if(child){
    const p=S.agents.find(o=>!o.dead&&a.parents&&o.name===a.parents[0]);
    if(p&&dist(p,a)>3)moveToward(S,a,p);a.task='growing up';return;
  }

  let need=neededMaterial(S,a)||((a.inv.wood||0)<(a.knows.has('hut')&&!a.home?9:4)?'wood':null);
  // F1.0c (D-360): ATT ARBETA METALL ÄR EN ARBETSHANDLING, INTE EN UPPTÄCKT.
  // Den som kan hantverket och har ingredienserna förbrukar dem och får tinget.
  // Saknas en ingrediens blir DEN det man går och hämtar — i stället för att, som
  // förr, vandra mot en marktyp som inte finns ("searching for undefined").
  // Inget S.rand dras här; strömmen skiljer sig först genom att själen arbetar
  // i stället för att vandra.
  if(need&&SMELT[need]){
    if(a.knows.has(need)){
      const _r=SMELT[need];
      if(Object.keys(_r).every(mm=>(a.inv[mm]||0)>=_r[mm])){
        for(const mm in _r)a.inv[mm]-=_r[mm];
        a.inv[need]=(a.inv[need]||0)+1;
        a.task='working '+need;
        return;
      }
      for(const mm in _r){if((a.inv[mm]||0)<_r[mm]){need=mm;break;}}
    } else need=null;
  }
  if(need){const tc=isTaboo(S,a,need);if(tc){if(wouldBreakTaboo(S,a,tc))a._breakC=tc;else need=null;}}
  if(need&&S.rand()<a.traits.diligence){
    doSeek(S,a,MATSOURCE[need],()=>{
      S.tiles[a.ty][a.tx0].n--;
      if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[need]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;S.tiles[a.ty][a.tx0]={t:'grass',n:0};if(typ==='forest'||typ==='clay'||typ==='sand')regrowLater(S,a.tx0,a.ty,typ);S.bgDirty=true;}
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
      // M3 (D-234): the curiosity expedition could bring home only six materials, and copper, tin,
      // coal, gold and pigment were not among them. The ore was ON THE MAP, the tech existed, the
      // insight existed in GATHER_OBS -- and no path in the engine ever led a soul to that tile, so
      // copperGreen could not be seen and the whole bronze->steel->clock->steam branch was
      // unreachable in every world. A curious person picks up the strange green stone. That is what
      // curiosity IS.
      const cand=['stone','fiber','sand','clay','iron','wood','copper','tin','coal','gold','pigment'].filter(m=>!isTaboo(S,a,m));
      if(cand.length){
        const m=pick(S,cand);
        const t=findNearest(S,a,MATSOURCE[m]);
        // how far an expedition dares go grows with what the people know about moving -- the same
        // ladder the world-gate uses -- CORRECTION (D-239 review): it is a PARALLEL ladder, not the same one. This reads the INDIVIDUAL's knows and runs 25/32/44/52; reachOf reads the VILLAGE's pooled knowledge and runs 16/24/36/44. Finding is a person walking; keeping is a community supplying. They agree in direction, not in number.
        let far=25;
        if(a.knows.has('wheel'))far=32; if(a.knows.has('sailing'))far=44; if(a.knows.has('road'))far=52;
        if(m==='fiber'||t&&Math.hypot(t.x-a.x,t.y-a.y)<=far)a.forage=m;
      }
    }
    if(a.forage){
      const m=a.forage;
      doSeek(S,a,MATSOURCE[m],()=>{
        S.tiles[a.ty][a.tx0].n--;
        if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[m]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;S.tiles[a.ty][a.tx0]={t:'grass',n:0};if(typ==='forest'||typ==='clay'||typ==='sand')regrowLater(S,a.tx0,a.ty,typ);S.bgDirty=true;}
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
  if(S.fields.length>=Math.max(3,S._aliveN||S.agents.filter(x=>!x.dead).length))return false;
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
  assignVillages(S); // ENGINE 2.1 (D-086): cache village membership once per tick (O(1) lookups after)
  buildGrid(S);      // ENGINE 2.3 (D-089): spatial hash for O(local) neighbour scans + alive count cache
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
        // E1.5b (V6, review I1): EVERY winter now enters the log at onset — the order's chain
        // "winter took the harvest -> Torv stole" needs the winter to be a citable event, not a
        // season flag. First and hard winters keep their voices; ordinary winters get a plain
        // line. No rand consumed; the chronicle's hard-winter filter is unaffected.
        let we;
        if(!S.seenWinter){S.seenWinter=true;we=ev(S,'season',`❄️ The first winter has come. The world turns white and hard, and the nights grow long. Fire is no longer comfort — it is life.`,{});}
        else if(S.winterSeverity>1.35&&(Math.floor(S.tick/YEAR)+1)-(S.lastHarshYear||0)>=12){
          S.lastHarshYear=Math.floor(S.tick/YEAR)+1;S.stats.harshWinters++;
          we=ev(S,'season',`❄️ A hard winter grips the world. The old will remember it; not all the young will see spring.`,{});
        }
        else we=ev(S,'season',`❄️ Winter closes over the world. The stores will decide who sees spring.`,{});
        S.lastWinterEv={id:we.id,tick:S.tick}; // V6 bookkeeping: the freshest winter, citable by desperation
      }
      if(ns==='spring'&&S.seenWinter&&S.rand()<.3)ev(S,'season',`🌸 Spring returns. The survivors count each other, and begin again.`,{});
    }
  }
  animalsTick(S);
  if(S.tick%YEAR===0)cultureYearTick(S);
  if(S.tick%YEAR===0)warTick(S); // TENSION PROTO: village-scale organised violence (the war rung)
  if(S.tick%YEAR===0)leaderTick(S); // E1.5 (D-166 B1): prestige -> recognized leader + tribute upward
  if(S.tick%YEAR===0)knowledgeRetentionTick(S); // ENGINE 2.1 (D-086): per-community knowledge census + local loss/rediscovery (yearly; pure readout)
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


// ENGINE 2.1 (D-086): a ROLE is an emergent LABEL, never assigned. It reads an individual's dominant
// knowledge + aptitude + the crafts their community can support — the same person is a smith in an
// iron valley, a boatwright on a coast, a mere forager where nothing has been discovered (EP philosophy).
// A role is a LABEL for what an individual became, not a list of what their village knows.
// Knowledge spreads to (almost) everyone, so gating a role purely on k.has(craft) collapses a
// whole population onto one high-priority craft (everyone "knows" painting => everyone a painter).
// Instead: among the crafts this person knows, pick the one their TRAITS make them best at — the
// EP's law that role emerges from the individual (traits -> drive -> specialization). Pure
// post-sim labeling: reads a.knows/a.traits only, never S.rand, never mutates — determinism-safe.
function roleOf(S,a){
  const k=a.knows, t=a.traits, T=n=>t[n]||0;
  // relative aptitude: what a person becomes is the craft where their OWN standout trait lies,
  // not the craft with the globally-biggest number. mean-centre so each individual gravitates to
  // their personal signature (high musicality -> musician; high dexterity -> smith/weaver). prestige
  // is a light tiebreak so an advanced craft edges a basic one at equal aptitude. Pure labeling.
  const keys=['curiosity','social','diligence','conformity','dexterity','creativity','musicality','empathy','ambition'];
  let mean=0; for(const n of keys)mean+=T(n); mean/=keys.length;
  const apt=n=>0.5+(T(n)-mean); // ~0.5 at baseline, higher where the person exceeds their own norm
  const cand=[]; const add=(id,ok,trait,prestige)=>{ if(ok)cand.push([id, apt(trait)+0.12*prestige]); };
  add('musician',   k.has('composition')||k.has('song'),        'musicality',1.00);
  add('painter',    k.has('painting'),                          'creativity',0.94);
  add('scholar',    k.has('science')||k.has('scholarship'),     'curiosity', 1.00);
  add('philosopher',k.has('philosophy'),                        'curiosity', 0.80);
  add('teacher',    k.has('university')||k.has('school'),        'empathy',   0.88);
  add('healer',     k.has('medicine'),                          'empathy',   0.90);
  add('priest',     k.has('temple')||k.has('monastery'),        'social',    0.84);
  add('magistrate', k.has('law'),                               'ambition',  0.86);
  add('merchant',   k.has('coinage'),                           'ambition',  0.84);
  add('smith',      k.has('steel')||k.has('smithing')||k.has('bronze'), 'dexterity',0.88);
  add('builder',    k.has('architecture')||k.has('masonry'),    'diligence', 0.82);
  add('boatwright', k.has('sailing'),                           'dexterity', 0.70);
  add('weaver',     k.has('weaving'),                           'dexterity', 0.60);
  add('potter',     k.has('pottery')||k.has('kiln'),            'dexterity', 0.50);
  add('farmer',     k.has('farming'),                           'diligence', 0.30);
  add('fisher',     k.has('fishing'),                           'diligence', 0.30);
  add('hunter',     k.has('spear')||k.has('bow'),               'ambition',  0.30);
  if(!cand.length)return 'forager';
  cand.sort((x,y)=>y[1]-x[1]||(x[0]<y[0]?-1:1));
  return cand[0][0];
}

// ---------- R2 INK1 (MOTOR-LANE-ORDER-R2-FAS4 §5): the engine owns the ERA concept ----------
// era = the highest TECH era any LIVING soul carries (0 before any era-tagged discovery) — the
// exact law the body derived interim (D-147), now motor-owned. Pure readout: no S.rand, no
// mutation. Canonical era NAMES (replace the body's interim dawn/stone/bronze/iron/mill/print/
// steam): index 0 is the wakening world (fire, sharp stone, rope, the first huts — the untagged
// base techs); index 1 is the settled hearth (brick, well, granary, storytelling — clay and
// tales, NOT stone tools, which live in index 0); 2-6 follow the materials and machines that
// define them (bronze; iron/steel; mills and clocks; the printing press; steam).
const ERAS=['The First Morning','The Age of Hearths','The Age of Bronze','The Age of Iron','The Age of Mills','The Age of the Press','The Age of Steam'];
function worldEra(S){let m=0;for(const a of S.agents){if(a.dead)continue;for(const t of a.knows){const q=TECH[t];if(q&&q.era>m)m=q.era;}}return m;}
function eraName(e){e=e|0;return ERAS[e<0?0:(e>=ERAS.length?ERAS.length-1:e)];}

// ---------- R2 INK1 (order §verb, Fas 2-grindens R2): canonical work/carry verb ----------
// Pure classifier over the EXISTING task strings (the prose strings themselves stay untouched —
// they are presentation text). The body animates role-true from the verb; unknown/future verbs
// must fall back to idle/walk body-side. Verb set (15): idle move gather carry work harvest
// hunt fish eat rest grow social ritual fight trade. 'carry' currently fires only for surplus
// hoarding ('adding to their store') — the only true haul in the sim today; an A->B haul
// mechanic would widen it. No S.rand, no mutation.
function verbOf(task){
  const t=String(task||'');
  if(t==='sleeping')return 'rest';
  if(t==='growing up')return 'grow';
  if(t==='eating'||t==='feasting on the hunt')return 'eat';
  if(t==='hunting')return 'hunt';
  if(t==='fishing')return 'fish';
  if(t==='harvesting')return 'harvest';
  if(t==='adding to their store')return 'carry';
  if(t==='trading')return 'trade';
  if(t==='taking food by force'||t==='settling a score'||t==='raiding a neighbour')return 'fight';
  if(t==='tilling the earth'||t==='experimenting'||t==='lighting a fire'||t==='tending the flame')return 'work';
  if(t.indexOf('gathering ')===0||t==='collecting curiosities')return 'gather';
  if(t==='talking'||t==='talking quietly, apart'||t.indexOf('visiting ')===0)return 'social';
  if(t.indexOf('joining the evening ')===0||t==='watching the sky with the others'||t==='keeping the rule of the shared fire')return 'ritual';
  if(t.indexOf('heading to ')===0||t.indexOf('searching for ')===0||t.indexOf('walking to ')===0||t.indexOf('going ')===0||t==='seeking warmth'||t==='seeking company'||t==='huddling for warmth')return 'move';
  return 'idle'; // 'thinking', 'wandering', and anything future
}

// ---------- VILLAGE SCOPE (export-only readout — MOTOR-LANE-ORDER-VILLAGE-SCOPE, 2026-08-09) ----------
// The C-condition's loss half made WITNESSABLE: per-village census {name,pop,maxGen,avgAge,crafts,knows}
// so the body can SEE what each village KNOWS (and later: loses). Membership = villageOf (the sim's own
// law); knows[] = union of living members' knows in TECHS-canonical order (deterministic). A pure READ:
// consumes no S.rand, mutates nothing — sim stream and goldens are byte-identical; only the engine
// file's own SHA shifts (the 2.3.1 flat-array precedent). Field names are CONTRACT with the body's
// WorldVillage parse and the seq-fixture format. cosmos/beliefs deliberately NOT in this wave.
function villageScope(S){
  const out=[];
  for(const v of S.villages){
    const mem=[];
    for(const a of S.agents){ if(!a.dead&&villageOf(S,a)===v)mem.push(a); }
    const ku=new Set();
    for(const a of mem)for(const k of a.knows)ku.add(k);
    const kn=[];
    for(const t of TECHS)if(ku.has(t.id))kn.push(t.id);
    let mg=0,ages=0;
    for(const a of mem){ if(a.gen>mg)mg=a.gen; ages+=a.age; }
    out.push({name:''+v.name,pop:mem.length,maxGen:mg,avgAge:mem.length?Math.round(ages/mem.length):0,crafts:kn.length,knows:kn});
  }
  return out;
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
    // E1.5 (D-166 B1): the drama's ledger — additive tail (no field removed/renamed above)
    violence:{steals:S.stats.steals||0,raids:S.stats.raids||0,feuds:S.stats.feuds||0,killings:S.stats.killings||0,brawls:S.stats.brawls||0,wars:S.stats.wars||0},
    trades:S.stats.trades||0,gifts:S.stats.gifts||0,
    leaders:S.villages.filter(v=>v.leaderName).map(v=>v.name+': '+v.leaderName),
    giftWays:S.villages.filter(v=>v.giftName).map(v=>v.name+': '+v.giftName),
    wealthTop:(()=>{let t=null;for(const a2 of S.agents){if(a2.dead)continue;const w=wealth(a2);if(!t||w>t.w)t={w,name:disp(a2)};}return t?t.name+' ('+t.w+')':'none';})(),
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
    let births=0,deaths={starvation:0,cold:0,age:0,wolves:0},techs=[],losses=[],redisc=[],vills=[],trads=[],rels=[],tabs=[],breaks=[],gods=0,hasStart=false,hasEnd=false,convs=0,reforms=[],fades=[],lenses=[],hard=[],wolves=[],hunts=[],sharing=0,epis=[],quirks=[],legends=[],journeys=[],
        steals=[],raidsE=[],feudsE=[],mourns=[],leadersE=[],giftways=[],tribs=0; // E1.5: the drama enters the chronicle
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
      else if(e.type==='steal')steals.push(e);
      else if(e.type==='raid')raidsE.push(e);
      else if(e.type==='feud')feudsE.push(e);
      else if(e.type==='mourn')mourns.push(e);
      else if(e.type==='leader')leadersE.push(e);
      else if(e.type==='giftway')giftways.push(e);
      else if(e.type==='tribute')tribs++;
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
    // E1.5: the chronicle tells the drama — every act below fell out of the forces, none was scripted.
    for(const e of leadersE)text.push(stripTags(e.txt).replace(/^👑 |^🕯️ /,''));
    for(const e of giftways)text.push(stripTags(e.txt).replace(/^🍞 /,''));
    if(tribs)text.push('And each harvest-turn, those with surplus laid a share at the leader\'s door. So standing became wealth, and wealth standing.');
    if(steals.length){const s0=steals[0];text.push(steals.length===1?('In the year '+s0.year+', hunger drove a hand to take what was another\'s: '+stripTags(s0.txt).replace(/^🥀 Hunger owned the hand: /,'')+' It was not forgotten.'):('Hunger turned to theft '+steals.length+' times in this age — desperate hands in lean seasons, and every one remembered.'));}
    if(raidsE.length){const r0=raidsE[0];text.push(raidsE.length===1?('The year '+r0.year+' knew open greed: '+stripTags(r0.txt).replace(/^⚔️ /,'')):('Greed walked openly '+raidsE.length+' times — the strong taking because the gap was cheaper to close by force.'));}
    // E1.5b (V8, review I8): feud lines obey the SAME room law as the other categories (the
    // quirk rule) — a chapter is a telling, not a ledger; on long horizons or a hot feudRate
    // the blood must not crowd out the rest of the age. Truncation is counted honestly.
    if(feudsE.length){
      const froom=Math.max(1,Math.min(feudsE.length,text.length<5?3:1));
      const fsel=[...feudsE];while(fsel.length>froom)fsel.splice(Math.floor(rng()*fsel.length),1);
      for(const e of fsel)text.push('The year '+e.year+' paid an old debt: '+stripTags(e.txt).replace(/^🩸 /,'')+' Blood remembers.');
      if(feudsE.length>fsel.length)text.push('Nor were those the only debts: '+feudsE.length+' old wrongs were paid in blood that age.');
    }
    if(mourns.length)text.push(mourns.length===1?stripTags(mourns[0].txt).replace(/^🕯️ /,'')+' Grief like that does not fade; it waits.':'And '+mourns.length+' times, mourners stood over the fallen and marked a name in their hearts. Grief like that waits.');
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
    else if(feudsE.length>=2)title='The Blood Years'; // E1.5: the feud can name an age
    else if(leadersE.length&&leadersE.some(e=>e.label==='A LEADER IS RECOGNIZED'))title='The Age of the Recognized';
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

return {createWorld,tickWorld,computeDNA,villageScope,resimulate,writeHistory,roleOf,verbOf,worldEra,eraName,ERAS,wealthOf:wealth,TECHS,TECH,OBS,QUIRK,W,H,YEAR,SEASONS,VERSION:'2.6.0'};
});
