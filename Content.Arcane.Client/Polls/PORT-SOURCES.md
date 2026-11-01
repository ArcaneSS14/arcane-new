# Polling booth and seen counts

Adapted from Goob [#6519](https://github.com/Goob-Station/Goob-Station/pull/6519), credited to Durk (GitHub PR author Aidenkrz). Final source revision: `45e8d6b1cb1d3464762c8a50ce2f7f5ddf6e59c1`. The adapted code, including the marked database and poll-manager integration, remains AGPL-3.0-or-later.

The polling booth sprite is by Leguia, licensed CC-BY-SA-3.0. Its original `meta.json` attribution is preserved in `Resources/Textures/_Arcane/Structures/Specific/polling.rsi`.

Trauma's poll manager, voting rules and `AdminFlags.Admin` administration are retained. Views are stored separately from votes, once per player/poll. Only active, started, unexpired polls in a list offered to the authenticated connection can be newly marked seen. Opening the window waits for the returned list and marks the cards added to that open window; background list requests and closed windows do not mark views.

## Administration

Use the existing server console or an account with Trauma's poll administration rights:

`createpoll "Title" "Description" 7 false "First choice" "Second choice"`

The duration is in days; `0` means no end date. The required boolean selects multiple choice. Existing commands `listpolls`, `pollinfo <id>` and `closepoll <id>` inspect or close polls. Players can use the existing `polls` client command or activate a polling booth.

The `PollBooth` entity is available in the admin entity-spawn menu and to mappers. This port does not place booths automatically on station maps. Activate one to open the existing poll window; the marker above it indicates an unseen available poll for that client.

The additive migrations create `poll_seen` and preserve existing polls/votes. Downgrading removes view history only; normal votes remain in their existing tables.

The integration tests cover SQLite and PostgreSQL upgrade/down/upgrade with pre-existing polls and votes. For the live PostgreSQL case, set `ARCANE_TEST_POSTGRES` to a fresh local database named `arcane_goob_poll_test_*` on `127.0.0.1:55432`; the test refuses other destinations. Without that explicit test target, this one provider case is skipped. SQL generation and the SQLite cases do not require an external server.
