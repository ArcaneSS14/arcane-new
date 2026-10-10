---
name: audio
description: Add data-driven audio with correct prediction, audience, resources, attribution, and verification.
---

# Audio

Classify the sound before selecting an API: predicted local feedback, PVS world sound, moving entity source, static coordinate source, global notification, client-only UI sound, ambient loop, or music.

Prefer component fields, prototypes, `SoundSpecifier`, and sound collections over hardcoded paths. Keep client-only playback out of Shared dependencies.

Predicted actions must not play the same sound locally and again on authoritative confirmation. Verify source deletion, cancellation, range, repetition, and concurrent playback.

Add `en-US` when the audio feature introduces player-visible captions, UI labels, announcements, examine text, or settings. Add `ru-RU` only when explicitly requested.

Preserve source attribution and asset-specific license metadata without editing SPDX.

## Verification commands

```powershell
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
git diff --check
```

Run the owning integration tests for predicted or networked playback behavior.
