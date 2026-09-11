#!/bin/bash
cd /home/claude/g0
printf '%s\n' 97013 4242 20260718 31415 2323 1618 777 97013-founders | xargs -P 2 -I{} sh -c 'node g0probe.js {} 120 >> g0-run.log 2>&1'
echo "# DONE $(date -u +%FT%TZ)" >> g0-run.log
