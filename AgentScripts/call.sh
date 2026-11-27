#!/bin/bash
# usage: call.sh Method [arg] [waitSeconds]
B=$HOME/mnt/Last67Seconds-Maps/AgentScripts/_bridge
[ -n "$2" ] && printf "%s" "$2" > $B/arg.txt
echo "$1" > $B/request.txt
for i in $(seq 1 ${3:-40}); do sleep 2; grep -q "> $1" $B/response.txt 2>/dev/null && ! grep -q "started async" $B/response.txt && break; done
cat $B/response.txt; echo
