#!/usr/bin/env python3
"""Exercise the entire Web -> API -> PostgreSQL path. Python standard library only."""
import json
import os
import urllib.error
import urllib.request
import uuid

base = os.environ.get("BASE_URL", "http://localhost:8080").rstrip("/")
def call(path, method="GET", data=None, expected=200):
    request = urllib.request.Request(base + path, method=method,
        data=json.dumps(data).encode() if data is not None else None,
        headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            code, body = response.status, response.read()
    except urllib.error.HTTPError as error:
        code, body = error.code, error.read()
    assert code == expected, f"{method} {path}: expected {expected}, got {code}: {body!r}"
    return json.loads(body) if body else None

with urllib.request.urlopen(base, timeout=15) as response:
    assert b"TaskBoard" in response.read(), "Homepage missing"
assert call("/health")["status"] == "healthy"
call("/api/tasks", "POST", {"title": "   "}, 400)
call("/api/tasks", "POST", {"title": "x" * 121}, 400)
call("/api/tasks", "POST", {}, 400)
title = "CI test " + uuid.uuid4().hex
item = call("/api/tasks", "POST", {"title": title}, 201)
task_id = item["id"]
try:
    assert item["title"] == title and item["completed"] is False
    assert any(task["id"] == task_id for task in call("/api/tasks"))
    updated = call(f"/api/tasks/{task_id}", "PUT", {"completed": True})
    assert updated["completed"] is True
    assert next(task for task in call("/api/tasks") if task["id"] == task_id)["completed"] is True
    updated = call(f"/api/tasks/{task_id}", "PUT", {"completed": False})
    assert updated["completed"] is False
finally:
    call(f"/api/tasks/{task_id}", "DELETE", expected=204)
assert all(task["id"] != task_id for task in call("/api/tasks"))
call(f"/api/tasks/{task_id}", "DELETE", expected=404)
call(f"/api/tasks/{task_id}", "PUT", {"completed": True}, 404)
print("PASS: homepage, readiness, validation, create, list, complete, reopen, delete, and missing tasks")
