import Anthropic from "@anthropic-ai/sdk";

const SYSTEM_PROMPT = `You clean up raw speech-to-text transcripts of a property agent's spoken notes about a client or deal.

Rules:
- Fix grammar, punctuation, and remove filler words (um, uh, like, you know) and false starts.
- Preserve every fact exactly as stated: names, numbers, dates, prices, percentages. Never invent, guess, or fill in information that wasn't said.
- Keep the agent's original meaning and tone. Do not summarize or shorten unless removing pure filler.
- Output plain text only — no headings, no bullet points, no preamble like "Here is the cleaned transcript:", no commentary.
- If the transcript is empty, garbled, or unintelligible, output it unchanged.`;

let client: Anthropic | null = null;

function getClient(): Anthropic {
  if (!client) {
    client = new Anthropic(); // reads ANTHROPIC_API_KEY from env (see secret binding in index.ts)
  }
  return client;
}

/**
 * Cleans a raw voice-note transcript with Claude. Thinking is disabled —
 * this is a bounded text-transformation task, not a reasoning task, and
 * every call here is billed against the agent's subscription (this is
 * the "AI cleanup" cost driver called out in the MVP checklist), so
 * keeping each call small and fast matters.
 */
export async function cleanupTranscript(rawTranscript: string): Promise<string> {
  const trimmed = rawTranscript.trim();
  if (trimmed.length === 0) return "";

  const response = await getClient().messages.create({
    model: "claude-opus-4-8",
    max_tokens: 2048,
    thinking: { type: "disabled" },
    system: SYSTEM_PROMPT,
    messages: [{ role: "user", content: trimmed }],
  });

  const textBlock = response.content.find(
    (block): block is Anthropic.TextBlock => block.type === "text"
  );
  return textBlock?.text.trim() ?? trimmed;
}
