# Engie File Converter

Engie File Converter is a game formats file converter, mostly aimed towards DOS games from the late 1980s and early 1990s.

It was mostly made as a result of research into file formats for translation projects on [the OldGamesItalia forum](https://www.oldgamesitalia.net/forum/), and for completing and correcting info on [the Shikadi Modding Wiki](https://moddingwiki.shikadi.net/).

---

This project is released under [WTFPL](LICENSE.md), meaning any part of it can be used without any restriction. The INI format handler included in this project is likewise released under a very permissive license, though one that asks attribution.

However, several pieces of code used in this were taken from external sources, and those files will have their own license added at the top of the file.

* The LZW compression code used in this project was written by [Pedro Villarreal](https://github.com/pevillarreal). That project can be found [here](https://github.com/pevillarreal/LzwCompressor).

* The LZHUF compression algorithm implementation is based on lzhuf.c by Haruyasu Yoshizaki (1988), with comments translated by Haruhiko Okumura and subsequent modifications by Paul Edwards.

  Source: https://github.com/pzgnss/snippets/blob/master/lzhuf.c

  The original source file contains no explicit license or copyright notice. The licensing status of this particular version is therefore unclear.

* The Dynamix LZW decompression code was converted from [the C++ code of VOGONS.org user tikalat's "midi tools v4"](https://www.vogons.org/viewtopic.php?p=273448#p273448). It contains no license but I assume the fact he included the code implies that he doesn't mind it being useful to more people. The license on the attachment on the forum says "Fair use/fair dealing exception"; not really a license meant for posting your own code under.

