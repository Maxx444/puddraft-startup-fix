# PUDDraft startup fix

A small patcher for **PUDDraft v0.96**, the classic WarCraft II map editor. It fixes this crash at startup:

> Access violation at address 00450E18 in module 'PUDDraft.exe'. Read of address 0000001C.

The crash happens when PUDDraft starts **maximized** (`maxed=1` in `PUDDRAFT.ini`). Before you open a map,
the map view tries to redraw itself and reads the map size from a map that doesn't exist yet.

This repo does **not** contain PUDDraft. You need your own copy of `PUDDraft.exe` v0.96.

### [⬇ Download the latest PUDDraft-StartupFix.exe](https://github.com/Maxx444/puddraft-startup-fix/releases/latest/download/PUDDraft-StartupFix.exe)

This link always gets the newest version. Older versions are on the [Releases](../../releases) page.

## How to use

1. [Download `PUDDraft-StartupFix.exe`](https://github.com/Maxx444/puddraft-startup-fix/releases/latest/download/PUDDraft-StartupFix.exe) (latest release).
2. Put it in the same folder as `PUDDraft.exe`.
3. Run it and click **Patch PUDDraft**.

That's it. Before changing anything, the patcher copies your original to `backup\PUDDraft.exe`.
To undo the patch, run it again and click **Revert to original**.

The window shows whether your `PUDDraft.exe` is the original, already patched, or a version it doesn't
know. It only patches the exact v0.96 file:

| File | Size | SHA-256 |
|---|---|---|
| original `PUDDraft.exe` v0.96 | 546,816 bytes | `526745751e70b142a0bac9138b47833d7aaf7b9e3ef9d1d242b3eeb4e1aa4ddb` |
| patched `PUDDraft.exe` | 546,816 bytes | `ebacd1b52ffe1be0ca5c758858b4aedff025474dd8cfd6c040f6ad9bc0639b74` |

It refuses any other file, and it checks the result after writing. If the check fails, it puts the
original back.

**Windows warning:** the patcher isn't signed, so Windows SmartScreen may say "Windows protected your PC".
Click **More info**, then **Run anyway**. If you'd rather not run a downloaded exe, build it yourself (below).

**Command line** (no window): `PUDDraft-StartupFix.exe --patch` or `--revert`. It writes
`PUDDraft-StartupFix.log` next to itself and exits with code 0 on success or 1 on failure.

## What the patch changes

34 bytes in three places:

| File offset | Original | New | Meaning |
|---|---|---|---|
| `0x200` | `24` | `50` | CODE section size `0x59424` → `0x59450`, so the new code below is inside the section |
| `0x50218` | `0f b7 40 1c 8b b3 a0 5a 01 00` | `e9 13 96 00 00 90 90 90 90 90` | jump to the new code (address `0x450E18` → `0x45A430`) |
| `0x59830` | 23 × `00` (unused padding) | `85 c0 0f 84 ab 6a ff ff 0f b7 40 1c 8b b3 a0 5a 01 00 e9 db 69 ff ff` | new code, below |

```
0x45A430  TEST EAX,EAX               ; EAX = current map (nil before a map is loaded)
          JZ   0x450EE3              ; no map -> leave the redraw routine normally
          MOVZX EAX, word [EAX+0x1C] ; original instruction (map width)
          MOV  ESI, [EBX+0x15AA0]    ; original instruction
          JMP  0x450E22              ; continue where the original code was
```

`0x450EE3` is the redraw routine's own exit. The original code already jumps there when there is nothing
to redraw, so "no map yet" now takes that same path. With a map loaded, PUDDraft runs exactly as before.

## Build it yourself

You don't need to install anything. Windows 10 and 11 come with the .NET Framework C# compiler. In this
repo's `src` folder, run:

```
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /win32manifest:app.manifest /out:PUDDraft-StartupFix.exe StartupFix.cs
```

The compiler stamps each build with a time and a random ID, so your exe won't match the released one byte for
byte. What it does is the same. `app.manifest` tells Windows the program doesn't need admin rights.
Without it, Windows may ask for admin, because the file name contains "Fix".

## Notes

- This is an unofficial fix, not made or endorsed by PUDDraft's author.
- Only the patcher is shared here. Please don't upload patched copies of `PUDDraft.exe`.
- Use at your own risk. The backup and the Revert button are there for that.
