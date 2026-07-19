/* ============================================================================
   .NET HOSTING CONTRACT PRELUDE — Math.hypot override (V8 algorithm)
   TD-008 / UNITY-BRIDGE-SPEC §2.3: V8 computes hypot with scale-by-max +
   Kahan-compensated summation; .NET/Jint computes naive sqrt(x²+y²) — 1 ULP
   apart in ~40% of calls, enough to fork a history (proven on seed 20260718).
   This prelude is executed by every .NET host BEFORE loading the engine.
   The engine file itself is NEVER edited. Fuzz-verified against native V8.
   ============================================================================ */
(function () {
  'use strict';
  var abs = Math.abs, sqrt = Math.sqrt;
  Math.hypot = function hypot() {
    var length = arguments.length;
    var args = [];
    var max = 0;
    for (var i = 0; i < length; i++) {
      var n = abs(Number(arguments[i]));
      if (n > max) max = n;
      args[i] = n;
    }
    if (max === Infinity) return Infinity;
    if (max === 0) return 0;
    var sum = 0;
    var compensation = 0;
    for (var j = 0; j < length; j++) {
      var m = args[j] / max;
      var summand = m * m - compensation;
      var preliminary = sum + summand;
      compensation = (preliminary - sum) - summand;
      sum = preliminary;
    }
    return sqrt(sum) * max;
  };
})();
