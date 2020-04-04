# Bad Boy (Bad Apple!! on Game Boy)

Every screen has its own Bad Apple!!

## Introduction

**Bad Apple!!** is a well-known music video in the field of retrocomputing. It has been ported to a variety of legacy hardware by many people. It's just like the "Hello, world!" but much complicated than a "hello, world!", specifically on 8-bit era hardware.

## How to Play

You can download the most recent ROM from [Releases](http://github.com/Dwscdv3/BadBoy/releases) page.

I assume you know how to deal with a ROM file.

## About Optimizing

I tried my best to optimize its performance. Now it can play the video at full 30 fps and 160x120 resolution (not 160x144 because the original video is 4:3), with just a few seconds of neglectable lag and glitch around the scene of Flandre Scarlet.

### Tile Deduplicating

First I wrote a simple and ugly C# program to pack a bunch of images to a binary file. It analyzes every image, to find out several most occurred tile patterns between all of the images (called **common tiles** hereafter), then places them to a specific area in VRAM. Only the tiles not covered by these common tiles are stored separately for each frame. After frame tileset, there is a 20x15 fixed size BG map, each byte contains a tile index shared by both common tiles and frame specific tiles.

This step has greatly reduced the size to 23.5% of the raw data, and because of the reduced size and zero overhead, it is also a speed boost for rendering. Though it introduced some new problems. (See chapter **Double Buffering**.)

### Loop Unrolling

This step has a significant performance gain. After unrolled all the critical loops inside the rendering procedure, VRAM copy runs about 1.8x faster than plain loops before.

Counting the cycle during H-Blank can be very helpful. If you write immediately after an H-Blank period starts, you can always write 8 ~ 12 bytes (depending on various conditions) safely before VRAM becomes inaccessible again, without the need to check the accessibility before each writes. This is another huge performance increase, almost doubled the copying speed.

### Double Buffering

Because of tile deduplicating, the images cannot be rendered linearly anymore. Therefore, if an image cannot finish drawing in a single frame, the inconsistency state of the tileset and BG map will make the graphics look glitched. So I tried to utilize the alternate 0 ~ 127 tiles area ($9000 ~ $97FF) and alternate BG map area ($9C00 ~ $9FFF) for every second frame. The screen can be switched to render using data from either the main area or the alternate area. After a whole image is rendered, switch the screen.

Then, almost all of the glitches have gone, except for only a few frames contained more than 128 frame specific tiles (mentioned before, that little vampire), that additional part must be written into a global area shared by both buffers.

(By the way, the existence of these switchable alternate area is interesting. Did Nintendo intentionally design this for double buffering? Or was it just a coincidence?)

## Note

Audio isn't included since there is no more space left for it, and it will be extremely difficult to play every note in time without degrading the video rendering performance.

I'm not expected anyone would fork or build this project, so resources are excluded from this repository. (It's over 40 MB.) If you actually need it, just ask me by E-mail, or extract all frames from the video, and use some tools such as Photoshop to reduce these images to 4 colors. (My image converter uses grayscale 0, 85, 170, 255.)

## Credit

Ported by Dwscdv3

[**Bad Apple!!** PV made by **あにら さん**](https://www.nicovideo.jp/watch/sm8628149)
