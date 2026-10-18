# Languages, board styles, UI themes and sounds

Customization files live beside the executable. Select them in Settings; restart after adding or editing files. Preferences are stored separately in the local user profile.

## Languages

languages.json contains a languages list and rows with de/en columns. The German text is the stable key. Add a language ID/name and a corresponding column to translated rows; preserve numbered placeholders such as {0}. Untranslated strings fall back to German. User-authored repertoire names and annotations are not automatically translated. Number formatting follows Windows locale.

## Board styles

Copy skins/classic.json to a new JSON file with a unique id/name. Colors use #RRGGBB. pieceStyle can be silhouette or rounded; slate.json provides the Slate & Ivory example.

Optional pieces maps wp, wn, wb, wr, wq, wk, bp, bn, bb, br, bq, bk to PNG paths relative to the skin file. Use transparent square images, preferably at least 256 pixels. Missing mappings use vector silhouettes. Paths must remain within the skin directory.

## UI themes and symbols

See ui-themes/README.md and icons/README.md. Board colors and application colors are independent. Settings also control animation duration, marking colors, arrow thickness and arrowhead size.

## Sound packs

The source edition contains project-synthesized legacy sounds. The complete Windows edition may additionally contain the separately licensed user pack; see the release's THIRD_PARTY_NOTICES.md. Do not redistribute that pack as a standalone sound collection.

Create sounds/my-pack/pack.json with a unique id/name and files mapping events to arrays of local filenames:

    {
      "id": "my-pack",
      "name": "My sounds",
      "files": {
        "light": ["pawn.wav"],
        "heavy": ["piece.wav"],
        "check": ["check.wav"],
        "mate": ["mate.wav"],
        "success": ["correct.wav"],
        "error": ["wrong.wav"]
      }
    }

light is for pawns; heavy for other pieces. Check/mate take precedence. Optional capture provides a dedicated capture sound; otherwise existing placement behavior is retained. success/error are training feedback, not menu clicks. Legacy synthesis uses its own move/capture events.

Use MP3 or PCM-WAV; paths must remain inside the pack. Volume/mute apply globally. The app remains usable without an audio device. Document your own assets' licenses before redistribution.
