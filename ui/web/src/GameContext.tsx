import { createContext, useContext, useState, useCallback, useEffect, useRef, type ReactNode } from "react";
import type { GameResponse, MarketOrder } from "./api/types";
import * as api from "./api/client";

const SAVE_KEY = "dreamlands_gameId";

export interface ToastLine {
  text: string;
  color?: "positive" | "negative";
}

export interface ToastData {
  lines: ToastLine[];
}

interface GameState {
  gameId: string | null;
  response: GameResponse | null;
  loading: boolean;
  error: string | null;
  toast: ToastData | null;
}

interface GameContextValue extends GameState {
  startNewGame: () => Promise<void>;
  resumeGame: () => Promise<void>;
  hasSavedGame: boolean;
  refreshState: () => Promise<void>;
  doAction: (body: {
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
  }) => Promise<GameResponse | null>;
  clearError: () => void;
  showToast: (data: ToastData) => void;
  clearToast: () => void;
}

const GameContext = createContext<GameContextValue | null>(null);

function stripNulls<T extends object>(obj: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(obj).filter(([, v]) => v != null)
  ) as Partial<T>;
}

function clearStale(result: GameResponse): Partial<GameResponse> {
  const cleared: Partial<GameResponse> = {};
  if (result.mode === "encounter") cleared.outcome = undefined;
  if (result.mode === "outcome") cleared.encounter = undefined;
  if (result.mode === "exploring") {
    cleared.encounter = undefined;
    cleared.outcome = undefined;
  }
  // Deliveries are one-shot — clear unless the response explicitly includes them
  if (!result.deliveries) cleared.deliveries = undefined;
  return cleared;
}

export function GameProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<GameState>({
    gameId: null,
    response: null,
    loading: false,
    error: null,
    toast: null,
  });

  const savedGameId = localStorage.getItem(SAVE_KEY);

  useEffect(() => {
    api.checkVersion().then((err) => {
      if (err) setState((s) => ({ ...s, error: err }));
    });
  }, []);

  const startNewGame = useCallback(async () => {
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const result = await api.newGame();
      localStorage.setItem(SAVE_KEY, result.gameId);
      setState({
        gameId: result.gameId,
        response: result.state,
        loading: false,
        error: null,
        toast: null,
      });
    } catch (e) {
      setState((s) => ({
        ...s,
        loading: false,
        error: e instanceof Error ? e.message : "Unknown error",
      }));
    }
  }, []);

  const resumeGame = useCallback(async () => {
    if (!savedGameId) return;
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const result = await api.getGame(savedGameId);
      setState({ gameId: savedGameId, response: result, loading: false, error: null, toast: null });
    } catch {
      localStorage.removeItem(SAVE_KEY);
      setState((s) => ({ ...s, loading: false, error: null }));
    }
  }, [savedGameId]);

  const refreshState = useCallback(async () => {
    if (!state.gameId) return;
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const result = await api.getGame(state.gameId);
      setState((s) => ({ ...s, response: result, loading: false }));
    } catch (e) {
      setState((s) => ({
        ...s,
        loading: false,
        error: e instanceof Error ? e.message : "Unknown error",
      }));
    }
  }, [state.gameId]);

  const doAction = useCallback(
    async (body: {
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
    }): Promise<GameResponse | null> => {
      if (!state.gameId) return null;
      setState((s) => ({ ...s, loading: true, error: null }));
      try {
        const result = await api.action(state.gameId, body);
        setState((s) => ({
          ...s,
          response: s.response
            ? { ...s.response, ...clearStale(result), ...stripNulls(result) }
            : result,
          loading: false,
        }));
        return result;
      } catch (e) {
        setState((s) => ({
          ...s,
          loading: false,
          error: e instanceof Error ? e.message : "Unknown error",
        }));
        return null;
      }
    },
    [state.gameId]
  );

  const clearError = useCallback(() => {
    setState((s) => ({ ...s, error: null }));
  }, []);

  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showToast = useCallback((data: ToastData) => {
    if (toastTimer.current) clearTimeout(toastTimer.current);
    setState((s) => ({ ...s, toast: data }));
    toastTimer.current = setTimeout(() => {
      setState((s) => ({ ...s, toast: null }));
      toastTimer.current = null;
    }, 4500);
  }, []);

  const clearToast = useCallback(() => {
    if (toastTimer.current) { clearTimeout(toastTimer.current); toastTimer.current = null; }
    setState((s) => ({ ...s, toast: null }));
  }, []);

  return (
    <GameContext.Provider
      value={{ ...state, startNewGame, resumeGame, hasSavedGame: !!savedGameId, refreshState, doAction, clearError, showToast, clearToast }}
    >
      {children}
    </GameContext.Provider>
  );
}

export function useGame(): GameContextValue {
  const ctx = useContext(GameContext);
  if (!ctx) throw new Error("useGame must be used within GameProvider");
  return ctx;
}
