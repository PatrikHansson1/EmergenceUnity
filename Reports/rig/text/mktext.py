#!/usr/bin/env python3
# mktext.py — TEXTPAKETET väg B (D-704/D-731: C+B godkänd av Patrik 4 sep): motor-textfynd 1/3/6/7
# ur HOGLASNINGSFYND.md som EGEN generator (mkb23-mönstret, räknade ankare).
# STATUS: UTKAST — appliceras FÖRST i lås D-vågen (efter M4-dom), FÖRE ev. mkd1-successor, så varje
# golden-diff har en ägare. Text-only ⇒ goldens RED-by-intent ⇒ omspelning per D-614-protokollet.
# Kör: python mktext.py <in-engine.js> <ut-engine.js>
import sys, io, hashlib

src_p, out_p = sys.argv[1], sys.argv[2]
s = io.open(src_p, encoding="utf-8").read()
n0 = 0
def sub(old, new, expect, tag):
    global s, n0
    c = s.count(old)
    assert c == expect, f"ANKARE {tag}: {c} träffar, väntade {expect}"
    s = s.replace(old, new); n0 += c

# FYND 1 — epitet dubbleras (97013 y150 "… the Voice of Runeheim the Voice of Runeheim's door").
# Rotorsaksvakt i disp(): lägg aldrig epitetet om namnet redan slutar med det. Text-only, idempotent.
sub("function disp(a){return a.epithet?a.name+' '+a.epithet:a.name;}",
    "function disp(a){return a.epithet&&!a.name.endsWith(a.epithet)?a.name+' '+a.epithet:a.name;}",
    1, "F1-disp")

# FYND 3 — versal mitt i mening (custom-namn): gemena första tecknet i löpande text.
# Hjälpare injiceras direkt efter disp-raden (ny rad, additiv):
sub("function giveEpithet(S,a,ep){",
    "function lcn(n){return n&&/^[A-Z]/.test(n)?n.charAt(0).toLowerCase()+n.slice(1):n;}\nfunction giveEpithet(S,a,ep){",
    1, "F3-helper")
sub("has left ${oc.name} for ${cu.name}. Minds change",
    "has left ${lcn(oc.name)} for ${lcn(cu.name)}. Minds change", 1, "F3-conversion")
sub("the old way is set aside: <b>${heir.name}</b> replaces ${c.name}.",
    "the old way is set aside: <b>${lcn(heir.name)}</b> replaces ${lcn(c.name)}.", 1, "F3-reformation")

# FYND 6 — possessiv + artikel i kunskapsnamn ("Embla's the telling", "Ask's the sail"):
# strippa inledande "the " när possessivet bildas (fixar även fynd 11:s motorsida vid källan).
sub("const name=`${a.name}'s ${mat==='iron'&&id==='axe'?'iron ':''}${pick(S,t.var)}`;",
    "const name=`${a.name}'s ${mat==='iron'&&id==='axe'?'iron ':''}${pick(S,t.var).replace(/^the /,'')}`;",
    1, "F6-possessiv")

# FYND 7 — taboo-txt ("that …") kolliderar med "is now simply what one does": egen mall.
sub("ev(S,'tradition',`\U0001F3D8\uFE0F In ${v.name}, ${c.txt} is now simply what one does. <b>${c.name}</b> has become a tradition.`",
    "ev(S,'tradition',c.txt&&c.txt.startsWith('that ')?`\U0001F3D8\uFE0F In ${v.name}, the rule holds: ${c.txt.slice(5)}. <b>${c.name}</b> has become a tradition.`:`\U0001F3D8\uFE0F In ${v.name}, ${c.txt} is now simply what one does. <b>${c.name}</b> has become a tradition.`",
    1, "F7-taboo")

io.open(out_p, "w", encoding="utf-8", newline="\n").write(s)
print(f"OK {n0} ersättningar; ut-sha256 {hashlib.sha256(s.encode()).hexdigest()[:16]}")
