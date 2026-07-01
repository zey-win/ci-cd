const apiBase = document.querySelector('meta[name="builder-api"]')?.content?.replace(/\/$/, "") || "https://zeywin-android-builder-api.vercel.app";
const operatorKey = document.querySelector('meta[name="builder-key"]')?.content?.trim() || "";
const op = (extra = {}) => ({ "Content-Type": "application/json", "x-builder-key": operatorKey, ...extra });
const $ = id => document.getElementById(id);
function ensureContainer(id) { return $(id) || (() => { const d = document.createElement("div"); d.id = id; d.style.cssText = "padding:0 12px 12px"; document.querySelector("header").after(d); return d; })(); }
const buildsContainer = ensureContainer("builds-container");
const activeContainer = ensureContainer("active-container");
const modal = $("modal");
const form = $("build-form");
const newBuildBtn = $("new-build");
const cancelBtn = $("modal-cancel");
const repoSelect = $("repo-select");
const branchSelect = $("branch-select");
const gameIcon = $("game-icon");
const iconSpinner = $("icon-spinner");
const iconFile = $("icon-file");
const firebaseFile = $("firebase-file");
const loading = $("loading");

const BRANCHES = { "zey-win/plinko": [{ ref: "main", label: "Plinko" }] };
const DEFAULT_BRANCHES = [{ ref: "main", label: "main" }];
const REPO_NAMES = { "zey-win/plinko": "plinko", "zey-win/blackjack": "blackjack", "zey-win/roulette": "roulette", "zey-win/dragon-tiger": "dragon tiger", "zey-win/baccarat-tiger": "baccarat", "zey-win/wheel-of-fortune": "lucky wheel", "zey-win/Unstopable": "unstopable", "zey-win/SlotSpot": "slotspot" };
const REPO_ICONS = {
  "zey-win/plinko": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/plinko.png",
  "zey-win/plinko@app/plinko-real-money": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__plinkorm.png",
  "zey-win/plinko@app/plinko": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/plinko.png",
  "zey-win/plinko@main": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__plinkofaling.png",
  "zey-win/blackjack": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__blackjack.png",
  "zey-win/roulette": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__roulette.png",
  "zey-win/dragon-tiger": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/plinko.png",
  "zey-win/baccarat-tiger": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__baccarat-tiger.png",
  "zey-win/wheel-of-fortune": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__wheel-of-fortune.png",
  "zey-win/Unstopable": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__Unstopable.png",
  "zey-win/SlotSpot": "https://raw.githubusercontent.com/zey-win/zey-win.github.io/main/repo-icons/zey-win__SlotSpot.png"
};

let runMeta = {}, releases = [], firebaseJson = null, currentIconDataUrl = null, iconLoadedDeferred = null;
let runs = [];

function repoFromTitle(t) { const s = (t || "").toLowerCase().replace(/[^a-z0-9 ]/g, " "); for (const [repo, name] of Object.entries(REPO_NAMES)) if (s.includes(name)) return repo; return null; }
function parseTitle(title) { if (!title) return { app: "Build", pkg: "" }; const parts = title.replace(/^Android:\s*/i, "").split(" / ").map(p => p.trim()); return { app: parts[0] || "Build", pkg: parts[1] || "" }; }
function esc(s) { const d = document.createElement("div"); d.textContent = s; return d.innerHTML; }

function showIconSpinner() { gameIcon.style.display = "none"; iconSpinner.style.display = "block"; }
function hideIconSpinner() { iconSpinner.style.display = "none"; }
function setIcon(src) { if (src) { gameIcon.src = src; gameIcon.style.display = "block"; } else gameIcon.style.display = "none"; hideIconSpinner(); }

async function getIconDataUrl(repo, ref) {
  const refStr = ref || "main"; showIconSpinner();
  try {
    const res = await fetch(`${apiBase}/api/icon?game_repository=${encodeURIComponent(repo)}&game_ref=${encodeURIComponent(refStr)}`, { headers: op() });
    if (!res.ok) { setIcon(null); return null; }
    const d = await res.json();
    if (d.ok && d.icon?.dataUrl) { setIcon(d.icon.dataUrl); return d.icon.dataUrl; }
    setIcon(null); return null;
  } catch { setIcon(null); return null; }
}

let audioCtx = null;
function getAudioCtx() { if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)(); return audioCtx; }
function playFanfare() { try { const ctx = getAudioCtx(); [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => { const o = ctx.createOscillator(); const g = ctx.createGain(); o.type = "sine"; o.frequency.value = f; g.gain.setValueAtTime(0.15, ctx.currentTime + i * 0.15); g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + i * 0.15 + 2.5); o.connect(g); g.connect(ctx.destination); o.start(ctx.currentTime + i * 0.15); o.stop(ctx.currentTime + i * 0.15 + 2.5); }); } catch {} }
function playErrorSound() { try { const ctx = getAudioCtx(); const o = ctx.createOscillator(); const g = ctx.createGain(); o.type = "sawtooth"; o.frequency.value = 150; g.gain.setValueAtTime(0.15, ctx.currentTime); g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 2.5); o.connect(g); g.connect(ctx.destination); o.start(); o.stop(ctx.currentTime + 2.5); } catch {} }

function fireConfetti(dur = 4000) {
  const canvas = document.createElement("canvas"); canvas.style.cssText = "position:fixed;inset:0;pointer-events:none;z-index:9999;mix-blend-mode:screen"; canvas.width = innerWidth; canvas.height = innerHeight; document.body.appendChild(canvas); const ctx = canvas.getContext("2d");
  const p = []; const colors = ["#ffd700","#ff4500","#00ff00","#ff1493","#00bfff","#ffa500","#ff00ff","#ffff00"];
  for (let i = 0; i < 150; i++) p.push({ x: Math.random() * canvas.width, y: Math.random() * canvas.height * -1, vx: (Math.random() - 0.5) * 6, vy: Math.random() * 4 + 2, w: Math.random() * 10 + 4, h: Math.random() * 6 + 2, color: colors[Math.floor(Math.random() * colors.length)], alpha: 1, rot: Math.random() * 360 });
  const start = Date.now();
  function anim() { const elapsed = Date.now() - start; if (elapsed > dur) { canvas.remove(); return; } ctx.clearRect(0, 0, canvas.width, canvas.height); for (const pt of p) { pt.x += pt.vx; pt.y += pt.vy; pt.vy += 0.02; pt.rot += 2; pt.alpha = Math.max(0, 1 - elapsed / dur); ctx.save(); ctx.translate(pt.x, pt.y); ctx.rotate(pt.rot * Math.PI / 180); ctx.globalAlpha = pt.alpha; ctx.fillStyle = pt.color; ctx.fillRect(-pt.w / 2, -pt.h / 2, pt.w, pt.h); ctx.restore(); } requestAnimationFrame(anim); }
  anim();
}

async function loadReleases() {
  try { const res = await fetch("https://api.github.com/repos/zey-win/ci-cd/releases?per_page=50"); if (!res.ok) return; const data = await res.json(); releases.length = 0; for (const r of (data || [])) { const tag = r.tag_name || ""; const assets = (r.assets || []).map(a => ({ name: a.name, url: a.browser_download_url })); releases.push({ tag, name: r.name, assets }); } } catch {}
}
function findDownloads(pkg) {
  if (!pkg) return { apk: null, aab: null };
  const parts = pkg.toLowerCase().split(".").filter(Boolean);
  const lastPart = parts[parts.length - 1];
  if (!lastPart || lastPart.includes("*")) return { apk: null, aab: null };
  for (const rel of releases) {
    const searchIn = (rel.name || rel.tag || "").toLowerCase();
    if (!searchIn.includes(lastPart)) continue;
    const apk = rel.assets.find(a => a.name.endsWith(".apk") && a.name.toLowerCase().includes(lastPart));
    const aab = rel.assets.find(a => a.name.endsWith(".aab") && a.name.toLowerCase().includes(lastPart));
    if (apk || aab) return { apk: apk?.url || null, aab: aab?.url || null };
  }
  return { apk: null, aab: null };
}
async function loadArtifactVersions() {
  const pkgs = new Set();
  for (const r of runs) {
    if (r.conclusion === "success" && !runMeta[r.id]?.ver) {
      const { pkg } = parseTitle(r.displayTitle || r.name || "");
      if (pkg) pkgs.add(pkg);
    }
  }
  for (const pkg of pkgs) {
    try {
      const res = await fetch(`${apiBase}/api/artifacts?package_name=${encodeURIComponent(pkg)}`);
      if (!res.ok) continue;
      const d = await res.json();
      if (d.versionName || d.versionCode) {
        const run = runs.find(r => { const { pkg: rp } = parseTitle(r.displayTitle || r.name || ""); return rp === pkg; });
        if (run) {
          if (!runMeta[run.id]) runMeta[run.id] = {};
          if (d.versionName) runMeta[run.id].ver = d.versionName;
          if (d.versionCode) runMeta[run.id].code = d.versionCode;
        }
      }
    } catch {}
  }
}

function resolveIcon(r) {
  const meta = runMeta[r.id];
  if (meta?.icon) return meta.icon;
  const raw = r.displayTitle || r.name || "";
  const { app } = parseTitle(raw);
  const repo = repoFromTitle(raw);
  const appLower = (app || "").toLowerCase();
  let iconUrl = REPO_ICONS[repo] || null;
  if (repo === "zey-win/plinko") {
    if (appLower.includes("real money")) iconUrl = REPO_ICONS["zey-win/plinko@app/plinko-real-money"] || iconUrl;
    else if (appLower.includes("real game")) iconUrl = REPO_ICONS["zey-win/plinko@app/plinko"] || iconUrl;
    else if (appLower.includes("plinko") && !appLower.includes("falling")) iconUrl = REPO_ICONS["zey-win/plinko@app/plinko"] || iconUrl;
    else iconUrl = REPO_ICONS["zey-win/plinko@main"] || iconUrl;
  }
  return iconUrl;
}

function buildCardHTML(r, idx) {
  const raw = r.displayTitle || r.name || "";
  const { app, pkg } = parseTitle(raw);
  const concl = r.conclusion || "";
  const st = r.status || "unknown";
  const url = r.htmlUrl || "#";
  const meta = runMeta[r.id];
  const iconUrl = resolveIcon(r);
  const verStr = (meta?.ver) || String(r.runNumber || "");
  const codeStr = (meta?.code) || String(r.runAttempt || "1");
  const versionInfo = verStr ? `<span class="version-line">v${esc(verStr)} (code ${codeStr})</span>` : "";
  let downloads = { apk: null, aab: null };
  if (concl === "success") {
    if (meta?.apk || meta?.aab) downloads = { apk: meta.apk || null, aab: meta.aab || null };
    else downloads = findDownloads(pkg);
    if (downloads.apk || downloads.aab) {
      if (!runMeta[r.id]) runMeta[r.id] = {};
      if (downloads.apk) runMeta[r.id].apk = downloads.apk;
      if (downloads.aab) runMeta[r.id].aab = downloads.aab;
      try { const sv = JSON.parse(localStorage.getItem('rv')||'{}'); if (!sv[r.id]) sv[r.id]={}; if (downloads.apk) sv[r.id].apk=downloads.apk; if (downloads.aab) sv[r.id].aab=downloads.aab; localStorage.setItem('rv',JSON.stringify(sv)); } catch {}
    }
  }
  let label, cls;
  if (concl === "success") { label = "✅<span class='lt'> Готов</span>"; cls = "status-success"; }
  else if (concl === "failure") { label = "❌<span class='lt'> Ошибка</span>"; cls = "status-failure"; }
  else if (["waiting","queued","pending"].includes(st)) { label = "⏳<span class='lt'> В очереди</span>"; cls = "status-pending"; }
  else if (st === "completed") { label = "❌<span class='lt'> Ошибка</span>"; cls = "status-failure"; }
  else { label = "🔄<span class='lt'> "+st+"</span>"; cls = "status-pending"; }
  const isProgress = ["in_progress","pending","queued","waiting"].includes(st);
  const bgStyle = idx % 2 === 0 ? 'background:#1a202c' : 'background:#161b22';
  const iconBlock = iconUrl ? `<img class="card-icon" src="${iconUrl}" alt="" loading="lazy" onerror="this.onerror=null;this.alt='🎮'">` : `<div class="card-icon card-icon-placeholder">🎮</div>`;
  const timerId = `t-${r.id}`;
  const timerHtml = `<span class="timer" id="${timerId}" data-run="${r.id}" style="display:${st!=='completed'&&timers[r.id]?'inline':'none'};color:#ffd700;font-size:15px;font-weight:700">⏱ 00:00,00</span>`;
  return `<div class="build-card${isProgress?' in-progress':''}" style="${bgStyle}" data-id="${r.id}">${iconBlock}<div class="info"><div class="app-name">${esc(app)}</div><div class="meta">${esc(pkg)}</div><div class="meta">${versionInfo}</div></div><div class="right-col"><div class="actions-col">${concl==="success"&&downloads.apk?`<a class="dl-btn dl-btn-apk" href="${downloads.apk}" download>APK</a>`:""}${concl==="success"&&downloads.aab?`<a class="dl-btn dl-btn-aab" href="${downloads.aab}" download>AAB</a>`:""}${concl!=="success"?`<a href="${esc(url)}" target="_blank" class="log-btn log-btn-bottom">Логи</a>`:""}</div><div class="actions-col"><div class="status-row">${timerHtml}<span class="status ${cls} status-small">${label}</span></div><button class="del-btn" onclick="deleteRun(${r.id},event)" title="Удалить">✕</button></div></div></div>`;
}

function updateTimers() {
  const now = Date.now();
  for (const [id, t] of Object.entries(timers)) {
    const run = runs.find(r => r.id === id);
    if (run && run.status === "completed") { delete timers[id]; const el = document.getElementById(`t-${id}`); if (el) el.style.display = "none"; continue; }
    const el = document.getElementById(`t-${id}`);
    if (!el) continue;
    const left = Math.max(0, t.total - (now - t.start));
    if (left <= 0) { el.style.display = "none"; delete timers[id]; continue; }
    const m = Math.floor(left / 60000);
    const s = Math.floor((left % 60000) / 1000);
    const cs = Math.floor((left % 1000) / 10);
    el.textContent = `⏱ ${String(m).padStart(2,"0")}:${String(s).padStart(2,"0")},${String(cs).padStart(2,"0")}`;
  }
}

const timers = {};
function startTimerLoop() {
  let running = true;
  function tick() { if (!running) return; updateTimers(); requestAnimationFrame(tick); }
  requestAnimationFrame(tick);
  return () => { running = false; };
}

function renderAll() {
  const active = runs.filter(r => r.status !== "completed");
  const done = runs.filter(r => r.status === "completed" && r.conclusion === "success");
  const fail = runs.filter(r => r.status === "completed" && r.conclusion !== "success");
  const allDone = [...done, ...fail].slice(0, 100);

  const activeHtml = active.length ? active.map((r, i) => buildCardHTML(r, i)).join("") : "";
  const doneHtml = allDone.length ? allDone.map((r, i) => buildCardHTML(r, i + active.length)).join("") : "";

  if (activeContainer.innerHTML !== activeHtml) activeContainer.innerHTML = activeHtml;
  if (buildsContainer.innerHTML !== doneHtml) buildsContainer.innerHTML = doneHtml;
  activeContainer.style.display = activeHtml ? "" : "none";
  buildsContainer.style.display = doneHtml ? "" : "none";

  let statsEl = document.getElementById("stats");
  if (!statsEl) {
    statsEl = document.createElement("div");
    statsEl.id = "stats";
    statsEl.style.cssText = "text-align:center;color:#8b949e;font-size:13px;padding:8px 0";
    buildsContainer.appendChild(statsEl);
  }
  statsEl.textContent = `Всего: ${runs.length} | Активные: ${active.length} | Успешно: ${done.length} | Ошибок: ${fail.length}`;
}

async function loadBuilds() {
  if (loading) loading.style.display = "inline-flex";
  try {
    const [runsRes] = await Promise.all([fetch(`${apiBase}/api/runs`), loadReleases()]);
    if (!runsRes.ok) return;
    const d = await runsRes.json();
    const oldRuns = runs;
    runs = Array.isArray(d.runs) ? d.runs : [];
    for (const r of runs) {
      let icon = r.iconUrl;
      if (!icon) {
        const title = `${r.displayTitle || ""} ${r.name || ""}`;
        const m = title.match(/builder-([a-z0-9]+)/);
        if (m) icon = `https://raw.githubusercontent.com/zey-win/ci-cd/main/builds/icons/${m[0]}.png`;
      }
      if (!runMeta[r.id]) runMeta[r.id] = {};
      if (icon) runMeta[r.id].icon = icon;
    }
    try {
      const sv = JSON.parse(localStorage.getItem('rv')||'{}');
      for (const [id, v] of Object.entries(sv)) {
        const rid = parseInt(id);
        if (!runMeta[rid]) runMeta[rid] = {};
        if (v.ver) runMeta[rid].ver = v.ver;
        if (v.code) runMeta[rid].code = v.code;
        if (v.apk) runMeta[rid].apk = v.apk;
        if (v.aab) runMeta[rid].aab = v.aab;
      }
    } catch {}
    for (const r of runs) {
      if (r.status !== "completed" && !timers[r.id]) {
        const created = r.createdAt ? new Date(r.createdAt).getTime() : Date.now();
        timers[r.id] = { start: created, total: (39 + (r.id % 12)) * 60 * 1000 };
      }
    }
    for (const r of runs) {
      const old = oldRuns.find(o => o.id === r.id);
      if (old && old.status !== "completed" && r.status === "completed") {
        if (r.conclusion === "success") { playFanfare(); fireConfetti(4000); }
        else { playErrorSound(); flashRed(); }
      }
    }
    renderAll();
    loadArtifactVersions().then(() => renderAll());
  } catch(e) { console.error(e); } finally { if (loading) loading.style.display = "none"; }
}

async function deleteRun(runId, e) {
  if (!confirm("Удалить эту сборку?")) return;
  const btn = e?.target; if (btn) btn.disabled = true;
  try {
    await fetch(`${apiBase}/api/delete`, { method: "POST", headers: op(), body: JSON.stringify({ run_id: runId }) });
    runs = runs.filter(r => r.id !== runId);
    delete runMeta[runId];
    delete timers[runId];
    try { const sv = JSON.parse(localStorage.getItem('rv')||'{}'); delete sv[runId]; localStorage.setItem('rv',JSON.stringify(sv)); } catch {}
    renderAll();
  } catch (err) { alert("Ошибка удаления: " + err.message); }
  finally { if (btn) btn.disabled = false; }
}

function updateBranches() {
  const repo = repoSelect.value;
  const list = BRANCHES[repo] || DEFAULT_BRANCHES;
  branchSelect.innerHTML = list.map(b => `<option value="${b.ref}">${b.label}</option>`).join("");
  getIconDataUrl(repo, branchSelect.value);
}

iconFile.addEventListener("change", e => {
  const f = e.target.files[0]; if (!f) return;
  if (f.type !== "image/png") { alert("Только PNG"); return; }
  const r = new FileReader();
  r.onload = () => { currentIconDataUrl = r.result; gameIcon.src = r.result; gameIcon.style.display = "block"; };
  r.readAsDataURL(f);
});
$("icon-gen-btn").addEventListener("click", () => {
  getIconDataUrl(repoSelect.value, branchSelect.value).then(url => {
    if (url) currentIconDataUrl = url;
  });
});
firebaseFile.addEventListener("change", e => {
  const f = e.target.files[0]; if (!f) return;
  if (f.type !== "application/json") { alert("Только JSON"); return; }
  const r = new FileReader();
  r.onload = () => { firebaseJson = r.result; };
  r.readAsDataURL(f);
});

function flashRed() {
  const div = document.createElement("div");
  div.style.cssText = "position:fixed;inset:0;background:#ff0000;z-index:99999;pointer-events:none";
  document.body.appendChild(div);
  requestAnimationFrame(() => {
    div.style.transition = "opacity 3s ease-out";
    div.style.opacity = "0";
    setTimeout(() => div.remove(), 3500);
  });
}

repoSelect.addEventListener("change", () => { updateBranches(); });
branchSelect.addEventListener("change", () => {
  getIconDataUrl(repoSelect.value, branchSelect.value).then(url => { if (url) currentIconDataUrl = url; });
});

newBuildBtn.addEventListener("click", () => {
  currentIconDataUrl = null; firebaseJson = null;
  form.style.display = "";
  $("build-progress").classList.add("hidden");
  const all = form.querySelectorAll("label, .modal-actions, .form-row, .icon-format-row");
  all.forEach(el => { el.style.opacity = ""; el.style.transform = ""; el.style.transition = ""; });
  modal.classList.remove("hidden");
  updateBranches();
  iconLoadedDeferred = getIconDataUrl(repoSelect.value, branchSelect.value).then(url => { if (url) currentIconDataUrl = url; });
});
cancelBtn.addEventListener("click", () => modal.classList.add("hidden"));
modal.addEventListener("click", e => { if (e.target === modal) modal.classList.add("hidden"); });

form.addEventListener("submit", async e => {
  e.preventDefault();
  const submitBtn = $("modal-submit");
  submitBtn.disabled = true;
  submitBtn.textContent = "Жди...";
  const fd = new FormData(form);
  if (!currentIconDataUrl && iconLoadedDeferred) {
    try { const url = await iconLoadedDeferred; if (url) currentIconDataUrl = url; } catch {}
  }
  const iconData = currentIconDataUrl || "";
  const p = {
    game_repository: fd.get("game_repository") || "zey-win/plinko",
    game_ref: fd.get("game_ref") || "main",
    app_name: fd.get("app_name") || "",
    package_name: fd.get("package_name") || "",
    build_format: fd.get("build_format") || "apk",
    version_name: fd.get("version_name") || "",
    version_code: fd.get("version_code") || "",
    icon_png_path: iconData,
    zeywin_api_key: fd.get("zeywin_api_key") || "",
    signing_profile: fd.get("signing_profile") || "slotspot",
    fast_build: fd.get("fast_build") || "true",
    admob_android_app_id: fd.get("admob_app_id") || "",
    admob_android_banner_id: fd.get("admob_banner") || "",
    admob_android_interstitial_id: fd.get("admob_interstitial") || "",
    admob_android_rewarded_id: fd.get("admob_rewarded") || "",
    admob_android_rewarded_interstitial_id: fd.get("admob_rewarded_interstitial") || "",
    admob_android_native_id: fd.get("admob_native") || "",
    admob_android_app_open_id: fd.get("admob_app_open") || "",
    firebase_json_base64: firebaseJson || ""
  };
  try {
    const res = await fetch(`${apiBase}/api/build`, { method: "POST", headers: op(), body: JSON.stringify(p) });
    if (!res.ok) { alert("Ошибка: " + await res.text().catch(() => "")); return; }
    const d = await res.json();
    if (d.run) {
      const serverIconPath = d.icon?.path;
      const serverIconUrl = serverIconPath ? `https://raw.githubusercontent.com/zey-win/ci-cd/main/${serverIconPath}` : (d.icon?.htmlUrl || null);
      runMeta[d.run.id] = { icon: serverIconUrl || null, ver: p.version_name || "", code: p.version_code || "" };
      try {
        const sv = JSON.parse(localStorage.getItem('rv')||'{}');
        sv[d.run.id]={ver:p.version_name||'',code:p.version_code||''};
        if (d.latestArtifact?.apkDownloadUrl) { runMeta[d.run.id].apk = d.latestArtifact.apkDownloadUrl; sv[d.run.id].apk = d.latestArtifact.apkDownloadUrl; }
        if (d.latestArtifact?.aabDownloadUrl) { runMeta[d.run.id].aab = d.latestArtifact.aabDownloadUrl; sv[d.run.id].aab = d.latestArtifact.aabDownloadUrl; }
        localStorage.setItem('rv',JSON.stringify(sv));
      } catch {}
      timers[d.run.id] = { start: Date.now(), total: (39 + (d.run.id % 12)) * 60 * 1000 };
      runs = [d.run, ...runs];
      renderAll();
    } else loadBuilds();
    iconLoadedDeferred = null;
    // Animated close
    animateModalClose();
  } catch (err) { alert("Ошибка: " + err.message); }
});

function animateModalClose() {
  const form = $("build-form");
  const progress = $("build-progress");
  const steps = $("bp-steps");
  const submitBtn = $("modal-submit");
  const all = form.querySelectorAll("label, .modal-actions, .form-row, .icon-format-row");
  const phrases = [
    "Клонирование репозитория...", "Установка зависимостей...", "Компиляция кода...",
    "Сборка ресурсов...", "Оптимизация изображений...", "Подписание APK...",
    "Проверка ProGuard...", "Генерация AAB...", "Запуск тестов...",
    "Финализация сборки...", "Загрузка артефактов...", "Обновление метаданных..."
  ];
  let delay = 0;
  all.forEach((el, i) => {
    const d = 200 + i * 120;
    setTimeout(() => { el.style.transition = "opacity .3s,transform .3s"; el.style.opacity = "0"; el.style.transform = "scale(.95)"; }, d);
    delay = d + 300;
  });
  setTimeout(() => { form.style.display = "none"; progress.classList.remove("hidden"); if (submitBtn) submitBtn.textContent = "0%"; }, delay);
  const totalTime = 5000;
  const start = Date.now();
  let idx = 0;
  function scrollText() {
    const elapsed = Date.now() - start;
    if (elapsed >= totalTime) { 
      modal.classList.add("hidden"); 
      form.style.display = ""; 
      form.querySelectorAll("label, .modal-actions, .form-row, .icon-format-row").forEach(el => { el.style.opacity = ""; el.style.transform = ""; }); 
      progress.classList.add("hidden"); 
      if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = "Собрать"; }
      return; 
    }
    idx = Math.floor((elapsed / totalTime) * phrases.length * 3) % phrases.length;
    steps.textContent = phrases[idx % phrases.length];
    const percent = Math.floor((elapsed / totalTime) * 100);
    if (submitBtn) submitBtn.textContent = percent + "%";
    steps.style.opacity = "0";
    requestAnimationFrame(() => { steps.style.transition = "opacity .05s"; steps.style.opacity = "1"; });
    requestAnimationFrame(scrollText);
  }
  requestAnimationFrame(scrollText);
}

// ----- Step buttons -----
document.addEventListener("click", e => {
  const btn = e.target.closest(".step-btn");
  if (!btn) return;
  const el = document.getElementById(btn.dataset.target);
  if (!el) return;
  const step = parseFloat(btn.dataset.step) || 1;
  const dir = parseInt(btn.dataset.dir) || 1;
  const cur = parseFloat(el.value) || 0;
  const nxt = Math.round((cur + dir * step) * 100) / 100;
  el.value = nxt < 0 ? 0 : nxt;
});

// ----- AdMob auto-split on paste + reveal hidden fields -----
$("f-admob-app").addEventListener("paste", e => {
  const text = (e.clipboardData || window.clipboardData).getData("text");
  const ids = text.split(/[\n\t,;]+/).map(s => s.trim()).filter(Boolean);
  if (ids.length >= 4) {
    e.preventDefault();
    document.querySelector('[name="admob_app_id"]').value = ids[0];
    document.querySelector('[name="admob_banner"]').value = ids[1];
    document.querySelector('[name="admob_interstitial"]').value = ids[2];
    document.querySelector('[name="admob_rewarded"]').value = ids[3];
  }
  showAdmobFields();
});
$("f-admob-app").addEventListener("input", () => {
  if ($("f-admob-app").value) showAdmobFields();
});

function showAdmobFields() {
  document.querySelectorAll(".admob-field").forEach(el => el.classList.add("show"));
}

// ----- Package name reveals extra fields -----
$("f-pkg").addEventListener("input", function() {
  if (this.value) document.querySelectorAll(".pkg-hidden").forEach(el => el.classList.add("show"));
});

// ----- all-txt-paste: parse entire data block into app_name field -----
$("f-app-name").addEventListener("paste", e => {
  const text = (e.clipboardData || window.clipboardData).getData("text").trim();
  if (!text) return;
  const map = {
    app_name: ["app", "name", "название", "имя", "title"],
    package_name: ["package", "pkg", "code", "bundle", "com."],
    version_name: ["version", "верс", "v"],
    version_code: ["code", "version code", "код"],
    zeywin_api_key: ["zeywin", "key", "api key", "zw_"],
    admob_app_id: ["admob app", "admob id", "ca-app-pub-...~"],
    admob_banner: ["banner"],
    admob_interstitial: ["interstitial", "inter"],
    admob_rewarded: ["rewarded", "reward"],
    admob_rewarded_interstitial: ["rewarded interstitial", "rew inter"],
    admob_native: ["native"],
    admob_app_open: ["app open", "appopen"]
  };

  // Try JSON first
  try {
    const json = JSON.parse(text);
    const fields = document.querySelectorAll("#build-form input, #build-form select");
    for (const field of fields) {
      const name = field.name;
      if (json[name] !== undefined) field.value = json[name];
    }
    return;
  } catch {}

  // Try key: value lines
  const lines = text.split("\n").filter(l => l.includes(":") || l.includes("="));
  if (lines.length < 2) return;
  e.preventDefault();
  for (const line of lines) {
    const sep = line.includes("=") ? "=" : ":";
    const idx = line.indexOf(sep);
    if (idx < 0) continue;
    let key = line.slice(0, idx).trim().toLowerCase();
    let val = line.slice(idx + 1).trim();
    // find matching field
    for (const [fieldName, aliases] of Object.entries(map)) {
      if (aliases.some(a => key.includes(a))) {
        const el = document.querySelector(`[name="${fieldName}"]`);
        if (el) el.value = val;
        break;
      }
    }
  }
});

// ----- Startup -----
const stopTimerLoop = startTimerLoop();
renderSkeletons();

(async () => {
  await loadReleases();
  updateBranches();
  loadBuilds();
  setInterval(loadBuilds, 15000);
})();

function renderSkeletons() {
  buildsContainer.style.display = "";
  activeContainer.style.display = "none";
  buildsContainer.innerHTML = '<div class="skeleton-card"></div>'.repeat(20);
}
