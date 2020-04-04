INCLUDE "header.inc"



SECTION "INT40", ROM0[$40]
    jp VBlankInterrupt
    


SECTION "INT48", ROM0[$48]
    reti
    


SECTION "Header", ROM0[$100]
    jp Entry
    
    ds 77
    


SECTION "Code", ROM0

Entry:
.initRAM
    xor a
    ldh [FrameCount], a
    ldh [altBuffer], a
    ld a, 1
    ldh [bankIndex], a
.initROMBank
    ld [$2000], a
.configureInterrupts
    ld a, IEF_VBLANK
    ldh [rIE], a
    ld a, STATF_LYC
    ldh [rSTAT], a
    ld a, 0
    ldh [rLYC], a
    ei
.waitForVBlank
    halt
    nop
.displayOff
    ld a, LCDCF_OFF
    ldh [rLCDC], a
.initBGMap_0
    ld a, 256 - DICTIONARY_SIZE + 1
    ld b, 18
    ld c, 20
    ld de, 12
    ld hl, _SCRN0
.initBGMap_0_loop:
    ld [hl+], a
.initBGMap_0_step:
    dec c
    jr nz, .initBGMap_0_loop
    ld c, 20
    add hl, de
    dec b
    jr nz, .initBGMap_0_loop
.initBGMap_1
    ld a, 256 - DICTIONARY_SIZE + 1
    ld b, 18
    ld c, 20
    ld de, 12
    ld hl, _SCRN1
.initBGMap_1_loop:
    ld [hl+], a
.initBGMap_1_step:
    dec c
    jr nz, .initBGMap_1_loop
    ld c, 20
    add hl, de
    dec b
    jr nz, .initBGMap_1_loop
.initTileset
    ld de, _VRAM + 16 * (256 - DICTIONARY_SIZE)
    ld h, $40
    ld l, $00
    ld b, [hl]
    inc l
    inc b
    jr .initTileset_step
.initTileset_loop
    REPT 16
        ld a, [hl+]
        ld [de], a
        inc de
    ENDR
.initTileset_step
    dec b
    jr nz, .initTileset_loop
.configureDisplay
    ld a, LCDCF_ON | LCDCF_BGON | LCDCF_BG8000
    ldh [rLCDC], a
.useStandardPalette
    ld a, %11100100
    ldh [rBGP], a
.soundOff
    ld a, AUDENA_OFF
    ldh [rAUDENA], a
    
Main:
.waitForVBlank
    halt
    nop
.altBuffer
    ldh a, [altBuffer]
    cp 0
    jr nz, .altBuffer_1
.altBuffer_0
    ld a, LCDCF_ON | LCDCF_BGON | LCDCF_BG8000 | LCDCF_BG9800
    ldh [rLCDC], a
    jr .checkForNextFrame
.altBuffer_1
    ld a, LCDCF_ON | LCDCF_BGON | LCDCF_BG8800 | LCDCF_BG9C00
    ldh [rLCDC], a
.checkForNextFrame
    ldh a, [FrameCount]
    sub 60 / 30
    jr c, Main
    ldh [FrameCount], a
    ld a, IEF_LCDC
    ldh [rIE], a
.checkForBankSwitchAndEOF
    ld a, [hl]
    cp $FF
    jr z, Fin
    cp $FE
    jr nz, .waitForLYC
    ldh a, [bankIndex]
    add 1               ; `inc a` won't affect c flag
    jr nc, .noCarry
    cpl
    ld [$3000], a
    cpl
.noCarry
    ldh [bankIndex], a
    ld [$2000], a
    ld h, $40
    ld l, $00
.waitForLYC
    halt
    nop
    ld a, IEF_VBLANK
    ldh [rIE], a
    
    call Draw
    
    jr Main
    
Fin:
    halt
    nop
    jr Fin
    
Draw:
    ldh a, [altBuffer]
    cp 0
    jr z, .tileBuffer1
.tileBuffer0
    ld de, _VRAM
    jr .tileCopy
.tileBuffer1
    ld de, _VRAM + $1000
.tileCopy
    ld b, [hl]
    ld c, 128
    inc hl
    inc b
    inc c
    jr .tileCopy_step
.tileCopy_loop
    REPT 2
.passThisBlank_0\@
        ldh a, [rSTAT]
        and STATF_BUSY
        jr z, .passThisBlank_0\@
.wait_0\@
        ldh a, [rSTAT]
        and STATF_LCD
        jr nz, .wait_0\@
        REPT 8
            ld a, [hl+]
            ld [de], a
            inc de
        ENDR
    ENDR
.tileCopy_step
    dec c
    jr nz, .tileCopy_step_b
    ld de, $8800
.tileCopy_step_b
    dec b
    jr nz, .tileCopy_loop

    ldh a, [altBuffer]
    cp 0
    jr z, .bgMapBuffer1
.bgMapBuffer0
    ld de, _SCRN0
    jr .bgMapCopy
.bgMapBuffer1
    ld de, _SCRN1
.bgMapCopy
    REPT 15
        REPT 2
.passThisBlank_1\@
            ldh a, [rSTAT]
            and STATF_BUSY
            jr z, .passThisBlank_1\@
.wait_1\@
            ldh a, [rSTAT]
            and STATF_LCD
            jr nz, .wait_1\@
            REPT 10
                ld a, [hl+]
                ld [de], a
                inc de
            ENDR
        ENDR
        REPT 12
            inc de
        ENDR
    ENDR
    
    ldh a, [altBuffer]
    cpl
    ldh [altBuffer], a
    
    ret
    
VBlankInterrupt:
    push af
    ldh a, [FrameCount]
    inc a
    ldh [FrameCount], a
    pop af
    reti
    