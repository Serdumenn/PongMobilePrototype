const { DataApi } = require("@unity-services/cloud-save-1.4");

const ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
const CODE_LENGTH = 6;
const KEY = "run";

function normalize(code) {
  return String(code || "").replace(/[\s-]/g, "").toUpperCase();
}

function valid(code) {
  if (code.length !== CODE_LENGTH) return false;
  for (const c of code) if (!ALPHABET.includes(c)) return false;
  return true;
}

module.exports = async ({ params, context, logger }) => {
  const code = normalize(params.code);
  if (!valid(code)) return { found: false, reason: "invalid" };

  const api = new DataApi(context);
  let record = null;
  try {
    const response = await api.getPrivateCustomItems(context.projectId, "ghost-" + code, [KEY]);
    const item = response.data.results[0];
    record = item ? item.value : null;
  } catch (err) {
    if (!(err.response && err.response.status === 404)) throw err;
  }

  if (!record || !record.run) return { found: false, reason: "missing" };
  if (record.expires <= Date.now()) return { found: false, reason: "expired" };

  return {
    found: true,
    run: record.run,
    name: record.name,
    score: record.score,
    mine: record.owner === context.playerId
  };
};

module.exports.params = {
  code: { type: "String", required: true }
};
