"""FastAPI backend: a thin console in front of fal.ai's hosted Flux API."""
import mimetypes
import uuid
from pathlib import Path
from typing import Optional

from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel

from . import config, storage
from .fal_client import FalAPIError, build_payload, download_image, get_result, get_status, submit_job

BASE_DIR = Path(__file__).resolve().parent.parent
FRONTEND_DIR = BASE_DIR / "frontend"

app = FastAPI(title="Flux Generation Console")

# In-memory job tracker. Jobs are transient by design — the durable record
# of a generation is the gallery (SQLite + files on disk), not this dict.
JOBS: dict[str, dict] = {}


class GenerateRequest(BaseModel):
    prompt: str
    model: str = "dev"
    image_size: str = "landscape_4_3"
    num_inference_steps: int = 28
    guidance_scale: Optional[float] = 3.5
    num_images: int = 1
    seed: Optional[int] = None
    enable_safety_checker: bool = True
    output_format: str = "jpeg"


class SettingsPatch(BaseModel):
    output_dir: Optional[str] = None
    default_model: Optional[str] = None
    default_image_size: Optional[str] = None
    default_enable_safety_checker: Optional[bool] = None


@app.post("/api/generate")
async def generate(req: GenerateRequest):
    if req.model not in ("dev", "schnell"):
        raise HTTPException(400, "model must be 'dev' or 'schnell'")
    if not req.prompt.strip():
        raise HTTPException(400, "prompt is required")
    if not (1 <= req.num_images <= 4):
        raise HTTPException(400, "num_images must be between 1 and 4")

    payload = build_payload(
        prompt=req.prompt,
        model=req.model,
        image_size=req.image_size,
        num_inference_steps=req.num_inference_steps,
        guidance_scale=req.guidance_scale,
        num_images=req.num_images,
        seed=req.seed,
        enable_safety_checker=req.enable_safety_checker,
        output_format=req.output_format,
    )

    try:
        submission = await submit_job(req.model, payload)
    except FalAPIError as e:
        raise HTTPException(e.status_code if e.status_code < 500 else 502, e.detail)

    job_id = str(uuid.uuid4())
    JOBS[job_id] = {
        "status": "IN_QUEUE",
        "model": req.model,
        "status_url": submission["status_url"],
        "response_url": submission["response_url"],
        "fal_request_id": submission.get("request_id"),
        "params": req.model_dump(),
        "result": None,
        "error": None,
    }
    return {"job_id": job_id}


@app.get("/api/jobs/{job_id}")
async def job_status(job_id: str):
    job = JOBS.get(job_id)
    if job is None:
        raise HTTPException(404, "unknown job_id")

    if job["status"] == "COMPLETED":
        return _job_response(job)
    if job["status"] == "FAILED":
        return _job_response(job)

    try:
        status = await get_status(job["status_url"])
    except FalAPIError as e:
        job["status"] = "FAILED"
        job["error"] = e.detail
        return _job_response(job)

    job["status"] = status.get("status", job["status"])
    job["queue_position"] = status.get("queue_position")
    job["logs"] = [entry.get("message", "") for entry in status.get("logs", []) if entry.get("message")]

    if job["status"] == "COMPLETED":
        try:
            result = await get_result(job["response_url"])
            job["result"] = await _persist_result(job, result)
        except FalAPIError as e:
            job["status"] = "FAILED"
            job["error"] = e.detail

    return _job_response(job)


async def _persist_result(job: dict, result: dict) -> dict:
    params = job["params"]
    out_dir = config.get_output_dir()
    images = result.get("images", [])
    nsfw_flags = result.get("has_nsfw_concepts", [])
    seed = result.get("seed", params.get("seed"))

    saved = []
    for idx, img in enumerate(images):
        image_bytes = await download_image(img["url"])
        ext = mimetypes.guess_extension(img.get("content_type", "image/jpeg")) or ".jpg"
        filename = f"{uuid.uuid4().hex}{ext}"
        (out_dir / filename).write_bytes(image_bytes)

        nsfw_flag = nsfw_flags[idx] if idx < len(nsfw_flags) else None
        gen_id = storage.save_generation(
            filename=filename,
            prompt=params["prompt"],
            model=params["model"],
            image_size=str(params["image_size"]),
            num_inference_steps=params["num_inference_steps"],
            guidance_scale=params.get("guidance_scale"),
            seed=seed,
            enable_safety_checker=params["enable_safety_checker"],
            has_nsfw_concept=nsfw_flag,
            fal_request_id=job["fal_request_id"],
        )
        saved.append({"id": gen_id, "filename": filename, "url": f"/api/images/{filename}"})

    return {"images": saved, "seed": seed}


def _job_response(job: dict) -> dict:
    return {
        "status": job["status"],
        "queue_position": job.get("queue_position"),
        "logs": job.get("logs", []),
        "result": job.get("result"),
        "error": job.get("error"),
    }


@app.get("/api/gallery")
def gallery(limit: int = 50, offset: int = 0):
    items = storage.list_generations(limit=limit, offset=offset)
    for item in items:
        item["url"] = f"/api/images/{item['filename']}"
    return {"items": items}


@app.get("/api/gallery/{gen_id}")
def gallery_item(gen_id: int):
    item = storage.get_generation(gen_id)
    if item is None:
        raise HTTPException(404, "not found")
    item["url"] = f"/api/images/{item['filename']}"
    return item


@app.delete("/api/gallery/{gen_id}")
def delete_gallery_item(gen_id: int):
    item = storage.get_generation(gen_id)
    if item is None:
        raise HTTPException(404, "not found")
    image_path = config.get_output_dir() / item["filename"]
    if image_path.exists():
        image_path.unlink()
    storage.delete_generation(gen_id)
    return {"deleted": True}


@app.get("/api/images/{filename}")
def get_image(filename: str):
    if "/" in filename or "\\" in filename or filename in (".", ".."):
        raise HTTPException(400, "invalid filename")
    path = config.get_output_dir() / filename
    if not path.is_file():
        raise HTTPException(404, "not found")
    return FileResponse(path)


@app.get("/api/settings")
def get_settings():
    settings = config.load_settings()
    settings["has_api_key"] = bool(config.get_fal_key())
    return settings


@app.post("/api/settings")
def update_settings(patch: SettingsPatch):
    updates = {k: v for k, v in patch.model_dump().items() if v is not None}
    settings = config.save_settings(updates)
    settings["has_api_key"] = bool(config.get_fal_key())
    return settings


app.mount("/", StaticFiles(directory=FRONTEND_DIR, html=True), name="frontend")
