const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => document.querySelectorAll(sel);

// ---- tabs ----
$$(".tab-btn").forEach((btn) => {
  btn.addEventListener("click", () => {
    $$(".tab-btn").forEach((b) => b.classList.remove("active"));
    $$(".tab-panel").forEach((p) => p.classList.remove("active"));
    btn.classList.add("active");
    $(`#tab-${btn.dataset.tab}`).classList.add("active");
    if (btn.dataset.tab === "gallery") loadGallery();
    if (btn.dataset.tab === "settings") loadSettings();
  });
});

// ---- model-dependent field visibility ----
$("#model").addEventListener("change", () => {
  const isDev = $("#model").value === "dev";
  $("#guidance-wrap").style.display = isDev ? "" : "none";
  $("#steps").value = isDev ? 28 : 4;
  $("#steps").max = isDev ? 50 : 12;
});

$("#random_seed").addEventListener("change", () => {
  $("#seed").disabled = $("#random_seed").checked;
});

// ---- generate ----
let pollTimer = null;

$("#generate-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  clearInterval(pollTimer);

  const body = {
    prompt: $("#prompt").value,
    model: $("#model").value,
    image_size: $("#image_size").value,
    num_inference_steps: Number($("#steps").value),
    guidance_scale: $("#model").value === "dev" ? Number($("#guidance_scale").value) : null,
    num_images: Number($("#num_images").value),
    seed: $("#random_seed").checked ? null : Number($("#seed").value) || null,
    enable_safety_checker: $("#enable_safety_checker").checked,
    output_format: $("#output_format").value,
  };

  setGenerating(true);
  $("#error").classList.add("hidden");
  $("#results").innerHTML = "";
  showProgress("Submitting job…");

  try {
    const res = await fetch("/api/generate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.detail || "Failed to submit job");

    pollJob(data.job_id);
  } catch (err) {
    showError(err.message);
    setGenerating(false);
    hideProgress();
  }
});

function pollJob(jobId) {
  pollTimer = setInterval(async () => {
    try {
      const res = await fetch(`/api/jobs/${jobId}`);
      const data = await res.json();
      if (!res.ok) throw new Error(data.detail || "Job polling failed");

      if (data.status === "IN_QUEUE") {
        showProgress(
          data.queue_position != null ? `Queued (position ${data.queue_position})…` : "Queued…"
        );
      } else if (data.status === "IN_PROGRESS") {
        const lastLog = data.logs && data.logs.length ? data.logs[data.logs.length - 1] : null;
        showProgress(lastLog || "Generating…");
      } else if (data.status === "COMPLETED") {
        clearInterval(pollTimer);
        hideProgress();
        setGenerating(false);
        renderResults(data.result.images, {
          prompt: $("#prompt").value,
          model: $("#model").value,
          seed: data.result.seed,
        });
      } else if (data.status === "FAILED") {
        clearInterval(pollTimer);
        hideProgress();
        setGenerating(false);
        showError(data.error || "Generation failed");
      }
    } catch (err) {
      clearInterval(pollTimer);
      hideProgress();
      setGenerating(false);
      showError(err.message);
    }
  }, 1200);
}

function setGenerating(isGenerating) {
  $("#generate-btn").disabled = isGenerating;
  $("#generate-btn").textContent = isGenerating ? "Generating…" : "Generate";
}

function showProgress(text) {
  $("#progress").classList.remove("hidden");
  $("#progress-text").textContent = text;
}

function hideProgress() {
  $("#progress").classList.add("hidden");
}

function showError(message) {
  $("#error").textContent = message;
  $("#error").classList.remove("hidden");
}

function renderResults(images, meta) {
  const grid = $("#results");
  images.forEach((img) => {
    grid.appendChild(buildImageCard(img.url, meta.prompt, `${meta.model} · seed ${meta.seed}`, meta));
  });
}

function buildImageCard(url, prompt, metaText, reuseData) {
  const tpl = $("#image-card-template");
  const node = tpl.content.cloneNode(true);
  node.querySelector("img").src = url;
  node.querySelector(".prompt").textContent = prompt;
  node.querySelector(".meta").textContent = metaText;
  const reuseBtn = node.querySelector(".reuse-btn");
  if (reuseData) {
    reuseBtn.addEventListener("click", () => reuseSettings(reuseData, prompt));
  } else {
    reuseBtn.remove();
  }
  return node;
}

function reuseSettings(meta, prompt) {
  $("#prompt").value = prompt || "";
  if (meta.model) {
    $("#model").value = meta.model;
    $("#model").dispatchEvent(new Event("change"));
  }
  if (meta.seed != null) {
    $("#seed").value = meta.seed;
    $("#random_seed").checked = false;
    $("#seed").disabled = false;
  }
  if (meta.image_size) $("#image_size").value = meta.image_size;
  if (meta.num_inference_steps) $("#steps").value = meta.num_inference_steps;
  if (meta.guidance_scale) $("#guidance_scale").value = meta.guidance_scale;
  if (typeof meta.enable_safety_checker === "boolean") {
    $("#enable_safety_checker").checked = meta.enable_safety_checker;
  }
  $$('.tab-btn[data-tab="generate"]')[0].click();
  window.scrollTo({ top: 0, behavior: "smooth" });
}

// ---- gallery ----
async function loadGallery() {
  const grid = $("#gallery-grid");
  grid.innerHTML = "<p>Loading…</p>";
  try {
    const res = await fetch("/api/gallery?limit=100");
    const data = await res.json();
    grid.innerHTML = "";
    if (!data.items.length) {
      grid.innerHTML = "<p>No generations yet.</p>";
      return;
    }
    data.items.forEach((item) => {
      const card = buildImageCard(
        item.url,
        item.prompt,
        `${item.model} · seed ${item.seed ?? "?"} · ${new Date(item.created_at + "Z").toLocaleString()}`,
        item
      );
      grid.appendChild(card);
    });
  } catch (err) {
    grid.innerHTML = `<p class="error">${err.message}</p>`;
  }
}

$("#refresh-gallery").addEventListener("click", loadGallery);

// ---- settings ----
async function loadSettings() {
  const res = await fetch("/api/settings");
  const data = await res.json();
  $("#api-key-status").textContent = data.has_api_key
    ? "fal.ai API key: configured ✓"
    : "fal.ai API key: NOT configured — add FAL_KEY to .env and restart the server.";
  $("#api-key-status").classList.toggle("warn", !data.has_api_key);
  $("#setting-output-dir").value = data.output_dir;
  $("#setting-default-model").value = data.default_model;
  $("#setting-default-size").value = data.default_image_size;
  $("#setting-default-safety").checked = data.default_enable_safety_checker;
}

$("#save-settings").addEventListener("click", async () => {
  const patch = {
    output_dir: $("#setting-output-dir").value,
    default_model: $("#setting-default-model").value,
    default_image_size: $("#setting-default-size").value,
    default_enable_safety_checker: $("#setting-default-safety").checked,
  };
  await fetch("/api/settings", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(patch),
  });
  $("#settings-saved").classList.remove("hidden");
  setTimeout(() => $("#settings-saved").classList.add("hidden"), 1500);
});

// ---- init: apply defaults from settings on load ----
(async function init() {
  try {
    const res = await fetch("/api/settings");
    const data = await res.json();
    $("#model").value = data.default_model;
    $("#model").dispatchEvent(new Event("change"));
    $("#image_size").value = data.default_image_size;
    $("#enable_safety_checker").checked = data.default_enable_safety_checker;
  } catch (_) {
    // Non-fatal — form keeps its HTML defaults.
  }
})();
