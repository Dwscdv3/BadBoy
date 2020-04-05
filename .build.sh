#!/bin/bash

TITLE=badapple

mkdir -p bin
mkdir -p obj

rgbasm -o obj/main.o main.asm
rgblink -d -n bin/$TITLE.sym -o bin/$TITLE.gb obj/main.o
rgbfix -v -m 0x19 -p 0 -j -k DW -t $TITLE bin/$TITLE.gb
