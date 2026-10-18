# Security policy

This is a community-maintained Windows application, without a guaranteed response time or long-term support commitment. Identify the affected version and whether the latest public release reproduces the issue.

## Reporting

If private vulnerability reporting is enabled in this repository's Security tab, use it. Otherwise, open an issue requesting a private channel without posting the exploit or sensitive files. The maintainer must enable and verify a channel before advertising it as available.

Include a minimal synthetic reproduction and potential impact. Never publish credentials, private profiles or full personal PGNs.

## Trust boundaries

PGNs, openings and customization packs are untrusted input. Engine executables are native programs: use trusted builds. The CHESS_TRAINER_STOCKFISH override can run a different engine and must not point at an untrusted download.

Release ZIPs are not code-signed. SHA-256 checksums detect modification relative to the published checksum file, but are not independent publisher signatures.

The app needs no cloud credentials. Local data is not encrypted by the app; protect your Windows account and backups. Personal profiles, tokens, games and working directories must not enter public artifacts.
