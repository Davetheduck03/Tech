import { useState } from "react";

// ── Tile definitions ──────────────────────────────────────────────────────────
const TILES = {
  X: { name: "Blocked",   color: "#252525", border: "#111",   text: "#555",   label: "X",  desc: "Not walkable, not buildable" },
  P: { name: "Path",      color: "#b8893e", border: "#8a6220", text: "#fff",  label: "·",  desc: "Walkable only — enemy route" },
  B: { name: "Buildable", color: "#3a7a3a", border: "#256025", text: "#fff",  label: "B",  desc: "Tower spots only, blocks path" },
  S: { name: "Spawn",     color: "#2860c0", border: "#1a3f90", text: "#fff",  label: "S",  desc: "Enemy spawn point" },
  E: { name: "Exit",      color: "#c03030", border: "#8a1515", text: "#fff",  label: "E",  desc: "Enemy exit / player base" },
  H: { name: "Hybrid",    color: "#c49520", border: "#8a6510", text: "#fff",  label: "H",  desc: "Walkable + buildable — tower blocks path" },
  ".": { name: "Empty",   color: "#606060", border: "#404040", text: "#aaa",  label: "",   desc: "Walkable + buildable (default)" },
};

// ── Map data ──────────────────────────────────────────────────────────────────
// rows[0] = top of display = highest Y coordinate
const MAPS = [
  {
    id: 1,
    name: "The Serpent",
    size: "10 × 10",
    spawns: 1,
    exits: 1,
    strategy: "Long S-curve gives every tower maximum exposure time. Build on the Buildable strips hugging each bend — the inner corners at (2,1–4) and (7,6–8) are prime spots.",
    // top=y9 → bottom=y0
    rows: [
      "XXXXXXXXXE",  // y=9
      "XBBBBBPPPP",  // y=8
      "XBXXXBPBBX",  // y=7
      "XBXXXBPBBX",  // y=6
      "XBBPPPPBBX",  // y=5
      "XXBPBBBBXX",  // y=4
      "XXBPBXXXXX",  // y=3
      "XXBPBXXXXX",  // y=2
      "XXBPBBBBBX",  // y=1
      "XSPPXXXXXX",  // y=0
    ],
  },
  {
    id: 2,
    name: "Pincer",
    size: "12 × 10",
    spawns: 2,
    exits: 1,
    strategy: "Two enemy streams merge at (4,4) before funnelling right. The H tiles at (5,5) and (5,3) are shortcut tiles — leave them open and enemies bypass (4,4), or build towers on both to force every enemy through the central chokepoint.",
    // top=y9 → bottom=y0
    rows: [
      "XXXXXXXXXXXX",  // y=9
      "XBBBBBBBBBBX",  // y=8
      "SPPPPBBBBBBX",  // y=7
      "XBBBPBBBBBBX",  // y=6
      "XBBBPHBBBBBX",  // y=5
      "XBBBPPPPPPPE",  // y=4
      "XBBBPHBBBBBX",  // y=3
      "SPPPPBBBBBBX",  // y=2
      "XBBBBBBBBBBX",  // y=1
      "XXXXXXXXXXXX",  // y=0
    ],
  },
];

// ── Grid component ────────────────────────────────────────────────────────────
function MapGrid({ map, cellSize }) {
  const [hovered, setHovered] = useState(null);
  const height = map.rows.length;
  const width  = map.rows[0].length;

  return (
    <div>
      {/* Y-axis label + grid */}
      <div style={{ display: "flex", gap: 4, alignItems: "flex-start" }}>
        {/* Y labels */}
        <div style={{ display: "flex", flexDirection: "column", paddingTop: 1 }}>
          {map.rows.map((_, ri) => {
            const y = height - 1 - ri;
            return (
              <div
                key={ri}
                style={{
                  height: cellSize,
                  width: 20,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "flex-end",
                  fontSize: 9,
                  color: "#666",
                  paddingRight: 3,
                  fontFamily: "monospace",
                }}
              >
                {y}
              </div>
            );
          })}
        </div>

        {/* Grid cells */}
        <div>
          <div style={{ display: "inline-block", border: "1px solid #333" }}>
            {map.rows.map((row, ri) => {
              const y = height - 1 - ri;
              return (
                <div key={ri} style={{ display: "flex" }}>
                  {row.split("").map((ch, x) => {
                    const tile = TILES[ch] || TILES["."];
                    const isHov = hovered && hovered.x === x && hovered.y === y;
                    return (
                      <div
                        key={x}
                        onMouseEnter={() => setHovered({ x, y, tile })}
                        onMouseLeave={() => setHovered(null)}
                        style={{
                          width: cellSize,
                          height: cellSize,
                          background: isHov
                            ? lighten(tile.color, 0.28)
                            : tile.color,
                          border: `1px solid ${tile.border}`,
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          fontSize: cellSize < 26 ? 8 : 10,
                          fontWeight: "bold",
                          color: tile.text,
                          cursor: "default",
                          boxSizing: "border-box",
                          transition: "background 0.08s",
                          userSelect: "none",
                          fontFamily: "monospace",
                        }}
                      >
                        {tile.label}
                      </div>
                    );
                  })}
                </div>
              );
            })}
          </div>

          {/* X labels */}
          <div style={{ display: "flex", marginTop: 2 }}>
            {map.rows[0].split("").map((_, x) => (
              <div
                key={x}
                style={{
                  width: cellSize,
                  textAlign: "center",
                  fontSize: 9,
                  color: "#666",
                  fontFamily: "monospace",
                }}
              >
                {x}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Hover tooltip */}
      <div
        style={{
          marginTop: 6,
          height: 22,
          fontSize: 11,
          color: hovered ? "#ddd" : "#444",
          fontFamily: "monospace",
          paddingLeft: 24,
        }}
      >
        {hovered
          ? `(${hovered.x}, ${hovered.y})  →  ${hovered.tile.name} — ${hovered.tile.desc}`
          : "Hover a cell to inspect"}
      </div>
    </div>
  );
}

// ── Legend ────────────────────────────────────────────────────────────────────
function Legend() {
  const entries = Object.entries(TILES).filter(([k]) => k !== ".");
  return (
    <div style={{ display: "flex", flexWrap: "wrap", gap: "6px 14px", marginTop: 8 }}>
      {entries.map(([key, t]) => (
        <div key={key} style={{ display: "flex", alignItems: "center", gap: 5 }}>
          <div
            style={{
              width: 14,
              height: 14,
              background: t.color,
              border: `1px solid ${t.border}`,
              borderRadius: 2,
              flexShrink: 0,
            }}
          />
          <span style={{ fontSize: 11, color: "#ccc", fontFamily: "monospace" }}>
            <b style={{ color: "#fff" }}>{t.name}</b>
          </span>
        </div>
      ))}
    </div>
  );
}

// ── Tile count badge strip ────────────────────────────────────────────────────
function TileCounts({ rows }) {
  const counts = {};
  rows.forEach(row => row.split("").forEach(ch => { counts[ch] = (counts[ch] || 0) + 1; }));
  return (
    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginTop: 4 }}>
      {Object.entries(counts)
        .filter(([k]) => TILES[k])
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([key, n]) => {
          const t = TILES[key];
          return (
            <div
              key={key}
              style={{
                background: t.color,
                border: `1px solid ${t.border}`,
                borderRadius: 4,
                padding: "1px 8px",
                fontSize: 10,
                color: t.text,
                fontFamily: "monospace",
                fontWeight: "bold",
              }}
            >
              {t.name}: {n}
            </div>
          );
        })}
    </div>
  );
}

// ── Colour helper ─────────────────────────────────────────────────────────────
function lighten(hex, amt) {
  const n = parseInt(hex.replace("#", ""), 16);
  const r = Math.min(255, ((n >> 16) & 0xff) + Math.round(255 * amt));
  const g = Math.min(255, ((n >> 8)  & 0xff) + Math.round(255 * amt));
  const b = Math.min(255, ( n        & 0xff) + Math.round(255 * amt));
  return `rgb(${r},${g},${b})`;
}

// ── Main ──────────────────────────────────────────────────────────────────────
export default function App() {
  const [active, setActive] = useState(0);
  const map = MAPS[active];

  // Cell size: shrink for wider maps
  const cellSize = map.rows[0].length > 10 ? 30 : 34;

  return (
    <div
      style={{
        background: "#1a1a1a",
        minHeight: "100vh",
        padding: "24px 28px",
        fontFamily: "sans-serif",
        color: "#ddd",
      }}
    >
      <h2 style={{ margin: "0 0 4px", color: "#fff", fontSize: 18 }}>
        Tower Defense — Map Designs
      </h2>
      <p style={{ margin: "0 0 18px", fontSize: 12, color: "#777" }}>
        Paint these in the Grid Map Editor. Hover any cell to see its type and coordinates.
      </p>

      {/* Map tabs */}
      <div style={{ display: "flex", gap: 8, marginBottom: 18 }}>
        {MAPS.map((m, i) => (
          <button
            key={i}
            onClick={() => setActive(i)}
            style={{
              padding: "6px 18px",
              background: active === i ? "#3a6abf" : "#2a2a2a",
              border: `1px solid ${active === i ? "#5588dd" : "#444"}`,
              borderRadius: 6,
              color: active === i ? "#fff" : "#aaa",
              cursor: "pointer",
              fontSize: 13,
              fontWeight: active === i ? "bold" : "normal",
            }}
          >
            {m.id}. {m.name}
          </button>
        ))}
      </div>

      {/* Map card */}
      <div
        style={{
          background: "#242424",
          borderRadius: 10,
          border: "1px solid #333",
          padding: "18px 20px",
        }}
      >
        {/* Header */}
        <div style={{ display: "flex", alignItems: "baseline", gap: 12, marginBottom: 6 }}>
          <h3 style={{ margin: 0, fontSize: 17, color: "#fff" }}>
            Map {map.id}: "{map.name}"
          </h3>
          <span style={{ fontSize: 12, color: "#888", fontFamily: "monospace" }}>
            {map.size}
          </span>
          <span style={{ fontSize: 11, color: "#4a90d9" }}>
            {map.spawns === 1 ? "1 Spawn" : `${map.spawns} Spawns`}
          </span>
          <span style={{ fontSize: 11, color: "#c04040" }}>
            {map.exits === 1 ? "1 Exit" : `${map.exits} Exits`}
          </span>
        </div>

        {/* Tile counts */}
        <TileCounts rows={map.rows} />

        {/* Grid */}
        <div style={{ marginTop: 16 }}>
          <MapGrid map={map} cellSize={cellSize} />
        </div>

        {/* Strategy tip */}
        <div
          style={{
            marginTop: 14,
            background: "#1e2a1e",
            border: "1px solid #2d4a2d",
            borderRadius: 6,
            padding: "10px 14px",
            fontSize: 12,
            color: "#9dc99d",
            lineHeight: 1.6,
          }}
        >
          <b style={{ color: "#7ecf7e" }}>Strategy tip:</b> {map.strategy}
        </div>

        {/* Path description */}
        <PathDescription map={map} />
      </div>

      {/* Legend */}
      <div style={{ marginTop: 20 }}>
        <div style={{ fontSize: 11, color: "#666", marginBottom: 4 }}>TILE LEGEND</div>
        <Legend />
      </div>
    </div>
  );
}

// ── Path description panel ────────────────────────────────────────────────────
function PathDescription({ map }) {
  const paths = map.id === 1
    ? [
        {
          label: "Enemy path",
          color: "#b8893e",
          steps: "S(1,0) → right → P(2,0)→P(3,0) → up → P(3,1)–P(3,5) → right → P(4,5)→P(5,5)→P(6,5) → up → P(6,6)→P(6,7)→P(6,8) → right → P(7,8)→P(8,8)→P(9,8) → up → E(9,9)",
        },
      ]
    : [
        {
          label: "Top spawn path",
          color: "#2860c0",
          steps: "S(0,7) → right → P(1,7)→…→P(4,7) → down → P(4,6)→P(4,5)→P(4,4) → right to → E(11,4)",
        },
        {
          label: "Bottom spawn path",
          color: "#c03030",
          steps: "S(0,2) → right → P(1,2)→…→P(4,2) → up → P(4,3)→P(4,4) → right to → E(11,4)",
        },
        {
          label: "H-tile note",
          color: "#c49520",
          steps: "H(5,5) and H(5,3) each offer a 1-tile shortcut bypassing (4,4). Build towers on both to force all enemies through the central choke.",
        },
      ];

  return (
    <div style={{ marginTop: 10, display: "flex", flexDirection: "column", gap: 6 }}>
      {paths.map((p, i) => (
        <div
          key={i}
          style={{
            background: "#1c1c2a",
            border: `1px solid ${p.color}44`,
            borderLeft: `3px solid ${p.color}`,
            borderRadius: 5,
            padding: "7px 12px",
            fontSize: 11,
            color: "#aaa",
            fontFamily: "monospace",
            lineHeight: 1.5,
          }}
        >
          <b style={{ color: p.color }}>{p.label}:</b> {p.steps}
        </div>
      ))}
    </div>
  );
}
