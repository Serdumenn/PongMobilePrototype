const { DataApi } = require("@unity-services/cloud-save-1.4");

const ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
const CODE_LENGTH = 6;
const VALID_DAYS = 7;
const KEY = "run";

const VERSION = 1;
const MAX_BYTES = 12000;
const MAX_SCORE = 300;
const MAX_DURATION = 6200;
const MAX_STEP = 3;
const MAX_BALL_KEYS = 1500;
const MAX_SCORE_KEYS = 400;
const MAX_SKIN = 40;
const SLACK = 50;
const PADDLE_KIND = 3;
const LAST_KIND = 5;

function read(bytes) {
  let at = 0;
  const need = (count) => {
    if (at + count > bytes.length) throw new Error("short");
  };
  const u8 = () => { need(1); return bytes.readUInt8(at++); };
  const u16 = () => { need(2); const v = bytes.readUInt16LE(at); at += 2; return v; };
  const i16 = () => { need(2); const v = bytes.readInt16LE(at); at += 2; return v; };
  const i32 = () => { need(4); const v = bytes.readInt32LE(at); at += 4; return v; };
  const text = () => {
    const length = u8();
    if (length > MAX_SKIN) throw new Error("skin");
    need(length);
    const value = bytes.toString("ascii", at, at + length);
    at += length;
    return value;
  };

  if (u8() !== VERSION) throw new Error("version");
  const run = { seed: i32(), score: u16(), duration: u16(), ball: text(), paddle: text(), keys: [], scores: [], paddles: 0 };

  const ballCount = u16();
  for (let i = 0; i < ballCount; i++) {
    const time = u16();
    i16();
    i16();
    const kind = u8();
    if (kind > LAST_KIND) throw new Error("kind");
    run.keys.push({ time, kind });
  }

  const scoreCount = u16();
  for (let i = 0; i < scoreCount; i++) run.scores.push({ time: u16(), score: u16() });

  run.paddles = u16();
  need(run.paddles);
  at += run.paddles;
  if (at !== bytes.length) throw new Error("trailing");
  return run;
}

function problem(run) {
  if (run.score < 1) return "empty";
  if (run.score > MAX_SCORE) return "score";
  if (run.duration <= 0 || run.duration > MAX_DURATION) return "duration";
  if (run.keys.length < 1 || run.keys.length > MAX_BALL_KEYS) return "ball";
  if (run.scores.length < 1 || run.scores.length > MAX_SCORE_KEYS) return "score";
  if (run.paddles > Math.ceil(run.duration / 10) + 20) return "paddle";

  let previous = 0;
  let hits = 0;
  for (const key of run.keys) {
    if (key.time < previous || key.time > run.duration + SLACK) return "ball";
    previous = key.time;
    if (key.kind === PADDLE_KIND) hits++;
  }

  previous = 0;
  let last = 0;
  for (const key of run.scores) {
    const step = key.score - last;
    if (key.time < previous || key.time > run.duration + SLACK || step < 1 || step > MAX_STEP) return "score";
    previous = key.time;
    last = key.score;
  }

  if (last !== run.score) return "score";
  if (hits < run.scores.length) return "hits";
  return null;
}

function cleanName(name) {
  const value = String(name || "").replace(/[^A-Za-z ]/g, "").trim().slice(0, 24);
  return value.length > 0 ? value : "A friend";
}

function makeCode() {
  let code = "";
  for (let i = 0; i < CODE_LENGTH; i++) code += ALPHABET[Math.floor(Math.random() * ALPHABET.length)];
  return code;
}

async function existing(api, projectId, id) {
  try {
    const response = await api.getPrivateCustomItems(projectId, id, [KEY]);
    const item = response.data.results[0];
    return item ? item.value : null;
  } catch (err) {
    if (err.response && err.response.status === 404) return null;
    throw err;
  }
}

module.exports = async ({ params, context, logger }) => {
  const encoded = String(params.run || "");
  const bytes = Buffer.from(encoded, "base64");
  if (bytes.length < 12 || bytes.length > MAX_BYTES) return { ok: false, reason: "size" };

  let run;
  try {
    run = read(bytes);
  } catch (err) {
    return { ok: false, reason: "format" };
  }

  const issue = problem(run);
  if (issue) {
    logger.warning("Ghost refused", { reason: issue, score: run.score });
    return { ok: false, reason: issue };
  }

  const api = new DataApi(context);
  const now = Date.now();
  const record = {
    run: bytes.toString("base64"),
    name: cleanName(params.name),
    score: run.score,
    owner: context.playerId,
    created: now,
    expires: now + VALID_DAYS * 24 * 60 * 60 * 1000
  };

  for (let attempt = 0; attempt < 6; attempt++) {
    const code = makeCode();
    const id = "ghost-" + code;
    const old = await existing(api, context.projectId, id);
    if (old && old.expires > now) continue;

    await api.setPrivateCustomItem(context.projectId, id, { key: KEY, value: record });
    return { ok: true, code, days: VALID_DAYS };
  }

  return { ok: false, reason: "busy" };
};

module.exports.params = {
  run: { type: "String", required: true },
  name: { type: "String", required: false }
};
