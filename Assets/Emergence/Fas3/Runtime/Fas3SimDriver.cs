// EMERGENCE — FAS 3 increment 1 (D-133): THE LIVING LOOP. The engine ticks LIVE on a worker thread
// (Jint, ~50 ms/tick in-editor) and the presentation consumes year-boundary snapshots.
//
// TIME LAW (Fas 3 gate): speed/pause affect ONLY how many ticks the presentation may consume per
// real second (token bucket). The engine always ticks sequentially from its own state — state at
// tick T is BY CONSTRUCTION independent of pacing. The probe proves it: two runs, same seed,
// different pacing (with pause/resume), identical snapshot hash at the same tick.
// D-078 r4: this class READS state (exports snapshots); it never writes into the sim.
//
// FAS 3 increment 4 (D-136/D-137): the LOOKAHEAD BUFFER + CHECKPOINT GRID. Player-Jint flat-out is
// 19 t/s (< the 24 t/s that 1× needs), so real-time pacing cannot ride the engine directly. In
// bufferMode the worker RACES flat-out (paused/ticksPerSecond are ignored — pacing moves to
// Fas3PresentationClock, which consumes the year queue at 1×/4×), the pending slot becomes a FIFO
// YEAR QUEUE (capped at lookaheadYears — the producer stalls, never drops), and every year snapshot
// is PERSISTED as a checkpoint (the seq pattern: seq-<seed>-yNNN.json in persistentDataPath) so
// pause/scrub can re-enter any produced year without resimulating. Legacy mode (bufferMode=false)
// keeps increment-1 semantics exactly: token bucket paces the worker, queue depth 1 with
// latest-wins overwrite — the D-133/134/135 probes run unchanged.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Jint;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3SimDriver : MonoBehaviour
    {
        public long seed = 8919;
        [Tooltip("Presentation pacing only — ticks the worker MAY consume per real second. Never touches the sim. IGNORED in bufferMode (the clock paces consumption instead).")]
        public float ticksPerSecond = 24f;
        public bool paused;
        [Tooltip("Stop ticking at this year (-1 = endless). The final snapshot is exported exactly at the boundary.")]
        public int targetYear = -1;
        [Tooltip("D-136: worker races flat-out, year snapshots queue up (lookahead) + persist as checkpoints; presentation pacing moves to Fas3PresentationClock.")]
        public bool bufferMode;
        [Tooltip("Max year snapshots buffered ahead of the consumer in bufferMode — the producer stalls (never drops) at this depth.")]
        public int lookaheadYears = 16;

        public int Tick => _tick;
        public int Year => _year;
        public int YearTicks => _yearTicks;   // engine YEAR (144), read once at boot
        public bool Finished => _finished;
        public string FinalHash => _finalHash;   // sha256 of the export at targetYear (determinism proof)
        public string LastError => _error;
        public int BufferedYears { get { lock (_lock) return _queue.Count; } }
        public string CheckpointDir => _checkpointDir;   // set at Start (main thread); worker writes into it
        // Fas 7 (save/load teardown honesty): TRUE while the worker thread lives. A destroyed driver's
        // worker may still be inside its current year batch and write ONE more checkpoint — a restorer
        // that wipes the grid must wait for this to go false, or a stale file could masquerade as a
        // fresh resimulation. Read-only observability; no behavior change.
        public bool WorkerAlive => _worker != null && _worker.IsAlive;
        /// <summary>Fas 7: ask the worker to stop NOW (it exits at the next between-batch check, ≤ one
        /// year batch away) without waiting for Destroy's deferred OnDestroy. Idempotent, read-side only.</summary>
        public void StopWorker() { _stop = true; }

        volatile int _tick; volatile int _year; volatile bool _finished;
        string _finalHash = ""; string _error = "";
        readonly Queue<string> _queue = new Queue<string>(); readonly object _lock = new object();
        Thread _worker; volatile bool _stop;
        string _engineSrc, _preludeSrc, _checkpointDir = "";
        int _yearTicks = 12;

        // VÅG 1.1 (2026-08-14, D-209): THE MAP IS NOW EXPORTED. The old note below said the dresser
        // 'doesn't need tiles live — terrain is dressed once from a verified snapshot'. That was true
        // of the editor dresser and it is exactly why the PLAYER never got ground: with no tiles in
        // the live export, nothing downstream could build terrain even after the law moved to Runtime.
        // 100x70 = 7000 tiles is 7 KB — less than pathUse, which was already exported every year.
        // node-exporter parity otherwise unchanged.
        // R2 INK1 (MOTOR-LANE-ORDER-R2-FAS4, engine wave): ADDITIVE fields only — eraName (motor-owned
        // era canon, §5), agents[].verb (canonical work/carry verb, §verb) and pathUse (cumulative
        // footfall per tile, row-major y*W+x, §pathUse). The pre-existing fields and their values are
        // byte-identical to before (era keeps the same inline law; E.worldEra is the same law motor-side).
        // E1.5 (MOTOR-LANE-ORDER-E15-DRAMATIK, engine 2.4.0): ADDITIVE again — agents[].wealth
        // (E.wealthOf, the Almanac's wealth sort), villages[].leader (recognized-leader name or '')
        // D-254: and villages[].lost / .everHeld — how many things this people has FORGOTTEN and
        // how many it has ever held. The engine has carried v.lostNow and v.everHeld since the
        // Memory Engine, and neither has ever crossed into the body: at year 120 of seed 4242 all
        // three villages are holding losses (tin, bronze, coinage, copper, smithing, metaltools)
        // and the world shows nothing at all. Forgetting is this game's subject; it was the one
        // fact the presentation could not see. Pure read, additive, the engine untouched.
        // and villages[].gift (the named gift-way or ''). Nothing removed or renamed.
        // FAS 4 PROSE WIRING (2026-08-13, FAS4-PROSE-DIRECTOR-ORDER §1): ADDITIVE again —
        // events[] = a BOUNDED (<=64, newest-last) tail of the engine's own causes-bearing events,
        // each cause reference RESOLVED here to a reader-ready phrase (the lookup tables — S.events,
        // S.agents, S.knowledge, E.TECH — live engine-side, so resolving anywhere else would mean
        // shipping the whole log). Pure READ: the engine JS is untouched, nothing is written back to
        // The tail is filtered to the event types the chronicle can actually stand behind (child /
        // death / the E1.5 acts / leader / giftway), so the 64 slots hold matchable beats instead of
        // being flooded by tech+hoard events the body never witnesses as their own entries.
        // Causes are ordered by CLASS before resolving (cause: > agent: > tech: > ev:) so the
        // FIRST cause is deterministically the most concrete force — the one an oral chronicle
        // keeps when it can keep only one. Before this it was emission order, so swapping two
        // concat() calls engine-side would have rewritten every remembered line in the book
        // while the golden master stayed 4/4 green (fientlig granskning inv. 6).
        // Names outlive the roster: the engine prunes the dead (S.agents.filter(!dead)), so a
        // parent who has since died resolved to 'someone' — in the birth of their own child, the
        // most loaded line the book has. The dead are still named in their OWN events (death
        // and birth both open with the name in bold), so the resolver falls back to those.
        // Condition B is a retellable story WITH NAMES; 'someone had come first' fails it.
        // S, no RNG is consumed, and the golden master runs a different path entirely (harness.js).
        // BELIEF SCOPE (C2 of D-226, 2026-08-14): villages[] now also carries cosmos + beliefs[] — the
        // ALIVE custom TARGETS held by that village's living members (cosmos/harm/fire/iron/night/wolf/
        // wood/worldview). Until now the export shipped none of it, so every codex gate written as
        // requiresCustom was SILENTLY DEAD in the live game: the star-banner could never rise because
        // nothing ever told the presentation that a people had learned to read the sky. Pure READ over
        // a.customs / S.customs and the engine's own a._vil assignment; no S.rand, no mutation.
        // VILLAGE-SCOPE (MOTOR-LANE-ORDER-VILLAGE-SCOPE 2026-08-09, TD-085): ADDITIVE again —
        // villages[] merges E.villageScope(S) {pop,maxGen,avgAge,crafts,knows[]} (same order as
        // S.villages, the engine's ONE census law) so the Almanac's village dossier and the
        // C-condition's loss half read LIVE data, not just fixtures. dna is now JSON (the D-179
        // '[object Object]' quirk fixed) — a parseable world fingerprint for divergence proofs.
        const string ExportJs = @"(function(){var E=Emergence,S=__S;return JSON.stringify({
engineVersion:E.VERSION,seed:__seed,years:Math.floor(S.tick/E.YEAR),tick:S.tick,ended:!!S.ended,season:''+S.season,
era:(function(){var m=0;S.agents.forEach(function(a){if(a.dead)return;a.knows.forEach(function(t){var q=E.TECH[t];if(q&&q.era>m)m=q.era})});return m})(),
eraName:''+E.eraName(E.worldEra(S)),
W:E.W,H:E.H,
tileTypes:(function(){var o='';for(var y=0;y<E.H;y++)for(var x=0;x<E.W;x++)o+=(''+S.tiles[y][x].t).charAt(0);return o})(),tileN:(function(){var o=[];for(var y=0;y<E.H;y++)for(var x=0;x<E.W;x++)o.push(S.tiles[y][x].n);return o})(),
agents:S.agents.filter(function(a){return !a.dead}).map(function(a){return {id:a.id,name:''+a.name,x:a.x,y:a.y,age:a.age,gen:a.gen,task:''+a.task,verb:''+E.verbOf(a.task),say:''+(a.say||''),sayAct:''+(a.sayAct||''),home:!!a.home,wealth:E.wealthOf(a)}}),
dead:S.agents.filter(function(a){return a.dead}).length,
huts:S.huts.map(function(h){return {x:h.x,y:h.y,owner:''+(h.owner||''),free:!!h.free}}),
fires:S.fires.map(function(f){return {x:f.x,y:f.y,fuel:f.fuel}}),
fields:S.fields.map(function(f){return {x:f.x,y:f.y,stage:f.stage,owner:''+(f.owner||'')}}),
villages:(function(){var sc=E.villageScope(S);return S.villages.map(function(v,i){var s=sc[i];var bs={},cos='';S.agents.forEach(function(a){if(a.dead||a._vil!==v)return;a.customs.forEach(function(cid){var c=S.customs[cid];if(!c||c.status!=='alive'||!c.target)return;bs[c.target]=1;if(c.target==='cosmos'&&!cos)cos=''+c.name})});return {x:v.x,y:v.y,name:''+v.name,leader:''+(v.leaderName||''),gift:''+(v.giftName||''),lost:(v.lostNow?v.lostNow.size:0),everHeld:(v.everHeld?v.everHeld.size:0),pop:s.pop,maxGen:s.maxGen,avgAge:s.avgAge,crafts:s.crafts,knows:s.knows,cosmos:cos,beliefs:Object.keys(bs).sort()}})})(),
animals:S.animals.map(function(an){return {id:an.id,type:''+an.type,x:an.x,y:an.y}}),
pathUse:S.pathUse||[],
worldKnows:(function(){var o=[];for(var k in S.knowledge)if(S.knowledge[k]&&S.knowledge[k].status==='alive')o.push(k);return o.sort()})(),
events:(function(){var A=S.events,n=A.length,o=[],lim=64,KEEP={child:1,death:1,steal:1,raid:1,feud:1,mourn:1,sharing:1,giftway:1,leader:1};var bold=function(t){var m=/<b>([^<]+)<\/b>/.exec(''+(t||''));return m?m[1]:''};var nm=function(id){for(var i=0;i<S.agents.length;i++)if(S.agents[i].id===id)return ''+S.agents[i].name;for(var j=A.length-1;j>=0;j--){var e3=A[j];if(e3.agent===id&&(e3.type==='death'||e3.type==='child')){var b=bold(e3.txt);if(b)return b}}return 'someone'};var tn=function(t){var k=S.knowledge[t],q=E.TECH[t];var a=k&&k.name?''+k.name:(q?''+q.base:''+t);return(q&&k&&k.name)?a+' ('+q.base+')':a};var strip=function(t){t=(''+(t||'')).replace(/<[^>]*>/g,' ');t=t.replace(/[^\x20-\x7E\u00A0-\u024F\u2010-\u203A]/g,' ');t=t.replace(/\s+/g,' ').trim();var c=t.search(/[.!?]/);if(c>20)t=t.slice(0,c);t=t.replace(/[\s\-\u2013\u2014,;:]+$/,'');return t.length>110?t.slice(0,107).replace(/\s+\S*$/,'')+'\u2026':t};var res=function(r){r=''+r;var c=r.indexOf(':');if(c<0)return r;var k=r.slice(0,c),v=r.slice(c+1);if(k==='ev'){var j=+v;if(!(j>=0&&j<n&&A[j]))return '';var e2=A[j];return e2.tech?tn(e2.tech):strip(e2.txt)}if(k==='agent')return nm(+v);if(k==='tech')return tn(v);if(k==='cause')return v;return r};for(var i=n-1;i>=0&&o.length<lim;i--){var e=A[i];if(!KEEP[e.type])continue;if(!e.causes||!e.causes.length)continue;var rk=function(r){var k=(''+r).split(':')[0];return k==='cause'?0:(k==='agent'?1:(k==='tech'?2:3))};var ord=e.causes.slice().sort(function(a,b){return rk(a)-rk(b)});var cs=[],seen={};for(var q=0;q<ord.length&&cs.length<4;q++){var rr=res(ord[q]);if(rr&&!seen[rr]){seen[rr]=1;cs.push(rr)}}if(!cs.length)continue;o.push({id:e.id,year:e.year,type:''+e.type,agent:(e.agent===undefined?-1:e.agent),village:''+(e.village||''),causes:cs})}return o.reverse()})(),
dna:JSON.stringify(E.computeDNA(S))})})()";

        void Start()
        {
            // file IO on the main thread; the worker gets plain strings (no Unity APIs off-thread).
            // Engine source: EngineSourcePath already prefers the StreamingAssets 2.3 twin (D-093) and
            // StreamingAssets is packaged into player builds — so the SAME file drives editor and player
            // (increment 3b). The prelude likewise: StreamingAssets copy first, editor Engine/ fallback.
            try
            {
                string engineDir = Path.Combine(Application.dataPath, "Emergence", "Engine");
                _engineSrc = File.ReadAllText(EmergenceJintHost.EngineSourcePath(engineDir));
                string saPrelude = Path.Combine(Application.streamingAssetsPath, "Emergence", "harness", "prelude-hypot.js");
                _preludeSrc = File.ReadAllText(File.Exists(saPrelude) ? saPrelude : Path.Combine(engineDir, "harness", "prelude-hypot.js"));
            }
            catch (Exception e) { _error = "load: " + e.Message; return; }
            if (bufferMode)
            {
                // persistentDataPath is main-thread-only — resolve here, worker only does plain file IO
                try { _checkpointDir = Path.Combine(Application.persistentDataPath, "Emergence", "checkpoints"); Directory.CreateDirectory(_checkpointDir); }
                catch (Exception e) { _error = "checkpointDir: " + e.Message; return; }
            }
            _worker = new Thread(Work) { IsBackground = true, Name = "Fas3SimDriver" };
            _worker.Start();
        }

        void OnDestroy() { _stop = true; }

        /// <summary>Presentation-side: dequeue the next year snapshot in year order (null if none ready).
        /// Legacy mode keeps latest-wins depth-1 semantics; bufferMode is a strict FIFO over years.</summary>
        public string TakeYearSnapshot() { lock (_lock) { return _queue.Count > 0 ? _queue.Dequeue() : null; } }

        void Work()
        {
            try
            {
                var host = new EmergenceJintHost(_engineSrc, _preludeSrc);
                var eng = host.Engine;
                eng.Execute($"var __seed={seed}; var __S=Emergence.createWorld({seed}); __S.silent=true;");
                _yearTicks = (int)eng.Evaluate("Emergence.YEAR").AsNumber();

                // D-138/onboarding: in bufferMode the GENESIS state (tick 0, year 0) is a first-class
                // snapshot — enqueued and checkpointed so the presentation can apply the world's
                // truth from frame one (the cold start owns the first beat; no backdrop cheat).
                if (bufferMode)
                {
                    string json0 = eng.Evaluate(ExportJs).AsString();
                    lock (_lock) _queue.Enqueue(json0);
                    if (_checkpointDir.Length > 0)
                    {
                        try { File.WriteAllText(Path.Combine(_checkpointDir, $"seq-{seed}-y000.json"), json0); }
                        catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3SimDriver] genesis checkpoint: " + e.Message); }
                    }
                }

                var sw = Stopwatch.StartNew();
                double budget = 0, last = 0;
                while (!_stop)
                {
                    double now = sw.Elapsed.TotalSeconds;
                    if (!bufferMode && !paused) budget += (now - last) * Mathf.Max(0.01f, ticksPerSecond);
                    last = now;
                    int stopTick = targetYear >= 0 ? targetYear * _yearTicks : int.MaxValue;

                    int n;
                    if (bufferMode)
                    {
                        // D-136: race flat-out; stall (never drop) when the lookahead buffer is full
                        bool full; lock (_lock) full = _queue.Count >= Math.Max(1, lookaheadYears);
                        if (full) { Thread.Sleep(20); continue; }
                        n = _yearTicks;                                    // one whole year per batch
                    }
                    else
                    {
                        n = (int)budget;
                        if (paused || n <= 0) { Thread.Sleep(10); continue; }
                    }
                    n = Math.Min(n, Math.Max(0, stopTick - _tick));
                    n = Math.Min(n, _yearTicks - (_tick % _yearTicks));   // never batch past a year boundary — every year exports
                    if (n > 0)
                    {
                        if (!bufferMode) budget -= n;
                        int before = _tick / _yearTicks;
                        eng.Execute($"for(var i=0;i<{n};i++)Emergence.tickWorld(__S);");
                        _tick += n;
                        int after = _tick / _yearTicks;
                        if (after > before || _tick >= stopTick)
                        {
                            string json = eng.Evaluate(ExportJs).AsString();
                            _year = after;
                            lock (_lock)
                            {
                                if (!bufferMode && _queue.Count > 0) _queue.Dequeue();   // legacy: latest wins (depth 1)
                                _queue.Enqueue(json);
                            }
                            // checkpoint grid (D-136): every produced year persists — scrub re-enters any of them
                            if (bufferMode && _checkpointDir.Length > 0)
                            {
                                try { File.WriteAllText(Path.Combine(_checkpointDir, $"seq-{seed}-y{after:000}.json"), json); }
                                catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3SimDriver] checkpoint write: " + e.Message); }
                            }
                            if (_tick >= stopTick)
                            {
                                _finalHash = EmergenceJintHost.Sha256Hex(json);
                                _finished = true;
                                return;
                            }
                        }
                    }
                    else Thread.Sleep(5);
                }
            }
            catch (Exception e) { _error = "worker: " + e.Message; _finished = true; }
        }
    }
}
