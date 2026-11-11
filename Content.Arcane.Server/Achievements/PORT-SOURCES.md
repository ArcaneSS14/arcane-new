# Achievement port sources

Adapted from Goob-Station/Goob-Station, revision `cc1d2c911f5cb6c90b36e0899ce918010bd41262`.

- [Achievements, #7373](https://github.com/Goob-Station/Goob-Station/pull/7373), Aiden / Aidenkrz, introductory commit `6b0b1cd395e1ae67c4d4d87309f7b6cdf08ca911`.
- [Achievement fixes, #7433](https://github.com/Goob-Station/Goob-Station/pull/7433), Aiden / Aidenkrz, commit `85356374cc30980579df6e2e44df90e7a2853824`.
- [Emote changes, #7464](https://github.com/Goob-Station/Goob-Station/pull/7464), Aidenkrz: the pinned achievement code incorporates removal of the obsolete emote-smite condition. The emote changes and removal of Trauma's punishment are outside this port.

The source code is licensed under AGPL-3.0-or-later, as stated in the source repository's README and license texts. The achievement client, shared protocol/categories, server conditions, selected prototype data and English text retain this provenance. Destination database integration, API adaptations and the limited spectral-hug path are Arcane adaptations by UmbiMax.

Existing destination icon and audio resources are reused with their existing attribution. No new image or audio assets are imported by this package.

The implementation uses the destination mind, role, body/organ, research, interaction, container and database owners. Spectral hugs keep the popup cooldown/chance/feedback and use a separate success event to avoid physical effects. The ordinary recycler retains Trauma limb damage; the suicide path also avoids whole-body gibbing.

The selected set excludes unsupported antagonist awards and holds awards whose required roles or Bible action are unavailable. Gambling's Jackpot award belongs to its feature package.
