"""SQLite-backed gallery/history for generated images."""
import sqlite3
from contextlib import contextmanager
from pathlib import Path

from . import config

SCHEMA = """
CREATE TABLE IF NOT EXISTS generations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    filename TEXT NOT NULL,
    prompt TEXT NOT NULL,
    model TEXT NOT NULL,
    image_size TEXT NOT NULL,
    num_inference_steps INTEGER NOT NULL,
    guidance_scale REAL,
    seed INTEGER,
    enable_safety_checker INTEGER NOT NULL,
    has_nsfw_concept INTEGER,
    fal_request_id TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);
"""


def _db_path() -> Path:
    return config.get_output_dir() / "gallery.db"


@contextmanager
def _connect():
    conn = sqlite3.connect(_db_path())
    conn.row_factory = sqlite3.Row
    try:
        conn.execute(SCHEMA)
        yield conn
        conn.commit()
    finally:
        conn.close()


def save_generation(
    filename: str,
    prompt: str,
    model: str,
    image_size: str,
    num_inference_steps: int,
    guidance_scale: float | None,
    seed: int | None,
    enable_safety_checker: bool,
    has_nsfw_concept: bool | None,
    fal_request_id: str,
) -> int:
    with _connect() as conn:
        cur = conn.execute(
            """INSERT INTO generations
                (filename, prompt, model, image_size, num_inference_steps,
                 guidance_scale, seed, enable_safety_checker, has_nsfw_concept, fal_request_id)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                filename,
                prompt,
                model,
                image_size,
                num_inference_steps,
                guidance_scale,
                seed,
                int(enable_safety_checker),
                None if has_nsfw_concept is None else int(has_nsfw_concept),
                fal_request_id,
            ),
        )
        return cur.lastrowid


def list_generations(limit: int = 50, offset: int = 0) -> list[dict]:
    with _connect() as conn:
        rows = conn.execute(
            "SELECT * FROM generations ORDER BY id DESC LIMIT ? OFFSET ?",
            (limit, offset),
        ).fetchall()
        return [dict(r) for r in rows]


def get_generation(gen_id: int) -> dict | None:
    with _connect() as conn:
        row = conn.execute("SELECT * FROM generations WHERE id = ?", (gen_id,)).fetchone()
        return dict(row) if row else None


def delete_generation(gen_id: int) -> bool:
    with _connect() as conn:
        cur = conn.execute("DELETE FROM generations WHERE id = ?", (gen_id,))
        return cur.rowcount > 0
