#!/bin/bash
cd /home/claude/p1
for eng in /home/claude/text/engine-v24text.js /home/claude/gc23/engine-v23.js /home/claude/gc23/engine-v22.js; do
  printf '%s\n' 97013 4242 20260718 31415 2323 1618 777 97013-founders | xargs -P 2 -I{} sh -c "node fsprobe.js $eng {} >> fs-run.log 2>&1"
done
echo "# DONE $(date -u +%FT%TZ)" >> fs-run.log
