// Verify unified dynamic algorithm against Master 6 Corners
const fs = require('fs');

function simulateMaster() {
  const placedCols = new Set();
  const placedTransoms = new Set();
  const elements = [];

  const x0 = 0, y0 = 0, z0 = 0, jackH = 250;
  const zH1 = z0 + jackH + 135;
  const zH3 = z0 + jackH + 830;
  const zH6 = z0 + jackH + 2180;
  const zH7 = z0 + jackH + 2630;
  const zDeck = z0 + jackH + 1730;

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

  function placeSharedTransom(tx, ty, trot, tlen) {
    const key = `${Math.round(tx)}_${Math.round(ty)}_${Math.round(trot)}`;
    if (!placedTransoms.has(key)) {
      placedTransoms.add(key);
      place('4', tx, ty, zH1, trot, 'l', tlen);
      place('4', tx, ty, zDeck, trot, 'l', tlen);
    }
  }

  function placeBayDiagonalBraces(x, y, rot, len) {
    place('5', x, y, zH1, rot, 'l', len);
  }

  // LEG 1
  ensureCol(x0, y0); ensureCol(x0, y0 + 1200);
  ensureCol(x0 + 1800, y0); ensureCol(x0 + 1800, y0 + 1200);
  ensureCol(x0 + 3600, y0); ensureCol(x0 + 2400, y0 + 1200);
  ensureCol(x0 + 3600, y0 + 1200);

  placeSharedTransom(x0, y0, 90, 1200);
  place('4', x0, y0, zH6, 90, 'l', 1200);
  place('4', x0, y0, zH7, 90, 'l', 1200);
  place('7', x0, y0, zDeck, 90, 'l', 1200);

  placeSharedTransom(x0 + 1800, y0, 90, 1200);
  placeSharedTransom(x0 + 3600, y0, 90, 1200);
  place('4', x0 + 3600, y0, zH6, 90, 'l', 1200);
  place('4', x0 + 3600, y0, zH7, 90, 'l', 1200);
  place('7', x0 + 3600, y0, zDeck, 90, 'l', 1200);

  // Bay 1
  place('1', x0, y0, z0, 0, 'l', 1800);
  place('4', x0, y0, zH1, 0, 'l', 1800);
  placeBayDiagonalBraces(x0, y0, 0, 1800);
  place('7', x0, y0 + 66, zDeck, 0, 'l', 1800);

  place('1', x0, y0 + 1200, z0, 0, 'l', 1800);
  place('4', x0, y0 + 1200, zH1, 0, 'l', 1800);
  place('4', x0, y0 + 1200, zH3, 0, 'l', 1800);
  place('4', x0, y0 + 1200, zH6, 0, 'l', 1800);
  place('4', x0, y0 + 1200, zH7, 0, 'l', 1800);
  place('7', x0, y0 + 1200 - 66, zDeck, 0, 'l', 1800);

  place('6', x0, y0 + 345, zDeck, 0, 'l', 1800);
  place('6', x0, y0 + 1200 - 345, zDeck, 0, 'l', 1800);

  // Bay 2 (Corner 1)
  place('1', x0 + 1800, y0, z0, 0, 'l', 1800);
  place('4', x0 + 1800, y0, zH1, 0, 'l', 1800);
  placeBayDiagonalBraces(x0 + 1800, y0, 0, 1800);
  place('7', x0 + 1800, y0 + 66, zDeck, 0, 'l', 1800);

  place('1', x0 + 1800, y0 + 1200, z0, 0, 'l', 600);
  place('4', x0 + 1800, y0 + 1200, zH1, 0, 'l', 600);
  place('4', x0 + 1800, y0 + 1200, zH3, 0, 'l', 600);
  place('4', x0 + 1800, y0 + 1200, zH6, 0, 'l', 600);
  place('4', x0 + 1800, y0 + 1200, zH7, 0, 'l', 600);
  place('7', x0 + 1800, y0 + 1200 - 66, zDeck, 0, 'l', 600);

  placeSharedTransom(x0 + 2400, y0 + 1200, 0, 1200);

  place('6', x0 + 1800, y0 + 345, zDeck, 0, 'l', 1800);
  place('6', x0 + 1800, y0 + 1200 - 345, zDeck, 0, 'l', 1800);

  return elements;
}

const masterElems = simulateMaster();
console.log("Master Leg 1 elements:", masterElems.length);
