"""App configuration: secrets from environment, non-secret defaults from a local JSON file."""
import json
import os
from pathlib import Path

from dotenv import load_dotenv

BASE_DIR = Path(__file__).resolve().parent.parent
load_dotenv(BASE_DIR / ".env")

SETTINGS_PATH = BASE_DIR / "settings.json"

DEFAULT_SETTINGS = {
    "output_dir": str(BASE_DIR / "outputs"),
    "default_model": "dev",
    "default_image_size": "landscape_4_3",
    "default_enable_safety_checker": True,
}


def get_fal_key() -> str | None:
    # Re-read from environment on every call so a key added to .env after
    # startup (or exported in the shell) is picked up without a restart.
    load_dotenv(BASE_DIR / ".env", override=True)
    return os.environ.get("FAL_KEY")


def load_settings() -> dict:
    if not SETTINGS_PATH.exists():
        return dict(DEFAULT_SETTINGS)
    try:
        with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, OSError):
        return dict(DEFAULT_SETTINGS)
    merged = dict(DEFAULT_SETTINGS)
    merged.update(data)
    return merged


def save_settings(patch: dict) -> dict:
    current = load_settings()
    current.update(patch)
    with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
        json.dump(current, f, indent=2)
    return current


def get_output_dir() -> Path:
    settings = load_settings()
    out_dir = Path(settings["output_dir"])
    out_dir.mkdir(parents=True, exist_ok=True)
    return out_dir
