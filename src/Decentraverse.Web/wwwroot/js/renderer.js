// Galaxy map and star-system view, drawn with PixiJS (window.PIXI, loaded by index.html).
// This module only draws: all data (positions, colors, sizes, orbits) is computed in C#
// by the generator and the scene builder, then passed in as plain objects.

const PIXI = window.PIXI;

const GALAXY_FIT_RADIUS = 1150;     // world units that must fit on screen at start
const PANEL_WIDTH = 380;            // px covered by the side panel in system view
const CLICK_TOLERANCE_PX = 4;       // pointer movement that still counts as a click
const PICK_RADIUS_PX = 14;          // how close to a star/planet a click must be

let app, host, dotnet, tooltip;
let mode = 'galaxy';
let elapsed = 0;
const tex = {};

const galaxy = {
    layer: null, world: null, nebulae: null, stars: null,
    entries: new Map(),             // block number → { data, root, glow, core, x, y, tx, ty, born, flash }
};

const system = {
    layer: null, world: null,
    planets: [], stars: [], moons: [], blackHole: null,
    selected: -1, selectionRing: null,
};

const pointer = { down: false, dragging: false, startX: 0, startY: 0, lastX: 0, lastY: 0 };

// ---------------------------------------------------------------------------------------------
// Public API (called from C#)
// ---------------------------------------------------------------------------------------------

export async function init(container, dotnetRef) {
    host = container;
    dotnet = dotnetRef;

    app = new PIXI.Application();
    await app.init({
        resizeTo: host,
        backgroundAlpha: 0,
        antialias: true,
        autoDensity: true,
        resolution: Math.min(window.devicePixelRatio || 1, 2),
    });
    host.appendChild(app.canvas);

    makeTextures();
    buildBackdrop();

    galaxy.layer = new PIXI.Container();
    galaxy.world = new PIXI.Container();
    galaxy.nebulae = new PIXI.Container();
    galaxy.stars = new PIXI.Container();
    const core = new PIXI.Sprite(tex.glow);
    core.anchor.set(0.5);
    core.scale.set(9);
    core.tint = 0xffe2b8;
    core.alpha = 0.22;
    core.blendMode = 'add';
    galaxy.world.addChild(core, galaxy.nebulae, galaxy.stars);
    galaxy.layer.addChild(galaxy.world);

    system.layer = new PIXI.Container();
    system.world = new PIXI.Container();
    system.layer.addChild(system.world);
    system.layer.visible = false;

    app.stage.addChild(galaxy.layer, system.layer);

    tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    host.appendChild(tooltip);

    bindInput();
    app.ticker.add(tick);
    window.addEventListener('resize', onResize);
    fitGalaxy();
}

/** Adds new stars (with a birth animation) and moves existing ones to their new positions. */
export function setGalaxy(stars, animateNew) {
    for (const s of stars) {
        const e = galaxy.entries.get(s.n);
        if (e) {
            e.data = s;
            e.tx = s.x;
            e.ty = s.y;
        } else {
            addStar(s, animateNew);
        }
    }
}

export function showSystem(scene) {
    mode = 'system';
    hideTooltip();
    galaxy.layer.visible = false;
    system.layer.visible = true;
    buildSystem(scene);
    fitSystem();
}

export function showMoons(planetIndex, moons) {
    system.selected = planetIndex;
    for (const m of system.moons) m.sprite.destroy();
    system.moons = moons.map(m => {
        const sprite = new PIXI.Sprite(tex.dot);
        sprite.anchor.set(0.5);
        sprite.tint = m.c;
        sprite.scale.set(m.s / 8);
        system.world.addChild(sprite);
        return { data: m, sprite };
    });
    const planet = system.planets.find(p => p.data.index === planetIndex);
    if (planet) {
        system.selectionRing.visible = true;
        system.selectionRing.clear()
            .circle(0, 0, planet.data.s + 3).stroke({ width: 1, color: 0xffffff, alpha: 0.7 });
    }
}

export function showGalaxy(focusNumber) {
    mode = 'galaxy';
    system.layer.visible = false;
    galaxy.layer.visible = true;
    const e = galaxy.entries.get(focusNumber);
    if (e) {
        e.flash = 1;
    }
}

const LANGUAGE_KEY = 'decentraverse.language';

// Words for the tooltip; replaced by applyLanguage().
let texts = { block: 'block', class: 'class', binary: 'binary', blackHole: 'black hole', locale: 'en-US' };

/** A ?lang= in the URL, otherwise the viewer's saved choice, otherwise the browser's language. */
export function preferredLanguage() {
    const fromUrl = new URLSearchParams(location.search).get('lang');
    if (fromUrl) return fromUrl;
    try {
        const saved = localStorage.getItem(LANGUAGE_KEY);
        if (saved) return saved;
    } catch {
        // Storage can be unavailable (private mode, blocked site data): fall back to the browser.
    }
    return navigator.language || 'en';
}

export function applyLanguage(code, words, remember) {
    texts = words;
    document.documentElement.lang = code;
    if (remember) {
        try {
            localStorage.setItem(LANGUAGE_KEY, code);
        } catch {
            // Not saved: the choice still applies to this visit.
        }
    }
}

export function dispose() {
    window.removeEventListener('resize', onResize);
    app?.destroy(true, { children: true, texture: true });
}

// ---------------------------------------------------------------------------------------------
// Galaxy
// ---------------------------------------------------------------------------------------------

function addStar(s, animate) {
    const root = new PIXI.Container();
    root.position.set(s.x, s.y);

    let glow, core;
    if (s.k === 1) {
        // Black hole: dark core inside a bright accretion ring, easy to spot.
        glow = new PIXI.Sprite(tex.ring);
        glow.anchor.set(0.5);
        glow.tint = 0xffa040;
        glow.scale.set(s.s / 10);
        glow.blendMode = 'add';
        core = new PIXI.Graphics().circle(0, 0, s.s * 0.45).fill({ color: 0x000000 });
    } else {
        glow = new PIXI.Sprite(tex.glow);
        glow.anchor.set(0.5);
        glow.tint = s.c;
        glow.scale.set(s.s * 0.11);
        glow.alpha = 0.55;
        glow.blendMode = 'add';
        core = new PIXI.Sprite(tex.dot);
        core.anchor.set(0.5);
        core.tint = lighten(s.c);
        core.scale.set(s.s / 14);
    }
    root.addChild(glow, core);

    if (s.k === 2) {
        // Binary: a small companion dot.
        const companion = new PIXI.Sprite(tex.dot);
        companion.anchor.set(0.5);
        companion.tint = 0xffffff;
        companion.scale.set(s.s / 28);
        companion.position.set(s.s * 0.9, 0);
        root.addChild(companion);
    }

    if (s.d > 650) {
        // Full blocks light up nebulae around their star.
        const neb = new PIXI.Sprite(tex.glow);
        neb.anchor.set(0.5);
        neb.position.set(s.x, s.y);
        neb.tint = s.n % 2 ? 0x7a5cff : 0x3fa7ff;
        neb.alpha = 0.05 + 0.12 * (s.d - 650) / 350;
        neb.scale.set(0.9 + 1.6 * (s.d - 650) / 350);
        neb.blendMode = 'add';
        galaxy.nebulae.addChild(neb);
    }

    galaxy.stars.addChild(root);
    const entry = { data: s, root, glow, core, x: s.x, y: s.y, tx: s.x, ty: s.y, born: animate ? 0 : 1, flash: animate ? 1 : 0 };
    if (animate) {
        root.scale.set(0);
    }
    galaxy.entries.set(s.n, entry);
}

function tickGalaxy(dt) {
    for (const e of galaxy.entries.values()) {
        // Glide towards the target position (it changes when a newer block becomes the reference).
        e.x += (e.tx - e.x) * Math.min(1, dt * 2.5);
        e.y += (e.ty - e.y) * Math.min(1, dt * 2.5);
        e.root.position.set(e.x, e.y);

        if (e.born < 1) {
            e.born = Math.min(1, e.born + dt / 1.2);
            // "Ease out back": grows past full size, then settles.
            const u = e.born - 1, c = 1.7;
            e.root.scale.set(1 + (c + 1) * u * u * u + c * u * u);
        }
        if (e.flash > 0) {
            e.flash = Math.max(0, e.flash - dt / 1.5);
            e.glow.alpha = 0.55 + 1.5 * e.flash;
        } else if (e.data.k !== 1) {
            e.glow.alpha = 0.5 + 0.08 * Math.sin(elapsed * 1.7 + e.data.n);
        } else {
            e.glow.rotation += dt * 0.8;
        }
    }
}

function fitGalaxy() {
    const w = app.screen.width, h = app.screen.height;
    const scale = Math.min(w, h) / (2 * GALAXY_FIT_RADIUS);
    galaxy.world.scale.set(scale);
    galaxy.world.position.set(w / 2, h / 2);
}

function pickStar(globalX, globalY) {
    const p = galaxy.world.toLocal({ x: globalX, y: globalY });
    const limit = PICK_RADIUS_PX / galaxy.world.scale.x;
    let best = null, bestD = limit * limit;
    for (const e of galaxy.entries.values()) {
        const dx = e.x - p.x, dy = e.y - p.y, d = dx * dx + dy * dy;
        if (d < bestD) {
            bestD = d;
            best = e;
        }
    }
    return best;
}

// ---------------------------------------------------------------------------------------------
// System
// ---------------------------------------------------------------------------------------------

function buildSystem(scene) {
    system.world.removeChildren().forEach(c => c.destroy({ children: true }));
    system.planets = [];
    system.stars = [];
    system.moons = [];
    system.blackHole = null;
    system.selected = -1;

    const g = new PIXI.Graphics();
    if (scene.hzOuter > scene.hzInner) {
        // Habitable zone: a faint green band.
        const mid = (scene.hzInner + scene.hzOuter) / 2;
        g.circle(0, 0, mid).stroke({ width: scene.hzOuter - scene.hzInner, color: 0x3ddc84, alpha: 0.07 });
    }
    for (const p of scene.planets) {
        g.circle(0, 0, p.orbit).stroke({ width: 1, color: 0xffffff, alpha: 0.09 });
    }
    system.world.addChild(g);

    if (scene.blackHole) {
        const disk = new PIXI.Sprite(tex.ring);
        disk.anchor.set(0.5);
        disk.tint = 0xffa040;
        disk.scale.set(1.6, 0.55);
        disk.blendMode = 'add';
        const halo = new PIXI.Sprite(tex.glow);
        halo.anchor.set(0.5);
        halo.tint = 0xff7a1a;
        halo.alpha = 0.35;
        halo.scale.set(2.2);
        halo.blendMode = 'add';
        const hole = new PIXI.Graphics().circle(0, 0, scene.stars[0].s).fill({ color: 0x000000 });
        system.world.addChild(halo, disk, hole);
        system.blackHole = disk;
    } else {
        scene.stars.forEach((s, i) => {
            const root = new PIXI.Container();
            const glow = new PIXI.Sprite(tex.glow);
            glow.anchor.set(0.5);
            glow.tint = s.c;
            glow.alpha = 0.7;
            glow.scale.set(s.s * 0.09);
            glow.blendMode = 'add';
            const core = new PIXI.Sprite(tex.dot);
            core.anchor.set(0.5);
            core.tint = lighten(s.c);
            core.scale.set(s.s / 8);
            root.addChild(glow, core);
            system.world.addChild(root);
            system.stars.push({ root, phase: i * Math.PI, distance: scene.stars.length > 1 ? 10 + s.s * 0.6 : 0 });
        });
    }

    for (const p of scene.planets) {
        const root = new PIXI.Container();
        if (p.ringed) {
            root.addChild(new PIXI.Graphics()
                .ellipse(0, 0, p.s * 2, p.s * 0.6).stroke({ width: 1.2, color: 0xe8d3a8, alpha: 0.6 }));
        }
        const body = new PIXI.Sprite(tex.dot);
        body.anchor.set(0.5);
        body.tint = p.c;
        body.scale.set(p.s / 8);
        root.addChild(body);
        system.world.addChild(root);
        system.planets.push({ data: p, root, x: 0, y: 0 });
    }

    system.selectionRing = new PIXI.Graphics();
    system.selectionRing.visible = false;
    system.world.addChild(system.selectionRing);
}

function tickSystem(dt) {
    for (const s of system.stars) {
        const a = s.phase + elapsed * 0.6;
        s.root.position.set(Math.cos(a) * s.distance, Math.sin(a) * s.distance);
    }
    if (system.blackHole) {
        system.blackHole.rotation += dt * 0.15;
    }

    let selected = null;
    for (const p of system.planets) {
        const a = p.data.phase + elapsed * p.data.speed;
        p.x = Math.cos(a) * p.data.orbit;
        p.y = Math.sin(a) * p.data.orbit;
        p.root.position.set(p.x, p.y);
        if (p.data.index === system.selected) selected = p;
    }

    if (selected) {
        system.selectionRing.position.set(selected.x, selected.y);
        for (const m of system.moons) {
            const a = m.data.phase + elapsed * m.data.speed;
            m.sprite.position.set(selected.x + Math.cos(a) * m.data.orbit, selected.y + Math.sin(a) * m.data.orbit);
        }
    }
}

function fitSystem() {
    const w = app.screen.width - (app.screen.width > 800 ? PANEL_WIDTH : 0);
    const h = app.screen.height;
    const scale = Math.min(w, h) / 900;
    system.world.scale.set(scale);
    system.world.position.set(w / 2, h / 2);
}

function pickPlanet(globalX, globalY) {
    const p = system.world.toLocal({ x: globalX, y: globalY });
    const limit = PICK_RADIUS_PX / system.world.scale.x;
    let best = null, bestD = Infinity;
    for (const planet of system.planets) {
        const dx = planet.x - p.x, dy = planet.y - p.y, d = Math.sqrt(dx * dx + dy * dy);
        if (d < Math.max(limit, planet.data.s + 4) && d < bestD) {
            bestD = d;
            best = planet;
        }
    }
    return best;
}

// ---------------------------------------------------------------------------------------------
// Input: drag to pan, wheel to zoom, click to select, hover for a tooltip
// ---------------------------------------------------------------------------------------------

function activeWorld() {
    return mode === 'galaxy' ? galaxy.world : system.world;
}

function bindInput() {
    const canvas = app.canvas;

    canvas.addEventListener('pointerdown', ev => {
        pointer.down = true;
        pointer.dragging = false;
        pointer.startX = pointer.lastX = ev.offsetX;
        pointer.startY = pointer.lastY = ev.offsetY;
        canvas.setPointerCapture(ev.pointerId);
    });

    canvas.addEventListener('pointermove', ev => {
        if (pointer.down) {
            const moved = Math.hypot(ev.offsetX - pointer.startX, ev.offsetY - pointer.startY);
            if (moved > CLICK_TOLERANCE_PX) pointer.dragging = true;
            if (pointer.dragging) {
                const world = activeWorld();
                world.position.x += ev.offsetX - pointer.lastX;
                world.position.y += ev.offsetY - pointer.lastY;
                hideTooltip();
            }
            pointer.lastX = ev.offsetX;
            pointer.lastY = ev.offsetY;
            return;
        }
        updateHover(ev.offsetX, ev.offsetY);
    });

    canvas.addEventListener('pointerup', ev => {
        const wasClick = pointer.down && !pointer.dragging;
        pointer.down = false;
        pointer.dragging = false;
        if (!wasClick) return;

        if (mode === 'galaxy') {
            const star = pickStar(ev.offsetX, ev.offsetY);
            if (star) dotnet.invokeMethodAsync('OnStarClicked', star.data.n);
        } else {
            const planet = pickPlanet(ev.offsetX, ev.offsetY);
            if (planet) dotnet.invokeMethodAsync('OnPlanetClicked', planet.data.index);
        }
    });

    canvas.addEventListener('pointerleave', hideTooltip);

    canvas.addEventListener('wheel', ev => {
        ev.preventDefault();
        const world = activeWorld();
        const factor = Math.exp(-ev.deltaY * 0.0015);
        const next = Math.min(Math.max(world.scale.x * factor, 0.05), 400);
        const f = next / world.scale.x;
        // Zoom around the cursor: the point under it stays still.
        world.position.x = ev.offsetX - (ev.offsetX - world.position.x) * f;
        world.position.y = ev.offsetY - (ev.offsetY - world.position.y) * f;
        world.scale.set(next);
    }, { passive: false });
}

function updateHover(x, y) {
    if (mode === 'galaxy') {
        const star = pickStar(x, y);
        if (star) {
            const cls = `${texts.class} ${star.data.cls}`;
            const kind = star.data.k === 1 ? texts.blackHole : star.data.k === 2 ? `${cls} · ${texts.binary}` : cls;
            showTooltip(x, y, `<b>${escapeHtml(star.data.name)}</b><br>${texts.block} #${star.data.n.toLocaleString(texts.locale)} · ${kind}`);
            app.canvas.style.cursor = 'pointer';
            return;
        }
    } else {
        const planet = pickPlanet(x, y);
        if (planet) {
            showTooltip(x, y, `<b>${escapeHtml(planet.data.name)}</b>`);
            app.canvas.style.cursor = 'pointer';
            return;
        }
    }
    hideTooltip();
}

function showTooltip(x, y, html) {
    tooltip.innerHTML = html;
    tooltip.style.transform = `translate(${x + 14}px, ${y + 14}px)`;
    tooltip.style.opacity = '1';
}

function hideTooltip() {
    if (tooltip) tooltip.style.opacity = '0';
    if (app) app.canvas.style.cursor = 'grab';
}

// ---------------------------------------------------------------------------------------------
// Frame loop, backdrop, textures, helpers
// ---------------------------------------------------------------------------------------------

function tick(ticker) {
    const dt = ticker.deltaMS / 1000;
    elapsed += dt;
    if (mode === 'galaxy') tickGalaxy(dt);
    else tickSystem(dt);
}

let backdrop;

function buildBackdrop() {
    // Faint fixed background stars (decoration only, not blocks). Seeded so they never jump around.
    backdrop?.destroy();
    backdrop = new PIXI.Graphics();
    let seed = 1234567;
    const rand = () => (seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648;
    const w = app.screen.width, h = app.screen.height;
    for (let i = 0; i < 450; i++) {
        backdrop.circle(rand() * w, rand() * h, rand() * 0.9 + 0.2).fill({ color: 0xffffff, alpha: 0.08 + rand() * 0.25 });
    }
    app.stage.addChildAt(backdrop, 0);
}

function onResize() {
    // resizeTo updates the canvas size on the next frame.
    requestAnimationFrame(() => {
        buildBackdrop();
        if (mode === 'system') fitSystem();
    });
}

function makeTextures() {
    tex.glow = PIXI.Texture.from(radial(128, [[0, 'rgba(255,255,255,1)'], [0.15, 'rgba(255,255,255,0.55)'], [0.45, 'rgba(255,255,255,0.12)'], [1, 'rgba(255,255,255,0)']]));
    tex.dot = PIXI.Texture.from(radial(16, [[0, 'rgba(255,255,255,1)'], [0.75, 'rgba(255,255,255,1)'], [1, 'rgba(255,255,255,0)']]));
    tex.ring = PIXI.Texture.from(radial(128, [[0, 'rgba(255,255,255,0)'], [0.32, 'rgba(255,255,255,0)'], [0.42, 'rgba(255,255,255,1)'], [0.6, 'rgba(255,255,255,0.35)'], [1, 'rgba(255,255,255,0)']]));
}

function radial(size, stops) {
    const c = document.createElement('canvas');
    c.width = c.height = size;
    const ctx = c.getContext('2d');
    const grad = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
    for (const [at, color] of stops) grad.addColorStop(at, color);
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, size, size);
    return c;
}

function lighten(color) {
    const r = (color >> 16) & 255, g = (color >> 8) & 255, b = color & 255;
    const mix = v => Math.round(v + (255 - v) * 0.55);
    return (mix(r) << 16) | (mix(g) << 8) | mix(b);
}

function escapeHtml(s) {
    return s.replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
}
