GameBoyImageConverter
=====================

A tool to pack a bunch of images as a binary file.

This tool is focused on decompression speed, therefore, the only method taken to reduce data size is removing duplicated tiles by throwing it to a global dictionary.

This is mostly a tool made for my own sake, so the code is somewhat ugly. However, it may still be helpful to you.

You will need to build it with Visual Studio 2017 or above. Configurations (`DictionarySize` and `FrameSkip`) are directly embedded into the source code `Program.cs` for now.
