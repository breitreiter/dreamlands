import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from "react";
import type { GameResponse, MarketOrder } from "./api/types";
import * as api from "./api/client";

const SAVE_KEY = "dreamlands_gameId";

export interface CampReportLine {
  text: string;
  color?: "positive" | "negative";
}

export interface CampReport {
  lines: CampReportLine[];
  severity: "ok" | "bad";
}

interface GameState {
  gameId: string | null;
  response: GameResponse | null;
  loading: boolean;
  error: string | null;
  campReport: CampReport | null;
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
    approach?: string;
    tacticalAction?: string;
    openingIndex?: number;
    path?: { x: number; y: number }[];
  }) => Promise<GameResponse | null>;
  clearError: () => void;
  setCampReport: (report: CampReport) => void;
  clearCampReport: () => void;
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
    cleared.tactical = undefined;
  }
  if (result.mode === "tactical") {
    cleared.encounter = undefined;
    cleared.outcome = undefined;
  }
  // One-shot fields — clear unless the response explicitly includes them
  if (!result.deliveries) cleared.deliveries = undefined;
  if (!result.travel) cleared.travel = undefined;
  return cleared;
}

export function GameProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<GameState>({
    gameId: null,
    response: null,
    loading: false,
    error: null,
    campReport: null,
  });

  const savedGameId = localStorage.getItem(SAVE_KEY);

  // Wake the backend from cold start while the user reads the splash screen
  useEffect(() => {
    api.nudge();
  }, []);

  const startNewGame = useCallback(async () => {
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const versionErr = await api.checkVersion();
      if (versionErr) {
        setState((s) => ({ ...s, loading: false, error: versionErr }));
        return;
      }
      const result = await api.newGame();
      localStorage.setItem(SAVE_KEY, result.gameId);
      setState({
        gameId: result.gameId,
        response: result.state,
        loading: false,
        error: null,
        campReport: null,
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
      const versionErr = await api.checkVersion();
      if (versionErr) {
        setState((s) => ({ ...s, loading: false, error: versionErr }));
        return;
      }
      const result = await api.getGame(savedGameId);
      setState({ gameId: savedGameId, response: result, loading: false, error: null, campReport: null });
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
      path?: { x: number; y: number }[];
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

  const setCampReport = useCallback((report: CampReport) => {
    setState((s) => ({ ...s, campReport: report }));
  }, []);

  const clearCampReport = useCallback(() => {
    setState((s) => ({ ...s, campReport: null }));
  }, []);

  return (
    <GameContext.Provider
      value={{ ...state, startNewGame, resumeGame, hasSavedGame: !!savedGameId, refreshState, doAction, clearError, setCampReport, clearCampReport }}
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
