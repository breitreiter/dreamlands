import type { ReactNode } from "react";

type ProseBlock =
  | { kind: "text"; content: string }
  | { kind: "inscription"; content: string };

/** Split prose into plain text and """inscription""" blocks. */
function splitBlocks(text: string): ProseBlock[] {
  const lines = text.split("\n");
  const blocks: ProseBlock[] = [];
  let current: string[] = [];
  let inInscription = false;

  for (const line of lines) {
    if (line.trim() === '"""') {
      if (inInscription) {
        blocks.push({ kind: "inscription", content: current.join("\n") });
        current = [];
        inInscription = false;
      } else {
        if (current.length > 0) {
          blocks.push({ kind: "text", content: current.join("\n") });
          current = [];
        }
        inInscription = true;
      }
    } else {
      current.push(line);
    }
  }

  if (current.length > 0) {
    blocks.push({ kind: inInscription ? "inscription" : "text", content: current.join("\n") });
  }

  return blocks;
}

/** Parse **bold** and *italic* markers in a string, returning React nodes. */
function formatInline(text: string): ReactNode[] {
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

/** Parse prose text into React nodes, handling """inscription""" blocks and inline markdown. */
export function formatProse(text: string): ReactNode[] {
  const blocks = splitBlocks(text);
  const nodes: ReactNode[] = [];

  for (let i = 0; i < blocks.length; i++) {
    const block = blocks[i];
    if (block.kind === "inscription") {
      nodes.push(
        <blockquote
          key={`ins-${i}`}
          className="font-inscription italic border-l-2 border-action/40 pl-4 my-4 text-primary/70"
        >
          {formatInline(block.content)}
        </blockquote>
      );
    } else {
      nodes.push(...formatInline(block.content));
    }
  }

  return nodes;
}
