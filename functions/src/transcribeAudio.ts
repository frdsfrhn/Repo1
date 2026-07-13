import { SpeechClient } from "@google-cloud/speech";
import { storage } from "./firebaseAdmin";

const speechClient = new SpeechClient();

/**
 * Parses a `gs://bucket/path/to/file` URL (what
 * StorageService.uploadRecording returns) into its parts.
 */
function parseGsUrl(gsUrl: string): { bucket: string; path: string } {
  const match = /^gs:\/\/([^/]+)\/(.+)$/.exec(gsUrl);
  if (!match) {
    throw new Error(`Not a gs:// URL: ${gsUrl}`);
  }
  return { bucket: match[1], path: match[2] };
}

/**
 * Downloads the recording and runs it through Google Cloud Speech-to-Text.
 * The client records 16kHz mono LINEAR16 WAV specifically so this can call
 * `recognize` directly with no server-side transcoding step — see
 * VoiceRecorderService in the Flutter app.
 */
export async function transcribeAudio(gsUrl: string): Promise<string> {
  const { bucket, path } = parseGsUrl(gsUrl);
  const [audioBytes] = await storage.bucket(bucket).file(path).download();

  const [response] = await speechClient.recognize({
    audio: { content: audioBytes.toString("base64") },
    config: {
      encoding: "LINEAR16",
      sampleRateHertz: 16000,
      languageCode: "en-MY",
      alternativeLanguageCodes: ["ms-MY", "zh-CN"],
      model: "default",
      enableAutomaticPunctuation: true,
    },
  });

  const transcript = (response.results ?? [])
    .map((result) => result.alternatives?.[0]?.transcript ?? "")
    .join(" ")
    .trim();

  return transcript;
}
