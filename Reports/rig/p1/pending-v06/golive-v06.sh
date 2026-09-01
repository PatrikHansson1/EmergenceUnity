#!/bin/bash
# P1 v0.6 go-live (D-656) — körs EFTER v20-commit, aldrig under en golden. Bash-VM via bryggan, cwd = EmergenceUnity-roten.
set -e
NEW=51cca887ea7579cdf8a0f82d956691fcf269606f51c9f5e4e40ff6267b70833b; OLD=1c7ba2da72684f748a54f52150aad1c4adf17162cc9503edb6ed875bcd1bd3f3
[ "$(sha256sum Assets/StreamingAssets/Emergence/emergence-presentation.js | cut -c1-64)" = "$OLD" ] || { echo "live P1 är inte v0.5!"; exit 1; }
[ "$(sha256sum Reports/rig/p1/pending-v06/emergence-presentation.js | cut -c1-64)" = "$NEW" ] || { echo "pending-v06 har fel sha!"; exit 1; }
mkdir -p Reports/rig/p1/backup-v05; cp Assets/StreamingAssets/Emergence/emergence-presentation.js Assets/StreamingAssets/Emergence/PRESENTATION-SHA.txt Reports/rig/p1/backup-v05/
cp Reports/rig/p1/pending-v06/emergence-presentation.js Assets/StreamingAssets/Emergence/emergence-presentation.js
printf '%s' "$NEW" > Assets/StreamingAssets/Emergence/PRESENTATION-SHA.txt
python3 - <<'PY'
p='Assets/Emergence/Runtime/EmergenceJintHost.cs'; s=open(p,encoding='utf-8').read()
old='public const string ExpectedPresentationSha = "1c7ba2da72684f748a54f52150aad1c4adf17162cc9503edb6ed875bcd1bd3f3"; //'
assert s.count(old)==1
s=s.replace(old,'public const string ExpectedPresentationSha = "51cca887ea7579cdf8a0f82d956691fcf269606f51c9f5e4e40ff6267b70833b"; // P1 v0.6 (D-648/D-656, 2026-09-01): tagFor uses the by-tag instead of a /144 division ("(born 0)" gone -> "of Stenholm"), SENTENCES{legend:2,customLost:2}, weightOf knowledgeLost scoped 20 (village-level while the tech lives) / 45 (world-level). P1-goldens on engine v20: 97013 460e621f / 4242 99e2a8fb / 20260718 699a93fa (Reports/rig/p1/pending-v06/p1-goldens-v06-on-v20.json). Prior sha (v0.5): 1c7ba2da... //')
open(p,'w',encoding='utf-8').write(s); print('JintHost P1 sha bumped')
PY
cp Reports/rig/p1/pending-v06/p1-goldens-v06-on-v20.json Reports/rig/p1/p1-goldens.json
echo "RUN_COMPILE" > Reports/RUN_COMPILE.trigger
echo "v0.6 installerad; RUN_COMPILE armad. Sedan: RUN_FAS4NATIVE (ögonbevis _fas4-native-book.jpg), RUN_LANG, commit."
