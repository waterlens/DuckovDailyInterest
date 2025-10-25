#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: $0 <path_to_tsv_file>"
  exit 1
fi

awk -F'\t' 'NR > 1 {print $4}' "$1" | paste -sd ';' -
