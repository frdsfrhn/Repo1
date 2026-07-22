"""Thin wrapper around fal.ai's queue API for the Flux text-to-image models.

API reference: https://fal.ai/models/fal-ai/flux/dev/api
Queue protocol: POST to submit, then poll the returned status_url until the
job is COMPLETED, then GET the response_url for the final result.
"""
import httpx

from . import config

FAL_QUEUE_BASE = "https://queue.fal.run"

MODEL_ENDPOINTS = {
    "dev": "fal-ai/flux/dev",
    "schnell": "fal-ai/flux/schnell",
}


class FalAPIError(RuntimeError):
    def __init__(self, status_code: int, detail: str):
        super().__init__(detail)
        self.status_code = status_code
        self.detail = detail


def _headers() -> dict:
    key = config.get_fal_key()
    if not key:
        raise FalAPIError(401, "No FAL_KEY configured. Add it to .env and restart the server.")
    return {"Authorization": f"Key {key}", "Content-Type": "application/json"}


def build_payload(
    prompt: str,
    model: str,
    image_size,
    num_inference_steps: int,
    guidance_scale: float | None,
    num_images: int,
    seed: int | None,
    enable_safety_checker: bool,
    output_format: str,
) -> dict:
    payload = {
        "prompt": prompt,
        "image_size": image_size,
        "num_inference_steps": num_inference_steps,
        "num_images": num_images,
        "enable_safety_checker": enable_safety_checker,
        "output_format": output_format,
    }
    # schnell ignores guidance_scale; only send it for models that use it.
    if guidance_scale is not None and model == "dev":
        payload["guidance_scale"] = guidance_scale
    if seed is not None:
        payload["seed"] = seed
    return payload


async def _request(method: str, url: str, timeout: float = 30.0, **kwargs) -> httpx.Response:
    try:
        async with httpx.AsyncClient(timeout=timeout) as client:
            resp = await client.request(method, url, **kwargs)
    except httpx.HTTPError as exc:
        raise FalAPIError(502, f"Could not reach fal.ai: {exc}") from exc
    if resp.status_code >= 400:
        raise FalAPIError(resp.status_code, resp.text)
    return resp


async def submit_job(model: str, payload: dict) -> dict:
    endpoint = MODEL_ENDPOINTS[model]
    resp = await _request("POST", f"{FAL_QUEUE_BASE}/{endpoint}", json=payload, headers=_headers())
    return resp.json()


async def get_status(status_url: str) -> dict:
    resp = await _request("GET", status_url, headers=_headers())
    return resp.json()


async def get_result(response_url: str) -> dict:
    resp = await _request("GET", response_url, headers=_headers())
    return resp.json()


async def download_image(url: str) -> bytes:
    resp = await _request("GET", url, timeout=60.0)
    return resp.content
