// EMERGENCE — FAS 3 increment 1 (D-133): THE LIVING LOOP. The engine ticks LIVE on a worker thread
// (Jint, ~50 ms/tick in-editor) and the presentation consumes year-boundary snapshots.
//
// TIME LAW (Fas 3 gate): speed/pause affect ONLY how many ticks the presentation may consume per
// real second (token bucket). The engine always ticks sequentially from its own state — state at
// tick T is BY CONSTRUCTION independent of pacing. The probe proves it: two runs, same seed,
// different pacing (with pause/resume), identical snapshot hash at the same tick.
// D-078 r4: this class READS state (exports snapshots); it never writes into the sim.
using System;
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
        [Tooltip("Presentation pacing only — ticks the worker MAY consume per real second. Never touches the sim.")]
        public float ticksPerSecond = 24f;
        public bool paused;
        [Tooltip("Stop ticking at this year (-1 = endless). The final snapshot is exported exactly at the boundary.")]
        public int targetYear = -1;

        public int Tick => _tick;
        public int Year => _year;
        public int YearTicks => _yearTicks;   // engine YEAR (144), read once at boot
        public bool Finished => _finished;
        public string FinalHash => _finalHash;   // sha256 of the export at targetYear (determinism proof)
        public string LastError => _error;

        volatile int _tick; volatile int _year; volatile bool _finished;
        string _finalHash = ""; string _error = "";
        string _pendingSnapshot; readonly object _lock = new object();
        Thread _worker; volatile bool _stop;
        string _engineSrc, _preludeSrc;
        int _yearTicks = 12;

        // node-exporter parity (minus tiles: the 2.3 flat grid is engine-internal and the dresser
        // doesn't need tiles live — terrain is dressed once from a verified snapshot).
        const string ExportJs = @"(function(){var E=Emergence,S=__S;return JSON.stringify({
engineVersion:E.VERSION,seed:__seed,years:Math.floor(S.tick/E.YEAR),tick:S.tick,ended:!!S.ended,season:''+S.season,W:E.W,H:E.H,
tileTypes:'',tileN:[],
agents:S.agents.filter(function(a){return !a.dead}).map(function(a){return {id:a.id,name:''+a.name,x:a.x,y:a.y,age:a.age,gen:a.gen,task:''+a.task,say:''+(a.say||''),sayAct:''+(a.sayAct||''),home:!!a.home}}),
dead:S.agents.filter(function(a){return a.dead}).length,
huts:S.huts.map(function(h){return {x:h.x,y:h.y,owner:''+(h.owner||''),free:!!h.free}}),
fires:S.fires.map(function(f){return {x:f.x,y:f.y,fuel:f.fuel}}),
fields:S.fields.map(function(f){return {x:f.x,y:f.y,stage:f.stage,owner:''+(f.owner||'')}}),
villages:S.villages.map(function(v){return {x:v.x,y:v.y,name:''+v.name}}),
animals:S.animals.map(function(an){return {id:an.id,type:''+an.type,x:an.x,y:an.y}}),
dna:''+E.computeDNA(S)})})()";

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
            _worker = new Thread(Work) { IsBackground = true, Name = "Fas3SimDriver" };
            _worker.Start();
        }

        void OnDestroy() { _stop = true; }

        /// <summary>Presentation-side: fetch and clear the latest year snapshot (null if none new).</summary>
        public string TakeYearSnapshot() { lock (_lock) { var s = _pendingSnapshot; _pendingSnapshot = null; return s; } }

        void Work()
        {
            try
            {
                var host = new EmergenceJintHost(_engineSrc, _preludeSrc);
                var eng = host.Engine;
                eng.Execute($"var __seed={seed}; var __S=Emergence.createWorld({seed}); __S.silent=true;");
                _yearTicks = (int)eng.Evaluate("Emergence.YEAR").AsNumber();

                var sw = Stopwatch.StartNew();
                double budget = 0, last = 0;
                while (!_stop)
                {
                    double now = sw.Elapsed.TotalSeconds;
                    if (!paused) budget += (now - last) * Mathf.Max(0.01f, ticksPerSecond);
                    last = now;
                    int stopTick = targetYear >= 0 ? targetYear * _yearTicks : int.MaxValue;

                    int n = (int)budget;
                    if (paused || n <= 0) { Thread.Sleep(10); continue; }
                    n = Math.Min(n, Math.Max(0, stopTick - _tick));
                    n = Math.Min(n, _yearTicks - (_tick % _yearTicks));   // never batch past a year boundary — every year exports
                    if (n > 0)
                    {
                        budget -= n;
                        int before = _tick / _yearTicks;
                        eng.Execute($"for(var i=0;i<{n};i++)Emergence.tickWorld(__S);");
                        _tick += n;
                        int after = _tick / _yearTicks;
                        if (after > before || _tick >= stopTick)
                        {
                            string json = eng.Evaluate(ExportJs).AsString();
                            _year = after;
                            lock (_lock) _pendingSnapshot = json;
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
