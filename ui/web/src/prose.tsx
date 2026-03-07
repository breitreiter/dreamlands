import type { ReactNode } from "react";

/** Parse **bold** and *italic* markers in prose text, returning React nodes. */
export function formatProse(text: string): ReactNode[] {
  // Match **bold** first (greedy inner), then *italic*
  const pattern = /(\*\*(.+?)\*\*|\*(.+?)\*)/g;
  const nodes: ReactNode[] = [];
  let last = 0;
  let key = 0;

  for (const match of text.matchAll(pattern)) {
    const idx = match.index!;
    if (idx > last) nodes.push(text.slice(last, idx));

    if (match[2] != null) {
      nodes.push(<strong key={key++}>{match[2]}</strong>);
    } else {
      nodes.push(<em key={key++}>{match[3]}</em>);
    }
    last = idx + match[0].length;
  }

  if (last < text.length) nodes.push(text.slice(last));
  return nodes;
}
