@echo off
rgbasm -o obj/main.o main.asm
rgblink -d -n bin/badapple.sym -o bin/badapple.gb obj/main.o
rgbfix -v -m 0x19 -p 0 -j -k DW -t badapple bin/badapple.gb
