const fs = require('fs');

// We simulate all 6 cases and compare:
// 1) ExecuteCase1_1 vs TableRun TH 1.1
// 2) ExecuteCase1_2 vs TableRun TH 1.2
// 3) ExecuteCase1_3 vs TableRun TH 1.3
// 4) ExecuteCase1_4 vs TableRun TH 1.4
// 5) ExecuteCase2_1 vs TableRun TH 2.1
// 6) ExecuteCase2_2 vs TableRun TH 2.2

function testCase11() {
  const x0 = 0, y0 = 0, z0 = 0, W = 1200, jackH = 250, storyCount = 1;
  const btn = [];
  function pBtn(sym, x, y, z, rot, val) { btn.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), val: Math.round(val || 0) }); }

  const colX = [0, 1800, 3000, 4800, 6000, 7800];
  for (let k = 0; k < colX.length; k++) {
    const px = colX[k];
    pBtn('2', px, y0, z0, 0, jackH);
    pBtn('2', px, y0 - W, z0, 0, jackH);
    pBtn('3', px, y0, z0 + jackH, 0);
    pBtn('3', px, y0 - W, z0 + jackH, 0);
    pBtn('4', px, y0, z0 + jackH + 135, -90, W);
    pBtn('4', px, y0, z0 + jackH + 1730, -90, W);
    if (k === 0 || k === colX.length - 1) {
      pBtn('4', px, y0, z0 + jackH + 2180, -90, W);
      pBtn('4', px, y0, z0 + jackH + 2630, -90, W);
      pBtn('7', px, y0, z0 + jackH + 1730, -90, W);
    }
  }

  for (let i = 0; i < 4; i++) {
    const px = colX[i];
    const L_i = (i % 2 === 0) ? 1800 : 1200;
    pBtn('1', px, y0, z0, 0, L_i);
    pBtn('1', px, y0 - W, z0, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 135, 0, L_i);
    pBtn('4', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 830, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 2180, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 2630, 0, L_i);
    pBtn('5', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pBtn('6', px, y0 - 345, z0 + jackH + 1730, 0, L_i);
    pBtn('6', px, y0 - W + 345, z0 + jackH + 1730, 0, L_i);
    pBtn('7', px, y0 - 66, z0 + jackH + 1730, 0, L_i);
    pBtn('7', px, y0 - W + 66, z0 + jackH + 1730, 0, L_i);
  }

  // Bay 5
  pBtn('1', 6000, y0 - W, z0, 0, 1800);
  pBtn('4', 6000, y0 - W, z0 + jackH + 135, 0, 1800);
  pBtn('5', 6000, y0 - W, z0 + jackH + 135, 0, 1800);
  pBtn('7', 6000, y0 - W + 66, z0 + jackH + 1730, 0, 1800);
  pBtn('6', 6000, y0 - 345, z0 + jackH + 1730, 0, 1800);
  pBtn('6', 6000, y0 - W + 345, z0 + jackH + 1730, 0, 1800);

  // Column xC = 6600
  pBtn('2', 6600, y0, z0, 0, jackH);
  pBtn('3', 6600, y0, z0 + jackH, 0);

  // Cut front bay 5
  pBtn('1', 6000, y0, z0, 0, 600);
  pBtn('4', 6000, y0, z0 + jackH + 135, 0, 600);
  pBtn('4', 6000, y0, z0 + jackH + 830, 0, 600);
  pBtn('4', 6000, y0, z0 + jackH + 2180, 0, 600);
  pBtn('4', 6000, y0, z0 + jackH + 2630, 0, 600);
  pBtn('7', 6000, y0 - 66, z0 + jackH + 1730, 0, 600);

  // Return branch
  pBtn('1', 6600, y0, z0, 90, 1800);
  pBtn('1', 7800, y0, z0, 90, 1800);
  pBtn('2', 6600, 1800, z0, 0, jackH);
  pBtn('2', 7800, 1800, z0, 0, jackH);
  pBtn('3', 6600, 1800, z0 + jackH, 0);
  pBtn('3', 7800, 1800, z0 + jackH, 0);

  pBtn('4', 6600, y0, z0 + jackH + 135, 90, 1800);
  pBtn('4', 7800, y0, z0 + jackH + 135, 90, 1800);
  pBtn('4', 6600, y0, z0 + jackH + 135, 0, 1200);
  pBtn('4', 6600, 1800, z0 + jackH + 135, 0, 1200);

  pBtn('4', 6600, y0, z0 + jackH + 830, 90, 1800);
  pBtn('5', 7800, y0, z0 + jackH + 135, 90, 1800);
  pBtn('4', 6600, y0, z0 + jackH + 1730, 0, 1200);
  pBtn('4', 6600, 1800, z0 + jackH + 1730, 0, 1200);
  pBtn('6', 6600 + 345, y0, z0 + jackH + 1730, 90, 1800);
  pBtn('6', 7800 - 345, y0, z0 + jackH + 1730, 90, 1800);
  pBtn('7', 6600 + 66, y0, z0 + jackH + 1730, 90, 1800);
  pBtn('7', 7800 - 66, y0, z0 + jackH + 1730, 90, 1800);
  pBtn('7', 6600, 1800, z0 + jackH + 1730, 0, 1200);
  pBtn('4', 6600, y0, z0 + jackH + 2180, 90, 1800);
  pBtn('4', 6600, 1800, z0 + jackH + 2180, 0, 1200);
  pBtn('4', 6600, y0, z0 + jackH + 2630, 90, 1800);
  pBtn('4', 6600, 1800, z0 + jackH + 2630, 0, 1200);

  return btn;
}

function testCase12() {
  const x0 = 0, y0 = 0, z0 = 0, W = 1200, jackH = 250, storyCount = 1;
  const btn = [];
  function pBtn(sym, x, y, z, rot, val) { btn.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), val: Math.round(val || 0) }); }

  const mainBays = [1800, 1200, 1800, 1200];
  const colX = [0, 1800, 3000, 4800, 6000];
  for (let k = 0; k < colX.length; k++) {
    const px = colX[k];
    pBtn('2', px, y0, z0, 0, jackH);
    pBtn('2', px, y0 - W, z0, 0, jackH);
    pBtn('3', px, y0, z0 + jackH, 0);
    pBtn('3', px, y0 - W, z0 + jackH, 0);
    pBtn('4', px, y0, z0 + jackH + 135, -90, W);
    pBtn('4', px, y0, z0 + jackH + 1730, -90, W);
    if (k === 0 || k === colX.length - 1) {
      pBtn('4', px, y0, z0 + jackH + 2180, -90, W);
      pBtn('4', px, y0, z0 + jackH + 2630, -90, W);
      pBtn('7', px, y0, z0 + jackH + 1730, -90, W);
    }
  }

  for (let i = 0; i < 3; i++) {
    const px = colX[i];
    const L_i = mainBays[i];
    pBtn('1', px, y0, z0, 0, L_i);
    pBtn('1', px, y0 - W, z0, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 135, 0, L_i);
    pBtn('4', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 830, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 2180, 0, L_i);
    pBtn('4', px, y0, z0 + jackH + 2630, 0, L_i);
    pBtn('5', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pBtn('6', px, y0 - 345, z0 + jackH + 1730, 0, L_i);
    pBtn('6', px, y0 - W + 345, z0 + jackH + 1730, 0, L_i);
    pBtn('7', px, y0 - 66, z0 + jackH + 1730, 0, L_i);
    pBtn('7', px, y0 - W + 66, z0 + jackH + 1730, 0, L_i);
  }

  // Bay 4 (4800 -> 6000, L = 1200):
  const xLast = 4800, L_last = 1200;
  pBtn('1', xLast, y0 - W, z0, 0, L_last);
  pBtn('4', xLast, y0 - W, z0 + jackH + 135, 0, L_last);
  pBtn('5', xLast, y0 - W, z0 + jackH + 135, 0, L_last);
  pBtn('7', xLast, y0 - W + 66, z0 + jackH + 1730, 0, L_last);
  pBtn('6', xLast, y0 - 345, z0 + jackH + 1730, 0, L_last);
  pBtn('6', xLast, y0 - W + 345, z0 + jackH + 1730, 0, L_last);

  // Return branch 1200 x 1200
  const xMax = 6000, yTop = 1200, L_return = 1200, W_return = 1200;
  pBtn('1', xLast, y0, z0, 90, L_return);
  pBtn('1', xMax, y0, z0, 90, L_return);
  pBtn('2', xLast, yTop, z0, 0, jackH);
  pBtn('2', xMax, yTop, z0, 0, jackH);
  pBtn('3', xLast, yTop, z0 + jackH, 0);
  pBtn('3', xMax, yTop, z0 + jackH, 0);

  pBtn('4', xLast, y0, z0 + jackH + 135, 90, L_return);
  pBtn('4', xMax, y0, z0 + jackH + 135, 90, L_return);
  pBtn('4', xLast, y0, z0 + jackH + 135, 0, W_return);
  pBtn('4', xLast, yTop, z0 + jackH + 135, 0, W_return);

  pBtn('4', xLast, y0, z0 + jackH + 830, 90, L_return);
  pBtn('5', xMax, y0, z0 + jackH + 135, 90, L_return);
  pBtn('4', xLast, y0, z0 + jackH + 1730, 0, W_return);
  pBtn('4', xLast, yTop, z0 + jackH + 1730, 0, W_return);
  pBtn('6', xLast + 345, y0, z0 + jackH + 1730, 90, L_return);
  pBtn('6', xMax - 345, y0, z0 + jackH + 1730, 90, L_return);
  pBtn('7', xLast + 66, y0, z0 + jackH + 1730, 90, L_return);
  pBtn('7', xMax - 66, y0, z0 + jackH + 1730, 90, L_return);
  pBtn('7', xLast, yTop, z0 + jackH + 1730, 0, W_return);
  pBtn('4', xLast, y0, z0 + jackH + 2180, 90, L_return);
  pBtn('4', xLast, yTop, z0 + jackH + 2180, 0, W_return);
  pBtn('4', xLast, y0, z0 + jackH + 2630, 90, L_return);
  pBtn('4', xLast, yTop, z0 + jackH + 2630, 0, W_return);

  return btn;
}

console.log(`Button 1.1 count: ${testCase11().length}`);
console.log(`Button 1.2 count: ${testCase12().length}`);

function testTbl12() {
  const x0 = 0, y0 = 0, z0 = 0, W = 1200, jackH = 250, storyCount = 1;
  const tbl = [];
  function pTbl(sym, x, y, z, rot, val) { tbl.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), val: Math.round(val || 0) }); }

  const placedCols = new Set();
  function ensureCol(cx, cy) {
    const k = `${Math.round(cx)}_${Math.round(cy)}`;
    if (!placedCols.has(k)) {
      placedCols.add(k);
      pTbl('2', cx, cy, z0, 0, jackH);
      pTbl('3', cx, cy, z0 + jackH, 0);
    }
  }

  const placedTransoms = new Set();
  function placeTransom(tx, ty, tz, trot, tlen) {
    const k = `${Math.round(tx)}_${Math.round(ty)}_${Math.round(tz)}_${Math.round(trot)}`;
    if (!placedTransoms.has(k)) {
      placedTransoms.add(k);
      pTbl('4', tx, ty, tz, trot, tlen);
    }
  }

  const bayLens = [1800, 1200, 1800, 1200];
  const colX = [0, 1800, 3000, 4800, 6000];
  // Straight run bays 0..2:
  for (let k = 0; k < 4; k++) {
    const px = colX[k];
    ensureCol(px, y0);
    ensureCol(px, y0 - W);
    placeTransom(px, y0, z0 + jackH + 135, -90, W);
    placeTransom(px, y0, z0 + jackH + 1730, -90, W);
    if (k === 0) {
      pTbl('4', px, y0, z0 + jackH + 2180, -90, W);
      pTbl('4', px, y0, z0 + jackH + 2630, -90, W);
      pTbl('7', px, y0, z0 + jackH + 1730, -90, W);
    }
  }

  for (let i = 0; i < 3; i++) {
    const px = colX[i];
    const L_i = bayLens[i];
    pTbl('1', px, y0, z0, 0, L_i);
    pTbl('1', px, y0 - W, z0, 0, L_i);
    pTbl('4', px, y0, z0 + jackH + 135, 0, L_i);
    pTbl('4', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pTbl('4', px, y0, z0 + jackH + 830, 0, L_i);
    pTbl('4', px, y0, z0 + jackH + 2180, 0, L_i);
    pTbl('4', px, y0, z0 + jackH + 2630, 0, L_i);
    pTbl('5', px, y0 - W, z0 + jackH + 135, 0, L_i);
    pTbl('6', px, y0 - 345, z0 + jackH + 1730, 0, L_i);
    pTbl('6', px, y0 - W + 345, z0 + jackH + 1730, 0, L_i);
    pTbl('7', px, y0 - 66, z0 + jackH + 1730, 0, L_i);
    pTbl('7', px, y0 - W + 66, z0 + jackH + 1730, 0, L_i);
  }

  // ConnectCorner_TH1_2 on bay 3:
  const xLast = 4800, L_last = 1200, W_B = 1200, xMax = 6000;
  ensureCol(xLast, y0);
  ensureCol(xMax, y0);
  ensureCol(xMax, y0 - W);

  // Back of last bay:
  pTbl('1', xLast, y0 - W, z0, 0, L_last);
  pTbl('4', xLast, y0 - W, z0 + jackH + 135, 0, L_last);
  pTbl('5', xLast, y0 - W, z0 + jackH + 135, 0, L_last);
  pTbl('7', xLast, y0 - W + 66, z0 + jackH + 1730, 0, L_last);
  pTbl('6', xLast, y0 - 345, z0 + jackH + 1730, 0, L_last);
  pTbl('6', xLast, y0 - W + 345, z0 + jackH + 1730, 0, L_last);

  // Shared transoms at doorway (from xLast to xMax along angle 0 of len W_B):
  placeTransom(xLast, y0, z0 + jackH + 135, 0, W_B);
  placeTransom(xLast, y0, z0 + jackH + 1730, 0, W_B);

  // End transoms at xMax across W_A:
  placeTransom(xMax, y0, z0 + jackH + 135, -90, W);
  placeTransom(xMax, y0, z0 + jackH + 1730, -90, W);

  // End railings at xMax across W_A:
  pTbl('4', xMax, y0, z0 + jackH + 2180, -90, W);
  pTbl('4', xMax, y0, z0 + jackH + 2630, -90, W);
  pTbl('7', xMax, y0, z0 + jackH + 1730, -90, W);

  // Standalone return branch:
  ensureCol(xLast, 1200);
  ensureCol(xMax, 1200);
  pTbl('1', xLast, y0, z0, 90, 1200);
  pTbl('1', xMax, y0, z0, 90, 1200);
  pTbl('4', xLast, y0, z0 + jackH + 135, 90, 1200);
  pTbl('4', xMax, y0, z0 + jackH + 135, 90, 1200);
  pTbl('4', xLast, 1200, z0 + jackH + 135, 0, 1200);

  pTbl('4', xLast, y0, z0 + jackH + 830, 90, 1200);
  pTbl('5', xMax, y0, z0 + jackH + 135, 90, 1200);
  pTbl('4', xLast, 1200, z0 + jackH + 1730, 0, 1200);
  pTbl('6', xLast + 345, y0, z0 + jackH + 1730, 90, 1200);
  pTbl('6', xMax - 345, y0, z0 + jackH + 1730, 90, 1200);
  pTbl('7', xLast + 66, y0, z0 + jackH + 1730, 90, 1200);
  pTbl('7', xMax - 66, y0, z0 + jackH + 1730, 90, 1200);
  pTbl('7', xLast, 1200, z0 + jackH + 1730, 0, 1200);
  pTbl('4', xLast, y0, z0 + jackH + 2180, 90, 1200);
  pTbl('4', xLast, 1200, z0 + jackH + 2180, 0, 1200);
  pTbl('4', xLast, y0, z0 + jackH + 2630, 90, 1200);
  pTbl('4', xLast, 1200, z0 + jackH + 2630, 0, 1200);

  return tbl;
}

function compareCase(name, btnFn, tblFn) {
  const btn = btnFn();
  const tbl = tblFn();
  console.log(`\n=== CASE ${name} ===`);
  console.log(`Button elements count: ${btn.length}`);
  console.log(`Table elements count: ${tbl.length}`);

  function key(e) {
    return `${e.sym}_${e.x}_${e.y}_${e.z}_${e.rot}_${e.val}`;
  }

  const btnMap = new Map();
  for (const e of btn) {
    const k = key(e);
    btnMap.set(k, (btnMap.get(k) || 0) + 1);
  }

  const tblMap = new Map();
  for (const e of tbl) {
    const k = key(e);
    tblMap.set(k, (tblMap.get(k) || 0) + 1);
  }

  let missing = 0;
  for (const [k, count] of btnMap.entries()) {
    const tCount = tblMap.get(k) || 0;
    if (tCount < count) {
      console.log(`MISSING in Table (${count - tCount}): ${k}`);
      missing += (count - tCount);
    }
  }

  let extra = 0;
  for (const [k, count] of tblMap.entries()) {
    const bCount = btnMap.get(k) || 0;
    if (bCount < count) {
      console.log(`EXTRA in Table (${count - bCount}): ${k}`);
      extra += (count - bCount);
    }
  }

  if (missing === 0 && extra === 0) {
    console.log(`--> [PERFECT MATCH 100%]`);
  } else {
    console.log(`--> [MISMATCH: Missing ${missing}, Extra ${extra}]`);
  }
}

compareCase('TH 1.2', testCase12, testTbl12);
