# Release licensing evidence and obligations

This is a practical release audit, not a legal opinion or a guarantee about unseen third-party material. Evidence was checked on 2026-10-18.

## Project license choice

Original application code and code-generated assets are released as GPL-3.0-or-later. LICENSE contains the complete GPL version 3 text; COPYRIGHT.md grants the “or later” option. The maintainer's raster assessment icon sheet is CC-BY-4.0, with attribution and its full license retained. This separate artwork license does not require an editable Figma source, which was not supplied. Other third-party terms remain explicit. The application communicates with Stockfish through UCI; choosing a GPL-compatible project license avoids making publication depend on an aggressive interpretation of that process boundary.

## Stockfish 19 verification

| Item | Recorded value |
| --- | --- |
| Official tag | sf_19 |
| Source commit | edb0d9db6731067ec50ce619ff372b463bc4dd5d |
| Official Windows universal ZIP SHA-256 | 3c8bf1f9ea66a09350a40df4f632288285ac206d99f33ab5842c408fc30b48a7 |
| Executable SHA-256 | 45bc8e4969147db9c2eb533810637994619bff0eacc81ccfd9854394901bcbd0 |
| Pinned source ZIP SHA-256 | a6368d704182ad976d73c8b8349119f6bf9e76737bdf609fd8e873346193dc98 |
| NNUE filename | nn-1a298aa575a0.nnue |
| NNUE SHA-256 | 1a298aa575a085434d29027978dc36867fe9c5bcea9376654b7a8eba1e52dfc2 |

The pre-existing engine binary matched the official release. All 82 local files under its src and scripts directories matched the pinned full source tree. The embedded network was exported using Stockfish's export_net command and hashed; no network training or engine changes were made.

The release companion includes the full pinned source tree (including upstream build workflows), network, app source and packaging scripts. Copy the network into Stockfish's src directory before compiling. Use the included upstream src/Makefile, scripts and .github/workflows/universal_compilation.yml for the universal build recipe; the upstream README/wiki explains platform toolchains. An ordinary Windows MinGW build can use make -j2 build ARCH=x86-64 COMP=mingw in src. Compiler/toolchain variations can change resulting bytes: no independently reproducible, bit-identical rebuild is claimed.

Sources:
- [Official release](https://github.com/official-stockfish/Stockfish/releases/tag/sf_19)
- [Pinned source](https://github.com/official-stockfish/Stockfish/tree/edb0d9db6731067ec50ce619ff372b463bc4dd5d)
- [GPL v3, including section 6](https://www.gnu.org/licenses/gpl-3.0.en.html)

Publish the companion alongside every matching binary, not only an upstream homepage link. Preserve notices and supply corresponding modified source if you change the engine or application. GitHub's automatic repository source ZIP is not the companion: downloaded engine/network files are deliberately untracked.

## Runtime components

The release pins Microsoft.NETCore.App.Runtime.win-x64 and Microsoft.WindowsDesktop.App.Runtime.win-x64 to 10.0.12. Their recorded product source revision is 95017c711e6afc1085133d440e42b4bd78155701 in the package version metadata. MIT license and third-party notices accompany the Windows package. WPF and WinForms notices are retained as additional component notices.

## Pixabay boundary

Attribution is not mandatory under the [license summary](https://pixabay.com/service/license-summary/), but source credits are retained for transparency. The [binding terms](https://pixabay.com/service/terms/) also restrict standalone redistribution; edits alone do not remove that restriction.

Accordingly, edited effects are included only as contextual audio in the complete application, with separate notices. They are excluded from the public source tree and are not offered as an independent audio archive. The source-only build remains functional using original synthesized sounds. The MP3 files are not covered by the GPL grant; this distinction must remain visible in future releases.

The maintainer supplied four original source links and confirmed Audacity edits. Original download certificates/dates were not available. Retain any such records privately if subsequently found. The review does not claim to independently establish every source uploader's rights.

## Changes made for publication

No engine replacement was necessary. Added exact-source/network verification, release companion generation, runtime notices, license boundaries, sanitized sample data and curated exports. Private profiles, historical build folders and user games are excluded rather than “cleaned” in place.
