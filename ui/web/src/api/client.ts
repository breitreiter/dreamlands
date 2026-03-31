import type {
  GameResponse,
  NewGameResponse,
  MarketStockResponse,
  MarketOrder,
  InnQuoteResponse,
  BankResponse,
  DiscoveryInfo,
  NoticesResponse,
} from "./types";

declare const __API_VERSION__: string;
declare const __API_BASE__: string;

const BASE = `${__API_BASE__}/api/game`;

/** Fire-and-forget ping to wake the backend from cold start. */
export function nudge(): void {
  fetch(`${__API_BASE__}/api/health`).catch(() => {});
}

export async function checkVersion(): Promise<string | null> {
  try {
    const res = await fetch(`${__API_BASE__}/api/health`);
    if (!res.ok) return "Server unreachable";
    const data = await res.json();
    if (data.apiVersion !== __API_VERSION__)
      return `Server API version mismatch: server is v${data.apiVersion}, UI expects v${__API_VERSION__}. Restart the server.`;
    return null;
  } catch {
    return "Server unreachable";
  }
}

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
    slot?: string;
    quantity?: number;
    order?: MarketOrder;
    source?: string;
    bankIndex?: number;
    offerIndex?: number;
    offerId?: string;
    encounterId?: string;
    path?: { x: number; y: number }[];
  }
): Promise<GameResponse> {
  return post(`${BASE}/${id}/action`, body);
}

export async function getMarketStock(
  id: string
): Promise<MarketStockResponse> {
  return get(`${BASE}/${id}/market`);
}

export async function getBank(id: string): Promise<BankResponse> {
  return get(`${BASE}/${id}/bank`);
}

export async function getDiscoveries(
  id: string
): Promise<DiscoveryInfo[]> {
  return get(`${BASE}/${id}/discoveries`);
}

export async function getNotices(
  id: string
): Promise<NoticesResponse> {
  return get(`${BASE}/${id}/notices`);
}

export async function getInnQuote(
  id: string
): Promise<InnQuoteResponse> {
  return get(`${BASE}/${id}/inn`);
}
