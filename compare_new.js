const fs = require('fs');

function testTableCorner11() {
  const x0 = 0, y0 = 0, z0 = 0, W_A = 1200, W_B = 1200, L_last = 1800, L_first = 1800;
  const jackH = 250, storyCount = 1;
  const elements = [];
  const placedCols = new Set();
  const placedTransoms = new Set();

  function place(sym, x, y, z, rot, pName, pVal) {
    elements.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), pName, pVal: Math.round(pVal || 0) });
  }

  function ensureCol(cx, cy) {
    const key = `${Math.round(cx)}_${Math.round(cy)}`;
    if (!placedCols.has(key)) {
      placedCols.add(key);
      place('2', cx, cy, z0, 0, 'h', jackH);
      place('3', cx, cy, z0 + jackH, 0);
    }
  }

  function placeTransom(tx, ty, tz, trot, tlen) {
    place('4', tx, ty, tz, trot, 'l', tlen);
  }

  // Row 0 start:
  // If closed loop / standalone: start columns at (0,0) and (0, -1200)
  ensureCol(0, 0);
  ensureCol(0, -W_A);
  // Start head closing transoms and rails:
  const zH1 = z0 + jackH + 135;
  const zDeck = z0 + jackH + 1730;
  placeTransom(0, 0, zH1, -90, W_A);
  placeTransom(0, 0, zDeck, -90, W_A);
  place('4', 0, 0, z0 + jackH + 2180, -90, 'l', W_A);
  place('4', 0, 0, z0 + jackH + 2630, -90, 'l', W_A);
  place('7', 0, 0, zDeck, -90, 'l', W_A);

  // Now ConnectCorner_TH1_1:
  const uAx = 1, uAy = 0;
  const vAx = 0, vAy = -1; // -Y
  const xStartLast = 0, yStartLast = 0;
  const L_cut = (L_last > W_B) ? (L_last - W_B) : 0;
  const xC = xStartLast + L_cut;
  const yC = yStartLast;
  const xMax = xStartLast + L_last;
  const yMax = yStartLast;

  if (L_cut > 0) ensureCol(xC, yC);
  ensureCol(xMax, yMax);
  ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

  // 1. Back of last bay:
  const xBack = xStartLast + W_A * vAx;
  const yBack = yStartLast + W_A * vAy;
  place('1', xBack, yBack, z0, 0, 'l', L_last);
  place('4', xBack, yBack, zH1, 0, 'l', L_last);
  place('5', xBack, yBack, z0 + jackH + 135, 0, 'l', L_last);
  place('7', xBack - 66 * vAx, yBack - 66 * vAy, zDeck, 0, 'l', L_last);
  place('6', xStartLast + 345 * vAx, yStartLast + 345 * vAy, zDeck, 0, 'l', L_last);
  place('6', xBack - 345 * vAx, yBack - 345 * vAy, zDeck, 0, 'l', L_last);

  // 2. Front of last bay:
  if (L_cut > 0) {
    place('1', xStartLast, yStartLast, z0, 0, 'l', L_cut);
    place('4', xStartLast, yStartLast, zH1, 0, 'l', L_cut);
    place('4', xStartLast, yStartLast, z0 + jackH + 830, 0, 'l', L_cut);
    place('4', xStartLast, yStartLast, z0 + jackH + 2180, 0, 'l', L_cut);
    place('4', xStartLast, yStartLast, z0 + jackH + 2630, 0, 'l', L_cut);
    place('7', xStartLast + 66 * vAx, yStartLast + 66 * vAy, zDeck, 0, 'l', L_cut);
  }

  // 3. Doorway shared transom:
  placeTransom(xC, yC, zH1, 0, W_B);
  placeTransom(xC, yC, zDeck, 0, W_B);

  // Outer end of row A at xMax across W_A:
  placeTransom(xMax, yMax, zH1, -90, W_A);
  placeTransom(xMax, yMax, zDeck, -90, W_A);
  place('4', xMax, yMax, z0 + jackH + 2180, -90, 'l', W_A);
  place('4', xMax, yMax, z0 + jackH + 2630, -90, 'l', W_A);
  place('7', xMax, yMax, zDeck, -90, 'l', W_A);

  // 4. Return bay (First bay of Row B):
  const rotDegB = 90;
  const xTopC = xC;
  const yTopC = yC + L_first;
  const xTopMax = xMax;
  const yTopMax = yMax + L_first;

  ensureCol(xTopC, yTopC);
  ensureCol(xTopMax, yTopMax);

  place('1', xC, yC, z0, rotDegB, 'l', L_first);
  place('1', xMax, yMax, z0, rotDegB, 'l', L_first);
  place('4', xC, yC, zH1, rotDegB, 'l', L_first);
  place('4', xMax, yMax, zH1, rotDegB, 'l', L_first);
  place('4', xTopC, yTopC, zH1, rotDegB - 90, 'l', W_B);

  place('4', xC, yC, z0 + jackH + 830, rotDegB, 'l', L_first);
  place('5', xMax, yMax, z0 + jackH + 135, rotDegB, 'l', L_first);
  place('4', xTopC, yTopC, zDeck, rotDegB - 90, 'l', W_B);

  place('6', xC + 345, yC, zDeck, rotDegB, 'l', L_first);
  place('6', xMax - 345, yMax, zDeck, rotDegB, 'l', L_first);

  place('7', xC + 66, yC, zDeck, rotDegB, 'l', L_first);
  place('7', xMax - 66, yMax, zDeck, rotDegB, 'l', L_first);
  place('7', xTopC, yTopC, zDeck, rotDegB - 90, 'l', W_B);

  place('4', xC, yC, z0 + jackH + 2180, rotDegB, 'l', L_first);
  place('4', xTopC, yTopC, z0 + jackH + 2180, rotDegB - 90, 'l', W_B);
  place('4', xC, yC, z0 + jackH + 2630, rotDegB, 'l', L_first);
  place('4', xTopC, yTopC, z0 + jackH + 2630, rotDegB - 90, 'l', W_B);

  console.log("Total elements in TableCorner11:", elements.length);
  return elements;
}

const eNew = require('./sim_case11_new.js');
const eTable = testTableCorner11();
