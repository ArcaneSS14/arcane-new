#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import itertools
import os
import sys
import urllib.parse
from pathlib import Path
from typing import Any, Iterable

import requests
import yaml

# Arcane-Start
import json
import time
import re
from changelog.discord import render_entry, link_fallback
from changelog.delivery import DeliveryState, RejectedDelivery, artifacts, read_artifact, restore_receipts
# Arcane-End

DEBUG = os.environ.get("SS14_CHANGELOG_DEBUG", "").lower() in {"1", "true", "yes"}
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
DEBUG_DISCORD_DUMP_FILE = Path("Resources/Changelog/DiscordDebug.md")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

# https://discord.com/developers/docs/resources/webhook
DISCORD_SPLIT_LIMIT = 2000
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")
TRUNCATION_SUFFIX = " [...]"

CHANGELOG_FILE = os.environ.get("CHANGELOG_FILE") or "Resources/Changelog/ArcaneChangelog.yml" # Arcane-Edit: Trauma journal > Arcane journal

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}

EXPERIMENTAL_LABEL = "Intent: Experimental"
EXPERIMENTAL_EMOJI = "🧪"

ChangelogEntry = dict[str, Any]


def main():
    if not DEBUG and not DISCORD_WEBHOOK_URL:
        # Arcane-Edit-Start: a skipped delivery must not become the next successful baseline.
        raise RuntimeError("CHANGELOG_DISCORD_WEBHOOK is not configured; changelog was not sent")
        # Arcane-Edit-End

    if DEBUG:
        # to debug this script locally, you can use
        # a separate local file as the old changelog
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text(encoding="utf-8-sig")
    else:
        # when running this normally in a GitHub actions workflow,
        # it will get the old changelog from the GitHub API
        # Arcane-Edit-Start: persist confirmed messages across failed runs and workflow retries.
        state = DeliveryState(os.environ["CHANGELOG_DELIVERY_STATE"])
        state.save()
        last_changelog_stream = get_last_changelog(state)
        for key in os.environ.get("CHANGELOG_CONFIRMED_MESSAGES", "").split():
            state.confirmed.add(key)
            state.pending.discard(key)
        for key in os.environ.get("CHANGELOG_RETRY_MESSAGES", "").split():
            state.pending.discard(key)
        state.prepared = True
        state.save()
        if state.pending:
            raise RuntimeError("A previous Discord delivery is uncertain; inspect delivery.json and use the recovery inputs")
        # Arcane-Edit-End

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r", encoding="utf-8-sig") as f:
        cur_changelog = yaml.safe_load(f)

    diff = diff_changelog(last_changelog, cur_changelog)
    # Arcane-Edit-Start: one Components V2 card per PR; debug uses the identical payloads.
    payloads = (payload for entry in diff for payload in render_entry(entry))
    if DEBUG:
        payloads = list(payloads)
        output = Path(os.environ.get("CHANGELOG_DEBUG_OUTPUT", "Resources/Changelog/DiscordDebug.json"))
        output.write_text(json.dumps(payloads, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Wrote {len(payloads)} Discord messages to {output}")
        return

    with requests.Session() as session:
        for payload in payloads:
            state.deliver(payload, lambda message: send_card(session, message))
    # Arcane-Edit-End


def get_most_recent_workflow(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    workflow_run = get_current_run(sess, github_repository, github_run)
    # Arcane-Edit-Start: compare completion times, including reruns of older builds.
    past_runs = list(get_past_runs(sess, workflow_run))
    return max(past_runs, key=lambda run: run.get("updated_at", run["created_at"]), default=None)
    # Arcane-Edit-End


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}", timeout=30 # Arcane-Edit: bounded request
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any, include_failed: bool = False) -> Iterable[Any]: # Arcane-Edit: failed runs may contain delivery receipts
    """
    Get all successful workflow runs before our current one.
    """
    # Trauma - replaced serverside filter with clientside because apparently copilot cant figure out basic SQL
    params = {
        "per_page": 100,
    }
    url = f"{current_run['workflow_url']}/runs"
    now = current_run.get("run_started_at", current_run["created_at"]) # Arcane-Edit: use the current attempt's start time

    while url:
        resp = sess.get(url, params=params, timeout=30) # Arcane-Edit: bounded request
        resp.raise_for_status()

        for run in resp.json()["workflow_runs"]:
            # First past successful run that isn't our current run.
            # Arcane-Edit-Start: select the same publication branch, including failed deliveries when requested.
            if (run["id"] == current_run["id"] or run["status"] != "completed"
                or (not include_failed and run.get("conclusion") != "success")
                or run.get("head_branch") != current_run.get("head_branch") or run.get("updated_at", run["created_at"]) > now):
            # Arcane-Edit-End
                continue

            yield run

        next_url = resp.links.get("next", {}).get("url")
        if not next_url:
            break

        url = next_url
        params = None


def get_last_changelog(state=None) -> str: # Arcane-Edit: restore delivery receipts alongside the published snapshot
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    # Arcane-Edit-Start: the packaged journal is authoritative, including recovered PRs.
    current = get_current_run(session, github_repository, github_run)
    runs = list(get_past_runs(session, current, include_failed=True))
    successful = [run for run in runs if run.get("conclusion") == "success"]
    completed_at = lambda run: run.get("updated_at", run["created_at"])
    most_recent = max(successful, key=completed_at, default=None)
    base_run = os.environ.get("CHANGELOG_BASE_RUN_ID")
    if base_run:
        if not base_run.isdecimal():
            raise ValueError("CHANGELOG_BASE_RUN_ID must be a run ID")
        most_recent = get_current_run(session, github_repository, base_run)
        if (most_recent.get("head_branch") != current.get("head_branch") or most_recent["status"] != "completed"
            or most_recent.get("workflow_url") != current.get("workflow_url")):
            raise ValueError("The recovery baseline must be a completed publication of this branch")
    if state is not None:
        failed = [run for run in runs if run.get("conclusion") != "success"
                  and (most_recent is None or completed_at(run) > completed_at(most_recent))]
        if current.get("run_attempt", 1) > 1:
            failed.append(current)
        restore_receipts(session, GITHUB_API_URL, github_repository, failed, state)
    if most_recent is None:
        return "Entries: []"
    saved = [a for a in artifacts(session, GITHUB_API_URL, github_repository, most_recent["id"])
             if a["name"] == "published-changelog"]
    if saved:
        return read_artifact(session, saved[0], "ArcaneChangelog.yml")
    last_sha = most_recent["head_sha"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )
    if yaml.safe_load(last_changelog_stream).get("Entries"):
        raise RuntimeError("The last published journal snapshot is missing; restore its artifact before sending")
    return last_changelog_stream
    # Arcane-Edit-End


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    params = {
        "ref": sha,
    }
    headers = {"Accept": "application/vnd.github.raw"}

    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
        timeout=30, # Arcane
    )
    # Arcane-Start: a successful build before introducing ArcaneChangelog.yml has no Arcane entries.
    if resp.status_code == 404:
        return "Entries: []"
    # Arcane-End
    resp.raise_for_status()
    return resp.text


def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    # Arcane-Edit-Start: recovered build entries can have different IDs from later committed entries.
    def key(entry):
        return entry.get("url") or entry["id"]
    old_entry_ids = {key(entry) for entry in old["Entries"]}
    current_entry_ids = {key(entry) for entry in cur["Entries"]}
    if not old_entry_ids <= current_entry_ids:
        raise RuntimeError("The journal omits previously published PRs; refusing to reset the Discord baseline to an older or incomplete history")
    return (entry for entry in cur["Entries"] if key(entry) not in old_entry_ids)
    # Arcane-Edit-End


def get_discord_body(content: str):
    return {
        "content": content,
        # Do not allow any mentions.
        "allowed_mentions": {"parse": []},
        # SUPPRESS_EMBEDS
        "flags": 1 << 2,
    }


def send_discord_webhook(lines: list[str]):
    content = "".join(lines)
    body = get_discord_body(content)

    response = requests.post(DISCORD_WEBHOOK_URL, json=body)
    response.raise_for_status()


def truncate_to_limit(text: str, limit: int) -> str:
    if len(text) <= limit:
        return text

    if limit <= len(TRUNCATION_SUFFIX):
        return TRUNCATION_SUFFIX[:limit]

    return text[: limit - len(TRUNCATION_SUFFIX)].rstrip() + TRUNCATION_SUFFIX


def create_change_line(emoji: str, message: str, url: str | None) -> str:
    if url is None:
        prefix = f"{emoji} - "
        suffix = "\n"
    else:
        pr_number = urllib.parse.urlparse(url).path.rstrip("/").split("/")[-1]
        prefix = f"{emoji} - "
        suffix = f" ([#{pr_number}]({url}))\n"

    available_message_length = DISCORD_SPLIT_LIMIT - len(prefix) - len(suffix)
    if available_message_length < 1:
        raise ValueError(f"Rendered changelog line has no room for a message: {url}")

    message = truncate_to_limit(message, available_message_length)
    return f"{prefix}{message}{suffix}"


def changelog_entries_to_message_lines(entries: Iterable[ChangelogEntry]) -> list[str]:
    """Process structured changelog entries into a list of lines making up a formatted message."""
    message_lines = []

    for contributor_name, group in itertools.groupby(entries, lambda x: x["author"]):
        message_lines.append("\n")
        message_lines.append(f"**{contributor_name}** updated:\n")

        for entry in group:
            url = entry.get("url")
            if url and not url.strip():
                url = None

            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change["type"], "❓")
                message = change["message"]

                labels = entry.get("labels") or []
                if EXPERIMENTAL_LABEL in labels:
                    emoji = f"{emoji}{EXPERIMENTAL_EMOJI}"

                message_lines.append(create_change_line(emoji, message, url))

    return message_lines


def split_message_lines(message_lines: list[str]) -> list[list[str]]:
    """Join message lines into chunks that are each below Discord's message length limit."""
    chunks = []
    chunk_lines = []
    chunk_length = 0

    for line in message_lines:
        line_length = len(line)
        if line_length > DISCORD_SPLIT_LIMIT:
            raise ValueError(
                f"Changelog line is too long for Discord after truncation: {line_length}"
            )

        new_chunk_length = chunk_length + line_length

        if new_chunk_length > DISCORD_SPLIT_LIMIT:
            if chunk_lines:
                chunks.append(chunk_lines)

            new_chunk_length = line_length
            chunk_lines = []

        chunk_lines.append(line)
        chunk_length = new_chunk_length

    if chunk_lines:
        chunks.append(chunk_lines)

    return chunks


def dump_debug_markdown(message_lines: list[str]):
    chunks = split_message_lines(message_lines)

    with DEBUG_DISCORD_DUMP_FILE.open("w", encoding="utf-8", newline="\n") as f:
        f.write("# Discord Changelog Debug Dump\n\n")
        f.write(
            f"Generated from `{DEBUG_CHANGELOG_FILE_OLD}` to `{CHANGELOG_FILE}`.\n\n"
        )

        if not chunks:
            f.write("_No changelog entries to send._\n")
            return

        for i, chunk_lines in enumerate(chunks, start=1):
            content = "".join(chunk_lines)
            f.write(
                f"<!-- Discord message break: chunk {i}/{len(chunks)}, {len(content)}/{DISCORD_SPLIT_LIMIT} characters -->\n\n"
            )
            f.write(f"## Discord Message {i}\n\n")
            f.write(content.lstrip("\n"))
            f.write("\n")

    print(f"Wrote Discord changelog debug dump to {DEBUG_DISCORD_DUMP_FILE}")


def send_message_lines(message_lines: list[str]):
    """Join a list of message lines into chunks that are each below Discord's message length limit, and send them."""
    chunks = split_message_lines(message_lines)

    for chunk_lines in chunks[:-1]:
        print("Split changelog and sending to discord")
        send_discord_webhook(chunk_lines)

    if chunks:
        print("Sending final changelog to discord")
        send_discord_webhook(chunks[-1])


# Arcane-Start
def send_card(session, payload):
    """Retry explicit rate limits only; an uncertain POST must not be replayed."""
    parsed = urllib.parse.urlsplit(DISCORD_WEBHOOK_URL or "")
    if parsed.scheme != "https" or parsed.hostname not in ("discord.com", "discordapp.com") or not re.fullmatch(r"/api(?:/v\d+)?/webhooks/\d+/[^/]+", parsed.path):
        raise ValueError("DISCORD_WEBHOOK_URL must be a Discord HTTPS webhook")
    query = dict(urllib.parse.parse_qsl(parsed.query))
    query.update(wait="true", with_components="true")
    target = urllib.parse.urlunsplit(parsed._replace(query=urllib.parse.urlencode(query)))
    for attempt in range(6):
        try:
            response = session.post(target, json=payload, timeout=30)
        except requests.RequestException:
            raise RuntimeError("Discord delivery could not be confirmed; inspect the channel before retrying") from None
        if response.status_code == 429 and attempt < 5:
            try:
                delay = float(response.json()["retry_after"])
            except (ValueError, TypeError, KeyError):
                raise RuntimeError("Discord returned an invalid rate-limit response") from None
            if not 0 <= delay <= 60:
                raise RuntimeError("Discord rate-limit delay exceeds the retry budget")
            time.sleep(max(delay, 0.05))
            continue
        if response.status_code == 400:
            fallback = link_fallback(payload)
            if fallback:
                print("Discord rejected a gallery; preserving its media as links")
                return fallback
        if 400 <= response.status_code < 500:
            raise RejectedDelivery(f"Discord rejected the changelog (HTTP {response.status_code})")
        if not 200 <= response.status_code < 300:
            raise RuntimeError(f"Discord rejected the changelog (HTTP {response.status_code})")
        try:
            message_id = response.json()["id"]
            if not isinstance(message_id, str) or not message_id.isdecimal():
                raise ValueError("Invalid Discord message ID")
        except (ValueError, KeyError, TypeError):
            raise RuntimeError("Discord returned no confirmed message ID; inspect the channel before retrying") from None
        return {"message_id": message_id}
# Arcane-End

if __name__ == "__main__":
    # Trauma - removed try catch so it can actually be debugged idiots
    main()
