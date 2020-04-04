using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static System.Console;

namespace GameBoyImageConverter
{
    class Program
    {
        const int TilesPerFrame = 300;
        const int TilesPerRow = 20;
        const int TilesPerColumn = 15;

        const byte DictionarySize = 108;
        const int FrameSkip = 0;

        static void Main(string[] args)
        {
            var files = Directory.GetFiles(".", "*.png");
            var totalFrames = (int)Math.Ceiling((double)files.Length / (FrameSkip + 1));
            var dict = new Dictionary<byte[], int>(totalFrames * TilesPerFrame, new ByteArrayComparer());
            var frameData = new byte[totalFrames, TilesPerFrame][];

            for (var i = 0; i < files.Length; i += FrameSkip + 1)
            {
                var pinnedBitmap = new DirectBitmap(160, 120);
                using (var g = Graphics.FromImage(pinnedBitmap.Bitmap))
                {
                    var img = new Bitmap($"{i}.png");
                    g.DrawImage(img, 0, 0);
                    img.Dispose();
                    for (var tileY = 0; tileY < TilesPerColumn; tileY++)
                    {
                        for (var tileX = 0; tileX < TilesPerRow; tileX++)
                        {
                            var tileData = new byte[16];
                            for (var j = 0; j < 64; j++)
                            {
                                var colorIndex = 3 - pinnedBitmap.GetPixel(
                                    (tileX << 3) + (j & 7),
                                    (tileY << 3) + (j >> 3)
                                ).R / 85;
                                if ((colorIndex & 1) > 0)
                                    tileData[(j >> 3) << 1] |= (byte)(1 << (7 - (j & 7)));
                                if ((colorIndex & 2) > 0)
                                    tileData[((j >> 3) << 1) + 1] |= (byte)(1 << (7 - (j & 7)));
                            }
                            if (dict.ContainsKey(tileData))
                                dict[tileData]++;
                            else
                                dict.Add(tileData, 1);
                            frameData[i / (FrameSkip + 1), tileY * TilesPerRow + tileX] = tileData;
                        }
                    }
                }

                Write($"\rConverting images to tiles... {i / (FrameSkip + 1) + 1} / {totalFrames}");
            }
            WriteLine();

            var mostFrequentTiles = dict.OrderByDescending(pair => pair.Value).Take(DictionarySize);
            WriteLine($"Finding {DictionarySize} most frequent tiles...");
            File.WriteAllText("MostFrequentTiles.log", "");
            foreach (var pair in mostFrequentTiles)
            {
                File.AppendAllText(
                    "MostFrequentTiles.log",
                    $"{pair.Value,6}\t{string.Join(" ", pair.Key.Select(b => b.ToString("X2")))}\r\n");
            }
            WriteLine("Logged to `MostFrequentTiles.log`.");
            var mostFrequentTilesDict = mostFrequentTiles
                .Select((pair, index) => (pair.Key, index))
                .ToDictionary(tuple => tuple.Key, tuple => (byte)tuple.index, new ByteArrayComparer());

            File.WriteAllText("frame.map", "");
            var stream = new BinaryWriter(new FileStream("resources.bin", FileMode.Create));
            stream.Write(DictionarySize);
            foreach (var pair in mostFrequentTiles)
            {
                stream.Write(pair.Key);
            }
            long lastFramePos = 0;
            for (var frameIndex = 0; frameIndex < totalFrames; frameIndex++)
            {
                byte frameSpecificTiles = 0;
                var tileSet = new byte[16 * TilesPerFrame];
                var bgMap = new byte[TilesPerFrame];
                for (var tileIndex = 0; tileIndex < TilesPerFrame; tileIndex++)
                {
                    var tile = frameData[frameIndex, tileIndex];
                    if (mostFrequentTilesDict.ContainsKey(tile))
                    {
                        bgMap[tileIndex] = (byte)(256 - DictionarySize + mostFrequentTilesDict[tile]);
                    }
                    else
                    {
                        for (var i = 0; i < 16; i++)
                        {
                            tileSet[16 * frameSpecificTiles + i] = tile[i];
                        }
                        bgMap[tileIndex] = frameSpecificTiles;
                        frameSpecificTiles++;
                    }
                }
                Array.Resize(ref tileSet, 16 * frameSpecificTiles);
                var bankRemainingSpace = 0x4000 - stream.BaseStream.Position % 0x4000;
                var frameSize = tileSet.Length + bgMap.Length + 1;
                if (bankRemainingSpace < frameSize)
                {
                    stream.Write((byte)0xFE);
                    stream.Write(new byte[bankRemainingSpace - 1]);
                }
                File.AppendAllText(
                    "frame.map",
                    $"Frame{frameIndex.ToString().PadRight(4)} EQU ${stream.BaseStream.Position:X6} ; {frameSize}\r\n");
                stream.Write(frameSpecificTiles);
                stream.Write(tileSet);
                stream.Write(bgMap);
                lastFramePos = stream.BaseStream.Position;
                Write($"\rWriting binary... {frameIndex + 1} / {totalFrames}");
            }
            stream.Write((byte)0xFF);
            stream.Close();
            WriteLine();
            WriteLine("Binary saved to `resources.bin`.");
            WriteLine("Frame offset saved to `frame.map`.");

            WriteLine("Done.");
            WriteLine();

            var maxUniqueTiles = 0;
            var maxUniqueTilesFrame = -1;
            for (var i = 0; i < totalFrames; i++)
            {
                var uniqueTiles = 0;
                for (var j = 0; j < TilesPerFrame; j++)
                {
                    if (!mostFrequentTilesDict.ContainsKey(frameData[i, j]))
                        uniqueTiles++;
                }
                if (uniqueTiles > maxUniqueTiles)
                {
                    maxUniqueTiles = uniqueTiles;
                    maxUniqueTilesFrame = i * (FrameSkip + 1);
                }
            }
            WriteLine($"Frame with the most unique tiles: {maxUniqueTilesFrame}, {maxUniqueTiles} tiles");
            WriteLine($"Total tile variants: {dict.Count}");
            var totalUniqueTiles = dict.Aggregate(0, (total, next) => total + (mostFrequentTilesDict.ContainsKey(next.Key) ? 0 : next.Value));
            WriteLine($"Total unique tiles: {totalUniqueTiles} out of {totalFrames * TilesPerFrame}");
            WriteLine($"File size: {lastFramePos / 1024.0:F0} KiB");
            WriteLine($"Compression ratio: {(double)lastFramePos * 100 / (5101 * totalFrames + 16 * DictionarySize + 1):F1}%");

            WriteLine();
            WriteLine("Press any key to continue...");
            ReadKey(true);
        }
    }
}
