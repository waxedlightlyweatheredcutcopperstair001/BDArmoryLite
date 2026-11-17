#!/bin/bash
# Build and summarise errors. Writes full list to $ERRF.
ERRF=/tmp/claude-1001/-home-gavin-code/e6d03006-86b9-4753-9930-6194ce50da46/scratchpad/err.txt
cd "$(dirname "$0")/.." && ./build.sh 2>&1 | grep -E ': error ' | sed -E 's|/home/gavin/code/BDArmoryLite/src/||; s/ \[.*//' | sort -u > $ERRF
echo "errors: $(wc -l < $ERRF)"
cut -d'(' -f1 $ERRF | sort | uniq -c | sort -rn | head -${1:-40}
echo "-- top missing names:"; grep -oE "name '[A-Za-z_]+'|'[A-Za-z_]+' does not contain a definition for '[A-Za-z_]+'" $ERRF | sort | uniq -c | sort -rn | head -${2:-40}
