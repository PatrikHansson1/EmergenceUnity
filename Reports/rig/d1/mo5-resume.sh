#!/bin/bash
# resumbar M-O5: fyll på till 5 rep per (motor,frö), i rep-ordning
cd /home/claude/gc23
for rep in 1 2 3 4 5; do
  for eng in engine-v22.js engine-v23.js; do
    for L in 97013 4242 20260718 97013-founders; do
      have=$(grep -c "\"engine\":\"$eng\",\"label\":\"$L\"," mo5-results.txt)
      if [ "$have" -lt "$rep" ]; then
        node golden-cloud.js $eng $L 8640 >> mo5-results.txt
      fi
    done
  done
done
tot=$(grep -c '"engine"' mo5-results.txt)
[ "$tot" -ge 40 ] && echo "# DONE $(date -u +%FT%TZ)" >> mo5-results.txt
