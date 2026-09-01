import io,hashlib
s=io.open('emergence-presentation.js',encoding='utf-8').read()
old_tag=s[s.index('  function tagFor(a, S) {'):s.index('  function disambiguate(')]
new_tag='''  function tagFor(a, S) {
    // v0.6 (D-648): epithet > home village > age. Never a birth year (a.born is not a year for later-born souls — "born 0").
    if (a.epithet) return a.name + ' ' + a.epithet;
    var v = null;
    if (a._vil && a._vil.name) v = a._vil;
    else if (a.village !== undefined && S.villages) {
      for (var i = 0; i < S.villages.length; i++) if (S.villages[i] && S.villages[i].id === a.village) { v = S.villages[i]; break; }
    }
    if (v && v.name) return a.name + ' of ' + v.name;
    if (a.age !== undefined) return a.name + ', aged ' + Math.floor(a.age);
    return a.name + ' #' + a.id;
  }
'''
s=s.replace(old_tag,new_tag)
old_fs='''  function firstSentence(s) {
    s = stripHtml(s).trim();
    // drop leading emoji/symbol run
    s = s.replace(/^[^A-Za-z0-9"']+/, '');
    var m = s.match(/^(.+?[.!?])(\\s|$)/);
    return (m ? m[1] : s).trim();
  }'''
new_fs='''  // v0.6 (D-648): some engine lines carry their meaning in the SECOND sentence ("X is gone. But someone still …",
  // "… until the end. No one else ever took it up.") — those types keep two sentences.
  var SENTENCES = { legend: 2, customLost: 2 };
  function firstSentence(s, n) {
    n = n || 1;
    s = stripHtml(s).trim();
    // drop leading emoji/symbol run
    s = s.replace(/^[^A-Za-z0-9"']+/, '');
    var out = '', rest = s;
    for (var k = 0; k < n; k++) {
      var m = rest.match(/^(.+?[.!?])(\\s+|$)([\\s\\S]*)$/);
      if (!m) { out += rest; rest = ''; break; }
      out += (k ? ' ' : '') + m[1]; rest = m[3];
      if (!rest) break;
    }
    return out.trim();
  }'''
assert s.count(old_fs)==1, 'fs'
s=s.replace(old_fs,new_fs)
old_rl="    return disambiguate(firstSentence(ev.txt), ev, idx, S);"
new_rl="    return disambiguate(firstSentence(ev.txt, SENTENCES[ev.type] || 1), ev, idx, S);"
assert s.count(old_rl)==1; s=s.replace(old_rl,new_rl)
old_w1="      var w = WEIGHT[e.type] === undefined ? 1 : WEIGHT[e.type];\n      if (w <= 0) continue;"
new_w1="      var w = weightOf(e, S);\n      if (w <= 0) continue;"
assert s.count(old_w1)==1; s=s.replace(old_w1,new_w1)
old_cmp="  function cmpEvent(a, b) { // weight desc, then year asc, then id asc — total order, no ties\n    var wa = WEIGHT[a.type] === undefined ? 1 : WEIGHT[a.type];\n    var wb = WEIGHT[b.type] === undefined ? 1 : WEIGHT[b.type];"
new_cmp='''  // v0.6 (D-648): a craft lost in ONE village while the world still knows it is local news (20);
  // the world's last knowledge dying ("With Embla died the last knowledge of …") keeps 45.
  var _wS = null;
  function weightOf(e, S) {
    var w = WEIGHT[e.type] === undefined ? 1 : WEIGHT[e.type];
    if (e.type === 'knowledgeLost' && e.village && e.tech && S && S.knowledge && S.knowledge[e.tech] && S.knowledge[e.tech].status === 'alive') w = 20;
    return w;
  }
  function cmpEvent(a, b) { // weight desc, then year asc, then id asc — total order, no ties
    var wa = weightOf(a, _wS);
    var wb = weightOf(b, _wS);'''
assert s.count(old_cmp)==1; s=s.replace(old_cmp,new_cmp)
old_sort="    pool.sort(cmpEvent);"
new_sort="    _wS = S; pool.sort(cmpEvent); _wS = null;"
assert s.count(old_sort)==1; s=s.replace(old_sort,new_sort)
old_line="      lines.push({ year: ev.year, type: ev.type, weight: WEIGHT[ev.type] === undefined ? 1 : WEIGHT[ev.type], text: text, why: whyChain(ev, evIdx, idx, S, 0) });"
new_line="      lines.push({ year: ev.year, type: ev.type, weight: weightOf(ev, S), text: text, why: whyChain(ev, evIdx, idx, S, 0) });"
assert s.count(old_line)==1; s=s.replace(old_line,new_line)
s=s.replace("v0.5, 2026-08-31, D-605/D-613/D-615/D-626","v0.6, 2026-09-01, D-605/D-613/D-615/D-626/D-648")
s=s.replace("VERSION: '0.5.0'","VERSION: '0.6.0'")
io.open('emergence-presentation-v06.js','w',encoding='utf-8',newline='').write(s)
h=hashlib.sha256(s.encode('utf-8')).hexdigest(); io.open('PRESENTATION-SHA.txt','w').write(h); print(h)
