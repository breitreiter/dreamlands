/** Client-side A* pathfinding over the map.json grid. */

export interface MapNode {
  x: number;
  y: number;
  terrain: string;
}

export interface MapData {
  width: number;
  height: number;
  nodes: MapNode[];
}

/** Parsed grid: terrain lookup by [y * width + x]. */
let grid: string[] = [];
let gridWidth = 0;
let gridHeight = 0;

export function loadGrid(map: MapData) {
  gridWidth = map.width;
  gridHeight = map.height;
  grid = new Array(gridWidth * gridHeight).fill("Lake");
  for (const n of map.nodes) {
    grid[n.y * gridWidth + n.x] = n.terrain;
  }
}

function walkable(x: number, y: number): boolean {
  if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight) return false;
  return grid[y * gridWidth + x] !== "Lake";
}

const DIRS = [
  [1, 0],
  [-1, 0],
  [0, 1],
  [0, -1],
];

/** A* from (fx,fy) to (tx,ty). Returns list of {x,y} from start to end, or null if unreachable. */
export function findPath(
  fx: number,
  fy: number,
  tx: number,
  ty: number
): { x: number; y: number }[] | null {
  if (!walkable(tx, ty)) return null;

  const key = (x: number, y: number) => y * gridWidth + x;
  const h = (x: number, y: number) => Math.abs(x - tx) + Math.abs(y - ty);

  const startKey = key(fx, fy);
  const goalKey = key(tx, ty);
  if (startKey === goalKey) return [{ x: fx, y: fy }];

  // Min-heap using sorted insertion (grid is small — 10k nodes max)
  const open: { k: number; f: number; g: number; x: number; y: number }[] = [];
  const gScore = new Map<number, number>();
  const cameFrom = new Map<number, number>();

  gScore.set(startKey, 0);
  open.push({ k: startKey, f: h(fx, fy), g: 0, x: fx, y: fy });

  while (open.length > 0) {
    // Pop lowest f-score
    let bestIdx = 0;
    for (let i = 1; i < open.length; i++) {
      if (open[i].f < open[bestIdx].f) bestIdx = i;
    }
    const cur = open[bestIdx];
    open[bestIdx] = open[open.length - 1];
    open.pop();

    if (cur.k === goalKey) {
      // Reconstruct path
      const path: { x: number; y: number }[] = [];
      let k = goalKey;
      while (k !== startKey) {
        path.push({ x: k % gridWidth, y: Math.floor(k / gridWidth) });
        k = cameFrom.get(k)!;
      }
      path.push({ x: fx, y: fy });
      path.reverse();
      return path;
    }

    for (const [dx, dy] of DIRS) {
      const nx = cur.x + dx;
      const ny = cur.y + dy;
      if (!walkable(nx, ny)) continue;

      const nk = key(nx, ny);
      const ng = cur.g + 1;
      const prev = gScore.get(nk);
      if (prev !== undefined && ng >= prev) continue;

      gScore.set(nk, ng);
      cameFrom.set(nk, cur.k);
      open.push({ k: nk, f: ng + h(nx, ny), g: ng, x: nx, y: ny });
    }
  }

  return null;
}
