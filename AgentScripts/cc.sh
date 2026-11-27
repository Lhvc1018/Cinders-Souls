#!/bin/bash
# usage: cc.sh [method]  -> refresh, wait compile, optionally run method
B=$HOME/mnt/Last67Seconds-Maps/AgentScripts/_bridge
echo refresh > $B/request.txt; sleep 12
for i in $(seq 1 40); do sleep 4; grep -q compiling $B/alive.txt || break; done
sleep 12; echo "--compile:"; cat $B/compile.txt; echo
if [ -n "$1" ]; then echo "$1" > $B/request.txt; for i in $(seq 1 30); do sleep 3; grep -q "> $1" $B/response.txt && break; done; sleep 2; cat $B/response.txt; fi
