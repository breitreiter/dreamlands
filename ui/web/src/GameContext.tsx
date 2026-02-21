import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import type { GameResponse } from "./api/types";
import * as api from "./api/client";

interface GameState {
  gameId: string | null;
  response: GameResponse | null;
  loading: boolean;
  error: string | null;
}

interface GameContextValue extends GameState {
  startNewGame: () => Promise<void>;
  doAction: (body: {
    action: string;
    direction?: string;
    choiceIndex?: number;
    itemId?: string;
    quantity?: number;
  }) => Promise<GameResponse | null>;
  setResponse: (r: GameResponse) => void;
  clearError: () => void;
}

const GameContext = createContext<GameContextValue | null>(null);

export function GameProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<GameState>({
    gameId: null,
    response: null,
    loading: false,
    error: null,
  });

  const startNewGame = useCallback(async () => {
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const result = await api.newGame();
      setState({
        gameId: result.gameId,
        response: result.state,
        loading: false,
        error: null,
      });
    } catch (e) {
      setState((s) => ({
        ...s,
        loading: false,
        error: e instanceof Error ? e.message : "Unknown error",
      }));
    }
  }, []);

  const doAction = useCallback(
    async (body: {
      action: string;
      direction?: string;
      choiceIndex?: number;
      itemId?: string;
      quantity?: number;
    }): Promise<GameResponse | null> => {
      if (!state.gameId) return null;
      setState((s) => ({ ...s, loading: true, error: null }));
      try {
        const result = await api.action(state.gameId, body);
        setState((s) => ({ ...s, response: result, loading: false }));
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

  const setResponse = useCallback((r: GameResponse) => {
    setState((s) => ({ ...s, response: r }));
  }, []);

  const clearError = useCallback(() => {
    setState((s) => ({ ...s, error: null }));
  }, []);

  return (
    <GameContext.Provider
      value={{ ...state, startNewGame, doAction, setResponse, clearError }}
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
