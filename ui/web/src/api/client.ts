import type {
  GameResponse,
  NewGameResponse,
  MarketStockResponse,
  MarketOrder,
  CampResolveChoices,
} from "./types";

const BASE = "/api/game";

async function post<T>(url: string, body?: object): Promise<T> {
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error || res.statusText);
  }
  return res.json();
}

async function get<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error || res.statusText);
  }
  return res.json();
}

export async function newGame(): Promise<NewGameResponse> {
  return post(`${BASE}/new`);
}

export async function getGame(id: string): Promise<GameResponse> {
  return get(`${BASE}/${id}`);
}

export async function action(
  id: string,
  body: {
    action: string;
    direction?: string;
    choiceIndex?: number;
    itemId?: string;
    quantity?: number;
    order?: MarketOrder;
    campChoices?: CampResolveChoices;
  }
): Promise<GameResponse> {
  return post(`${BASE}/${id}/action`, body);
}

export async function getMarketStock(
  id: string
): Promise<MarketStockResponse> {
  return get(`${BASE}/${id}/market`);
}
