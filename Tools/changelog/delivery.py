# Arcane-Start
"""Confirmed-delivery receipts and exact published journal snapshots."""
import hashlib
import io
import json
from pathlib import Path
import zipfile


class RejectedDelivery(RuntimeError):
    """Discord explicitly rejected a request; retrying cannot duplicate it."""


class DeliveryState:
    def __init__(self, filename):
        self.filename = Path(filename)
        self.confirmed = set()
        self.pending = set()
        self.messages = {}
        self.fallbacks = {}
        self.prepared = False

    def absorb(self, data):
        self.confirmed.update(data.get("confirmed", []))
        # Each prepared receipt inherits previous state, including explicit operator resolutions.
        # Unioning pending sets would resurrect a request already cleared for a safe retry.
        self.pending = set(data.get("pending", [])) - self.confirmed
        self.messages.update(data.get("messages", {}))
        self.fallbacks.update(data.get("fallbacks", {}))

    def save(self):
        self.filename.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.filename.with_suffix(".tmp")
        temporary.write_text(json.dumps({"prepared": self.prepared, "confirmed": sorted(self.confirmed), "pending": sorted(self.pending), "messages": self.messages, "fallbacks": self.fallbacks}, ensure_ascii=False, indent=2), encoding="utf-8")
        temporary.replace(self.filename)

    def deliver(self, payload, send):
        key = hashlib.sha256(json.dumps(payload, sort_keys=True, ensure_ascii=False).encode()).hexdigest()
        if key in self.confirmed:
            return
        if self.pending:
            raise RuntimeError("A previous Discord request has an uncertain outcome; inspect delivery.json before acknowledging or retrying it")
        if key in self.fallbacks:
            # Resume the chosen representation, even if Discord would now accept the gallery.
            for message in self.fallbacks[key]:
                self.deliver(message, send)
            self.confirmed.add(key)
            self.save()
            return
        self.prepared = True
        self.pending.add(key)
        components = payload["components"][0]["components"]
        self.messages[key] = {"heading": components[0]["content"], "footer": components[-1].get("content", "")}
        self.save()
        try:
            fallback = send(payload)
        except (RejectedDelivery, ValueError):
            self.pending.remove(key)
            self.save()
            raise
        if isinstance(fallback, list) and fallback:
            # The original gallery was rejected. Each fallback has its own receipt.
            self.pending.remove(key)
            self.fallbacks[key] = fallback
            self.save()
            for message in fallback:
                self.deliver(message, send)
        elif isinstance(fallback, dict):
            self.messages[key].update(fallback)
        self.confirmed.add(key)
        self.pending.discard(key)
        self.save()


def artifacts(session, api, repository, run_id):
    result = []
    page = 1
    while True:
        response = session.get(f"{api}/repos/{repository}/actions/runs/{run_id}/artifacts", params={"per_page": 100, "page": page}, timeout=30)
        response.raise_for_status()
        members = response.json()["artifacts"]
        result.extend(members)
        if len(members) < 100:
            return result
        page += 1


def read_artifact(session, artifact, filename):
    if artifact.get("expired"):
        raise RuntimeError("The changelog recovery artifact has expired; a verified baseline is required")
    # Never extract paths from the archive to disk. Limit compressed and uncompressed input.
    with session.get(artifact["archive_download_url"], timeout=30, stream=True) as response:
        response.raise_for_status()
        content = bytearray()
        for chunk in response.iter_content(65536):
            content.extend(chunk)
            if len(content) > 16 * 1024 * 1024:
                raise RuntimeError("Changelog artifact exceeds the download limit")
    with zipfile.ZipFile(io.BytesIO(content)) as archive:
        member = archive.getinfo(filename)
        if member.file_size > 32 * 1024 * 1024:
            raise RuntimeError("Changelog artifact exceeds the uncompressed limit")
        return archive.read(member).decode("utf-8-sig")


def restore_receipts(session, api, repository, runs, state):
    for run in sorted(runs, key=lambda r: r.get("updated_at", r.get("created_at", ""))):
        saved = [a for a in artifacts(session, api, repository, run["id"]) if a["name"].startswith("changelog-delivery-")]
        # The newest prepared receipt contains all state inherited by that attempt.
        for artifact in sorted(saved, key=lambda a: a["created_at"], reverse=True):
            data = json.loads(read_artifact(session, artifact, "delivery.json"))
            if data.get("prepared", True):
                state.absorb(data)
                break
        latest_attempt = run.get("run_attempt", 1) - (run.get("status") != "completed")
        for attempt in range(1, latest_attempt + 1):
            if any(a["name"] == f"changelog-delivery-{attempt}" for a in saved):
                continue
            response = session.get(f"{api}/repos/{repository}/actions/runs/{run['id']}/attempts/{attempt}/jobs", params={"per_page": 100}, timeout=30)
            response.raise_for_status()
            attempted = any(step["name"] == "Publish changelog (Discord)" and step.get("conclusion") not in (None, "skipped")
                            for job in response.json()["jobs"] for step in job.get("steps", []))
            if attempted:
                raise RuntimeError(f"Run {run['id']} attempt {attempt} attempted Discord delivery but has no receipt; inspect the channel before recovery")
# Arcane-End
