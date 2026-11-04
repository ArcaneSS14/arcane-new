# Arcane-Start
"""Offline boundary checks; never send messages to a live webhook."""
import importlib
from pathlib import Path
import sys
import unittest
import tempfile
import json
import io
import os
import zipfile
from unittest.mock import Mock, MagicMock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
publisher = importlib.import_module("actions_changelogs_since_last_run")
from changelog.discord import render_entry, text_length, link_fallback
from changelog.delivery import DeliveryState, RejectedDelivery
from changelog import delivery


def response(status=200, data=None, links=None):
    result = Mock(status_code=status, links=links or {})
    result.json.return_value = data if data is not None else {"id": "123"}
    return result


def entry(**changes):
    return {"id": 1, "author": "Автор", "url": "https://github.com/ArcaneSS14/arcane-new/pull/42",
            "time": "2026-11-04T10:00:00+00:00", "changes": [{"type": "Fix", "message": "Исправлена ошибка."}], **changes}


class DiscordChangelogTest(unittest.TestCase):
    def test_russian_card_preserves_author_pr_and_experimental_label(self):
        card, = render_entry(entry(labels=["Intent: Experimental"]))
        text = "\n".join(c.get("content", "") for c in card["components"][0]["components"])
        for expected in ("Автор", "PR #42", "Исправлено", "Исправлена ошибка.", "Экспериментальные"):
            self.assertIn(expected, text)
        self.assertEqual(card["allowed_mentions"], {"parse": []})
        self.assertNotIn("content", card)
        self.assertNotIn("embeds", card)

    def test_long_unicode_changes_are_preserved_and_gallery_is_paginated(self):
        message = "Ж😀" * 6000
        media = [f"https://example.org/{i}.png" for i in range(23)]
        cards = list(render_entry(entry(changes=[{"type": "Add", "message": message}], media=media)))
        rendered = ""
        seen_media = []
        for card in cards:
            components = card["components"][0]["components"]
            text = "".join(c.get("content", "") for c in components)
            self.assertLessEqual(text_length(text), 4000)
            if components[2]["type"] == 10 and not components[2]["content"].startswith("-#"):
                rendered += components[2]["content"]
            for component in components:
                if component["type"] == 12:
                    self.assertLessEqual(len(component["items"]), 10)
                    seen_media.extend(item["media"]["url"] for item in component["items"])
        self.assertEqual(rendered, "### 🆕 Добавлено\n• " + message)
        self.assertEqual(seen_media, media)

    def test_media_fallback_keeps_all_links_and_text(self):
        links = ["https://example.org/" + "x" * 1850 + str(i) for i in range(10)]
        card, = render_entry(entry(media=links))
        fallbacks = link_fallback(card)
        text = "\n".join(c.get("content", "") for p in fallbacks for c in p["components"][0]["components"])
        self.assertIn("Исправлена ошибка.", text)
        for link in links:
            self.assertIn(link, text)
        for p in fallbacks:
            self.assertLessEqual(sum(text_length(c.get("content", "")) for c in p["components"][0]["components"]), 4000)

    def test_failed_and_foreign_branch_runs_are_not_publication_baselines(self):
        current = {"id": 9, "created_at": "2026-11-04", "head_branch": "master", "workflow_url": "https://api.github.com/workflows/1"}
        def run(id, conclusion="success", branch="master"):
            return {"id": id, "status": "completed", "conclusion": conclusion, "head_branch": branch, "created_at": f"2026-11-0{id}"}
        session = Mock()
        session.get.side_effect = [response(data=current),
            response(data={"workflow_runs": [run(3, "failure"), run(2, branch="testing")]}, links={"next": {"url": "https://api.github.com/page2"}}),
            response(data={"workflow_runs": [run(1)]})]
        self.assertEqual(publisher.get_most_recent_workflow(session, "ArcaneSS14/arcane-new", "9")["id"], 1)

    def test_journal_absent_before_migration_has_no_entries(self):
        session = Mock()
        session.get.return_value = response(404)
        self.assertEqual(publisher.get_last_changelog_by_sha(session, "old-sha", "ArcaneSS14/arcane-new"), "Entries: []")

    @patch.object(publisher, "DISCORD_WEBHOOK_URL", "https://discord.com/api/webhooks/123/test-token?thread_id=456")
    def test_rejected_gallery_falls_back_and_preserves_thread(self):
        card, = render_entry(entry(media=["https://example.org/a.png"]))
        session = Mock()
        session.post.side_effect = [response(400), response(), response()]
        with tempfile.TemporaryDirectory() as directory:
            DeliveryState(Path(directory) / "delivery.json").deliver(card, lambda p: publisher.send_card(session, p))
        self.assertEqual(session.post.call_count, 3)
        target = session.post.call_args.args[0]
        self.assertIn("thread_id=456", target)
        self.assertIn("wait=true", target)
        self.assertIn("with_components=true", target)

    @patch.object(publisher, "DISCORD_WEBHOOK_URL", "https://discord.com/api/webhooks/123/test-token")
    def test_timeout_is_not_retried_and_does_not_expose_webhook(self):
        session = Mock()
        session.post.side_effect = publisher.requests.Timeout("test-token")
        with self.assertRaises(RuntimeError) as result:
            publisher.send_card(session, next(render_entry(entry())))
        self.assertEqual(session.post.call_count, 1)
        self.assertNotIn("test-token", str(result.exception))

    @patch.object(publisher, "DISCORD_WEBHOOK_URL", "https://discord.com/api/webhooks/123/test-token")
    @patch.object(publisher.time, "sleep")
    def test_explicit_rate_limit_is_retried(self, sleep):
        session = Mock()
        session.post.side_effect = [response(429, {"retry_after": 0.1}), response()]
        publisher.send_card(session, next(render_entry(entry())))
        self.assertEqual(session.post.call_count, 2)
        sleep.assert_called_once_with(0.1)

    def test_confirmed_delivery_is_skipped_after_restoring_receipts(self):
        payload = next(render_entry(entry()))
        send = Mock(return_value=None)
        with tempfile.TemporaryDirectory() as directory:
            filename = Path(directory) / "delivery.json"
            first = DeliveryState(filename)
            first.deliver(payload, send)
            restored = DeliveryState(filename)
            restored.absorb(json.loads(filename.read_text()))
            restored.deliver(payload, send)
            self.assertEqual(send.call_count, 1)

    def test_uncertain_delivery_is_not_blindly_replayed(self):
        payload = next(render_entry(entry()))
        send = Mock(side_effect=RuntimeError("connection lost after POST"))
        with tempfile.TemporaryDirectory() as directory:
            filename = Path(directory) / "delivery.json"
            state = DeliveryState(filename)
            with self.assertRaises(RuntimeError):
                state.deliver(payload, send)
            restored = DeliveryState(filename)
            restored.absorb(json.loads(filename.read_text()))
            with self.assertRaisesRegex(RuntimeError, "uncertain"):
                restored.deliver(payload, send)
            self.assertEqual(send.call_count, 1)

    def test_partial_fallback_resumes_without_repeating_confirmed_messages(self):
        payload = next(render_entry(entry(media=["https://example.org/a.png"])))
        fallbacks = link_fallback(payload)
        send = Mock(side_effect=[fallbacks, None, RejectedDelivery("HTTP 429")])
        with tempfile.TemporaryDirectory() as directory:
            filename = Path(directory) / "delivery.json"
            state = DeliveryState(filename)
            with self.assertRaises(RejectedDelivery):
                state.deliver(payload, send)
            restored = DeliveryState(filename)
            restored.absorb(json.loads(filename.read_text()))
            replay = Mock(return_value={"message_id": "456"})
            restored.deliver(payload, replay)
            self.assertEqual(replay.call_count, 1)
            self.assertEqual(replay.call_args.args[0], fallbacks[1])

    def test_published_diff_uses_pr_identity_instead_of_reassigned_ids(self):
        old = {"Entries": [entry(id=1)]}
        current = {"Entries": [entry(id=50), entry(id=51, url="https://github.com/ArcaneSS14/arcane-new/pull/43")]}
        self.assertEqual([e["id"] for e in publisher.diff_changelog(old, current)], [51])

    def test_rerun_of_an_older_build_cannot_reset_the_delivered_history(self):
        previous = {"Entries": [entry(), entry(id=2, url="https://github.com/ArcaneSS14/arcane-new/pull/43")]}
        with self.assertRaisesRegex(RuntimeError, "previously published PRs"):
            publisher.diff_changelog(previous, {"Entries": [entry()]})

    def test_old_run_rerun_uses_current_attempt_time_for_baseline(self):
        current = {"id": 1, "created_at": "2026-10-01", "run_started_at": "2026-11-04", "head_branch": "master", "workflow_url": "https://api.github.com/workflows/1"}
        previous = {"id": 2, "created_at": "2026-11-02", "updated_at": "2026-11-03", "status": "completed", "conclusion": "success", "head_branch": "master"}
        session = Mock()
        session.get.side_effect = [response(data=current), response(data={"workflow_runs": [previous]})]
        self.assertEqual(publisher.get_most_recent_workflow(session, "ArcaneSS14/arcane-new", "1")["id"], 2)

    def test_artifact_reads_only_the_expected_snapshot(self):
        stream = io.BytesIO()
        with zipfile.ZipFile(stream, "w") as archive:
            archive.writestr("ArcaneChangelog.yml", "Entries: []")
            archive.writestr("../unexpected.txt", "never extracted")
        downloaded = MagicMock(status_code=200)
        downloaded.__enter__.return_value = downloaded
        downloaded.iter_content.return_value = [stream.getvalue()]
        session = Mock()
        session.get.return_value = downloaded
        self.assertEqual(delivery.read_artifact(session, {"archive_download_url": "https://api.github.com/archive"}, "ArcaneChangelog.yml"), "Entries: []")

    @patch.dict(os.environ, {"GITHUB_REPOSITORY": "ArcaneSS14/arcane-new", "GITHUB_RUN_ID": "2", "GITHUB_TOKEN": "test"}, clear=True)
    def test_baseline_comes_from_the_packaged_artifact_not_git_yaml(self):
        current = {"id": 2, "created_at": "2026-11-04", "head_branch": "master"}
        previous = {"id": 1, "created_at": "2026-11-03", "conclusion": "success"}
        with patch.object(publisher, "get_current_run", return_value=current), \
             patch.object(publisher, "get_past_runs", return_value=iter([previous])), \
             patch.object(publisher, "artifacts", return_value=[{"name": "published-changelog"}]), \
             patch.object(publisher, "read_artifact", return_value="Entries: []"), \
             patch.object(publisher, "get_last_changelog_by_sha") as git_yaml:
            self.assertEqual(publisher.get_last_changelog(), "Entries: []")
            git_yaml.assert_not_called()

    def test_older_receipt_cannot_hide_a_missing_later_attempt(self):
        run = {"id": 5, "run_attempt": 2, "status": "completed"}
        saved = [{"name": "changelog-delivery-1", "created_at": "2026-11-01"}]
        session = Mock()
        session.get.return_value = response(data={"jobs": [{"steps": [{"name": "Publish changelog (Discord)", "conclusion": "failure"}]}]})
        with tempfile.TemporaryDirectory() as directory, \
             patch.object(delivery, "artifacts", return_value=saved), \
             patch.object(delivery, "read_artifact", return_value='{"prepared": true, "confirmed": ["old"], "pending": []}'):
            state = DeliveryState(Path(directory) / "delivery.json")
            with self.assertRaisesRegex(RuntimeError, "no receipt"):
                delivery.restore_receipts(session, "https://api.github.com", "ArcaneSS14/arcane-new", [run], state)

    def test_preflight_failure_preserves_the_previous_attempts_confirmations(self):
        run = {"id": 5, "run_attempt": 2, "status": "completed"}
        saved = [{"name": f"changelog-delivery-{i}", "created_at": f"2026-11-0{i}"} for i in (1, 2)]
        with tempfile.TemporaryDirectory() as directory, \
             patch.object(delivery, "artifacts", return_value=saved), \
             patch.object(delivery, "read_artifact", side_effect=['{"prepared": false}', '{"prepared": true, "confirmed": ["old"]}']):
            state = DeliveryState(Path(directory) / "delivery.json")
            session = Mock()
            delivery.restore_receipts(session, "https://api.github.com", "ArcaneSS14/arcane-new", [run], state)
            self.assertIn("old", state.confirmed)
            session.get.assert_not_called()

    def test_new_preflight_receipt_does_not_hide_an_older_attempt_without_receipts(self):
        run = {"id": 5, "run_attempt": 2, "status": "completed"}
        saved = [{"name": "changelog-delivery-2", "created_at": "2026-11-02"}]
        session = Mock()
        session.get.return_value = response(data={"jobs": [{"steps": [{"name": "Publish changelog (Discord)", "conclusion": "failure"}]}]})
        with tempfile.TemporaryDirectory() as directory, \
             patch.object(delivery, "artifacts", return_value=saved), \
             patch.object(delivery, "read_artifact", return_value='{"prepared": false}'):
            with self.assertRaisesRegex(RuntimeError, "attempt 1"):
                delivery.restore_receipts(session, "https://api.github.com", "ArcaneSS14/arcane-new", [run], DeliveryState(Path(directory) / "state.json"))

    def test_restored_operator_resolution_does_not_resurrect_an_old_uncertain_send(self):
        runs = [{"id": i, "run_attempt": 1, "status": "completed", "updated_at": f"2026-11-0{i}"} for i in (1, 2)]
        with tempfile.TemporaryDirectory() as directory, \
             patch.object(delivery, "artifacts", return_value=[{"name": "changelog-delivery-1", "created_at": "2026-11-01"}]), \
             patch.object(delivery, "read_artifact", side_effect=[
                 '{"prepared": true, "confirmed": ["delivered"], "pending": ["uncertain"]}',
                 '{"prepared": true, "confirmed": ["delivered"], "pending": []}']):
            state = DeliveryState(Path(directory) / "state.json")
            delivery.restore_receipts(Mock(), "https://api.github.com", "ArcaneSS14/arcane-new", runs, state)
            self.assertEqual(state.confirmed, {"delivered"})
            self.assertFalse(state.pending)


if __name__ == "__main__":
    unittest.main()
# Arcane-End
