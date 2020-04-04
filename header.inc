INCLUDE "hardware.inc"
INCLUDE "resources.inc"



SECTION "Constants", ROM0

DICTIONARY_SIZE EQU 108



SECTION "SystemVariables", HRAM

FrameCount      db



SECTION "Variables", HRAM

bankIndex       db
altBuffer       db
