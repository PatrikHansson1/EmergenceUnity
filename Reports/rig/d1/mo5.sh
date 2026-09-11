#!/bin/bash
cd /home/claude/gc23
for rep in 1 2 3 4 5; do
  for eng in engine-v22.js engine-v23.js; do
    for L in 97013 4242 20260718 97013-founders; do
      node golden-cloud.js $eng $L 8640 >> mo5-results.txt
    done
  done
done
echo "# DONE $(date -u +%FT%TZ)" >> mo5-results.txt
