const fs = require('fs');

function runComparison() {
  const x0 = 0, y0 = 0, z0 = 0, W = 1200, jackH = 250, storyCount = 1;
  const topZ = z0 + jackH + 2630; // or whatever GetTopZ does
  const hasTopTransom = false;

  // Let's record placed elements: { sym, x, y, z, rot, param, val }
  const btnElems = [];
  function placeBtn(sym, x, y, z, rot, pName, pVal) {
    btnElems.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), pName, pVal: Math.round(pVal || 0) });
  }

  // 1. ExecuteCase1_1 logic:
  const colX = [0, 1800, 3000, 4800, 6000, 7800];
  // BuildMainColumnGrid:
  for (let k = 0; k < colX.length; k++) {
    const px = colX[k];
    const isOuterEnd = (k === 0 || k === colX.length - 1);
    placeBtn('2', px, y0, z0, 0, 'h', jackH);
    placeBtn('2', px, y0 - W, z0, 0, 'h', jackH);
    placeBtn('3', px, y0, z0 + jackH, 0);
    placeBtn('3', px, y0 - W, z0 + jackH, 0);

    const zH1 = z0 + jackH + 135;
    placeBtn('4', px, y0, zH1, -90, 'l', W);
    for (let s = 0; s < storyCount; s++) {
      const sDeck = z0 + jackH + 1730 + s * 1800;
      placeBtn('4', px, y0, sDeck, -90, 'l', W);
    }
    if (isOuterEnd) {
      for (let s = 0; s < storyCount; s++) {
        const sOff = s * 1800;
        placeBtn('4', px, y0, z0 + jackH + 2180 + sOff, -90, 'l', W);
        placeBtn('4', px, y0, z0 + jackH + 2630 + sOff, -90, 'l', W);
        placeBtn('7', px, y0, z0 + jackH + 1730 + sOff, -90, 'l', W);
      }
    }
  }

  // Bays 0..3 standard:
  for (let i = 0; i < 4; i++) {
    const px = colX[i];
    const L_i = (i % 2 === 0) ? 1800 : 1200;
    // BuildStandardBayElements:
    placeBtn('1', px, y0, z0, 0, 'l', L_i);
    placeBtn('1', px, y0 - W, z0, 0, 'l', L_i);
    const zH1 = z0 + jackH + 135;
    placeBtn('4', px, y0, zH1, 0, 'l', L_i);
    placeBtn('4', px, y0 - W, zH1, 0, 'l', L_i);
    for (let s = 0; s < storyCount; s++) {
      const sOff = s * 1800;
      placeBtn('4', px, y0, z0 + jackH + 830 + sOff, 0, 'l', L_i);
      placeBtn('4', px, y0, z0 + jackH + 2180 + sOff, 0, 'l', L_i);
      placeBtn('4', px, y0, z0 + jackH + 2630 + sOff, 0, 'l', L_i);
      // Diagonals at back
      placeBtn('5', px, y0 - W, z0 + jackH + 135, 0, 'l', L_i);
      // Planks
      const zDeck = z0 + jackH + 1730 + sOff;
      placeBtn('6', px, y0 - 345, zDeck, 0, 'l', L_i);
      placeBtn('6', px, y0 - W + 345, zDeck, 0, 'l', L_i);
      // Toe boards
      placeBtn('7', px, y0 - 66, zDeck, 0, 'l', L_i);
      placeBtn('7', px, y0 - W + 66, zDeck, 0, 'l', L_i);
    }
  }

  // Bay 5 main (6000 -> 7800):
  const xBay5 = 6000, L_bay5 = 1800;
  placeBtn('1', xBay5, y0 - W, z0, 0, 'l', L_bay5);
  const zH1_5 = z0 + jackH + 135;
  placeBtn('4', xBay5, y0 - W, zH1_5, 0, 'l', L_bay5);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeBtn('5', xBay5, y0 - W, z0 + jackH + 135, 0, 'l', L_bay5);
    const zDeck = z0 + jackH + 1730 + sOff;
    placeBtn('7', xBay5, y0 - W + 66, zDeck, 0, 'l', L_bay5);
    placeBtn('6', xBay5, y0 - 345, zDeck, 0, 'l', L_bay5);
    placeBtn('6', xBay5, y0 - W + 345, zDeck, 0, 'l', L_bay5);
  }

  // Column at xC = 6600 (front only):
  const xC = 6600;
  placeBtn('2', xC, y0, z0, 0, 'h', jackH);
  placeBtn('3', xC, y0, z0 + jackH, 0);

  // Front of bay 5: cut to 600 mm:
  const L_cut = 600;
  placeBtn('1', xBay5, y0, z0, 0, 'l', L_cut);
  placeBtn('4', xBay5, y0, zH1_5, 0, 'l', L_cut);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeBtn('4', xBay5, y0, z0 + jackH + 830 + sOff, 0, 'l', L_cut);
    placeBtn('4', xBay5, y0, z0 + jackH + 2180 + sOff, 0, 'l', L_cut);
    placeBtn('4', xBay5, y0, z0 + jackH + 2630 + sOff, 0, 'l', L_cut);
    const zDeck = z0 + jackH + 1730 + sOff;
    placeBtn('7', xBay5, y0 - 66, zDeck, 0, 'l', L_cut);
  }

  // Return branch (90 deg to +Y, 1800 x 1200):
  const xMax = 7800, yTop = 1800, L_return = 1800, W_return = 1200;
  placeBtn('1', xC, y0, z0, 90, 'l', L_return);
  placeBtn('1', xMax, y0, z0, 90, 'l', L_return);
  placeBtn('2', xC, yTop, z0, 0, 'h', jackH);
  placeBtn('2', xMax, yTop, z0, 0, 'h', jackH);
  placeBtn('3', xC, yTop, z0 + jackH, 0);
  placeBtn('3', xMax, yTop, z0 + jackH, 0);

  const zH1_R = z0 + jackH + 135;
  placeBtn('4', xC, y0, zH1_R, 90, 'l', L_return);
  placeBtn('4', xMax, y0, zH1_R, 90, 'l', L_return);
  placeBtn('4', xC, y0, zH1_R, 0, 'l', W_return);
  placeBtn('4', xC, yTop, zH1_R, 0, 'l', W_return);

  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeBtn('4', xC, y0, z0 + jackH + 830 + sOff, 90, 'l', L_return);
    placeBtn('5', xMax, y0, z0 + jackH + 135, 90, 'l', L_return);
    const zDeck_R = z0 + jackH + 1730 + sOff;
    placeBtn('4', xC, y0, zDeck_R, 0, 'l', W_return);
    placeBtn('4', xC, yTop, zDeck_R, 0, 'l', W_return);
    placeBtn('6', xC + 345, y0, zDeck_R, 90, 'l', L_return);
    placeBtn('6', xMax - 345, y0, zDeck_R, 90, 'l', L_return);
    placeBtn('7', xC + 66, y0, zDeck_R, 90, 'l', L_return);
    placeBtn('7', xMax - 66, y0, zDeck_R, 90, 'l', L_return);
    placeBtn('7', xC, yTop, zDeck_R, 0, 'l', W_return);
    placeBtn('4', xC, y0, z0 + jackH + 2180 + sOff, 90, 'l', L_return);
    placeBtn('4', xC, yTop, z0 + jackH + 2180 + sOff, 0, 'l', W_return);
    placeBtn('4', xC, y0, z0 + jackH + 2630 + sOff, 90, 'l', L_return);
    placeBtn('4', xC, yTop, z0 + jackH + 2630 + sOff, 0, 'l', W_return);
  }

  console.log(`BUTTON Elements Count: ${btnElems.length}`);

  // =========================================================================
  // 2. NOW TABLE RUN (isStandalone = true, rowSingle has [1800, 1200, 1800, 1200, 1800]):
  // =========================================================================
  const tblElems = [];
  function placeTbl(sym, x, y, z, rot, pName, pVal) {
    tblElems.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), pName, pVal: Math.round(pVal || 0) });
  }

  const placedCols = new Set();
  function ensureColTbl(cx, cy) {
    const key = `${Math.round(cx)}_${Math.round(cy)}`;
    if (!placedCols.has(key)) {
      placedCols.add(key);
      placeTbl('2', cx, cy, z0, 0, 'h', jackH);
      placeTbl('3', cx, cy, z0 + jackH, 0);
    }
  }

  const placedTransoms = new Set();
  function placeTransomTbl(tx, ty, tz, trot, tlen) {
    const key = `${Math.round(tx)}_${Math.round(ty)}_${Math.round(tz)}_${Math.round(trot)}`;
    if (!placedTransoms.has(key)) {
      placedTransoms.add(key);
      placeTbl('4', tx, ty, tz, trot, 'l', tlen);
    }
  }

  // BuildTableStraightRun with hasCornerConnect = true:
  const bayLens = [1800, 1200, 1800, 1200, 1800];
  const nBaysTotal = 5;
  const nStandardBays = 4; // nBaysTotal - 1
  const nColsToBuild = 5; // distances: 0, 1800, 3000, 4800, 6000
  const dists = [0, 1800, 3000, 4800, 6000, 7800];

  for (let k = 0; k < nColsToBuild; k++) {
    const cx1 = dists[k], cy1 = 0;
    const cx2 = cx1, cy2 = cy1 - W;
    ensureColTbl(cx1, cy1);
    ensureColTbl(cx2, cy2);
    placeTransomTbl(cx1, cy1, z0 + jackH + 135, -90, W);
    for (let s = 0; s < storyCount; s++) {
      placeTransomTbl(cx1, cy1, z0 + jackH + 1730 + s * 1800, -90, W);
    }
    if (k === 0) { // openStart is false
      for (let s = 0; s < storyCount; s++) {
        const sOff = s * 1800;
        placeTbl('4', cx1, cy1, z0 + jackH + 2180 + sOff, -90, 'l', W);
        placeTbl('4', cx1, cy1, z0 + jackH + 2630 + sOff, -90, 'l', W);
        placeTbl('7', cx1, cy1, z0 + jackH + 1730 + sOff, -90, 'l', W);
      }
    }
  }

  // Longitudinal for bays 0..3:
  for (let b = 0; b < nStandardBays; b++) {
    const px1 = dists[b], py1 = 0;
    const px2 = px1, py2 = py1 - W;
    const bLen = bayLens[b];
    placeTbl('1', px1, py1, z0, 0, 'l', bLen);
    placeTbl('1', px2, py2, z0, 0, 'l', bLen);
    placeTbl('4', px1, py1, z0 + jackH + 135, 0, 'l', bLen);
    placeTbl('4', px2, py2, z0 + jackH + 135, 0, 'l', bLen);
    for (let s = 0; s < storyCount; s++) {
      const sOff = s * 1800;
      placeTbl('4', px1, py1, z0 + jackH + 830 + sOff, 0, 'l', bLen);
      placeTbl('4', px1, py1, z0 + jackH + 2180 + sOff, 0, 'l', L_i = bLen);
      placeTbl('4', px1, py1, z0 + jackH + 2630 + sOff, 0, 'l', bLen);
      placeTbl('5', px2, py2, z0 + jackH + 135, 0, 'l', bLen);
      const zDeck = z0 + jackH + 1730 + sOff;
      placeTbl('6', px1, py1 - 345, zDeck, 0, 'l', bLen);
      placeTbl('6', px2, py2 + 345, zDeck, 0, 'l', bLen);
      placeTbl('7', px1, py1 - 66, zDeck, 0, 'l', bLen);
      placeTbl('7', px2, py2 + 66, zDeck, 0, 'l', bLen);
    }
  }

  // Now ConnectCorner_TH1_1:
  // ensureCol(xC, yC); ensureCol(xMax, yMax); ensureCol(xMax, yMax - W);
  ensureColTbl(6600, 0);
  ensureColTbl(7800, 0);
  ensureColTbl(7800, -1200);

  // Back of last bay:
  placeTbl('1', 6000, -1200, z0, 0, 'l', 1800);
  placeTbl('4', 6000, -1200, z0 + jackH + 135, 0, 'l', 1800);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeTbl('5', 6000, -1200, z0 + jackH + 135, 0, 'l', 1800);
    const zDeck = z0 + jackH + 1730 + sOff;
    placeTbl('7', 6000, -1134, zDeck, 0, 'l', 1800);
    placeTbl('6', 6000, -345, zDeck, 0, 'l', 1800);
    placeTbl('6', 6000, -855, zDeck, 0, 'l', 1800);
  }

  // Front of last bay:
  placeTbl('1', 6000, 0, z0, 0, 'l', 600);
  placeTbl('4', 6000, 0, z0 + jackH + 135, 0, 'l', 600);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeTbl('4', 6000, 0, z0 + jackH + 830 + sOff, 0, 'l', 600);
    placeTbl('4', 6000, 0, z0 + jackH + 2180 + sOff, 0, 'l', 600);
    placeTbl('4', 6000, 0, z0 + jackH + 2630 + sOff, 0, 'l', 600);
    const zDeck = z0 + jackH + 1730 + sOff;
    placeTbl('7', 6000, -66, zDeck, 0, 'l', 600);
  }

  // Shared transom at doorway:
  placeTransomTbl(6600, 0, z0 + jackH + 135, 0, 1200);
  for (let s = 0; s < storyCount; s++) {
    placeTransomTbl(6600, 0, z0 + jackH + 1730 + s * 1800, 0, 1200);
  }

  // End transoms at xMax:
  placeTransomTbl(7800, 0, z0 + jackH + 135, -90, 1200);
  for (let s = 0; s < storyCount; s++) { placeTransomTbl(7800, 0, z0 + jackH + 1730 + s * 1800, -90, 1200); }
  // End at xMax = 7800:
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeTbl('4', 7800, 0, z0 + jackH + 2180 + sOff, -90, 'l', 1200);
    placeTbl('4', 7800, 0, z0 + jackH + 2630 + sOff, -90, 'l', 1200);
    placeTbl('7', 7800, 0, z0 + jackH + 1730 + sOff, -90, 'l', 1200);
  }

  // Standalone return branch:
  ensureColTbl(6600, 1800);
  ensureColTbl(7800, 1800);
  placeTbl('1', 6600, 0, z0, 90, 'l', 1800);
  placeTbl('1', 7800, 0, z0, 90, 'l', 1800);
  placeTbl('4', 6600, 0, z0 + jackH + 135, 90, 'l', 1800);
  placeTbl('4', 7800, 0, z0 + jackH + 135, 90, 'l', 1800);
  placeTbl('4', 6600, 1800, z0 + jackH + 135, 0, 'l', 1200);

  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    placeTbl('4', 6600, 0, z0 + jackH + 830 + sOff, 90, 'l', 1800);
    placeTbl('5', 7800, 0, z0 + jackH + 135, 90, 'l', 1800);
    const zDeck_R = z0 + jackH + 1730 + sOff;
    placeTbl('4', 6600, 1800, zDeck_R, 0, 'l', 1200);
    placeTbl('6', 6600 + 345, 0, zDeck_R, 90, 'l', 1800);
    placeTbl('6', 7800 - 345, 0, zDeck_R, 90, 'l', 1800);
    placeTbl('7', 6600 + 66, 0, zDeck_R, 90, 'l', 1800);
    placeTbl('7', 7800 - 66, 0, zDeck_R, 90, 'l', 1800);
    placeTbl('7', 6600, 1800, zDeck_R, 0, 'l', 1200);
    placeTbl('4', 6600, 0, z0 + jackH + 2180 + sOff, 90, 'l', 1800);
    placeTbl('4', 6600, 1800, z0 + jackH + 2180 + sOff, 0, 'l', 1200);
    placeTbl('4', 6600, 0, z0 + jackH + 2630 + sOff, 90, 'l', 1800);
    placeTbl('4', 6600, 1800, z0 + jackH + 2630 + sOff, 0, 'l', 1200);
  }

  console.log(`TABLE Elements Count: ${tblElems.length}`);

  // Now find differences:
  // Key for each element: sym_x_y_z_rot_val
  const btnKeys = new Map();
  btnElems.forEach(e => {
    const k = `${e.sym}_${e.x}_${e.y}_${e.z}_${e.rot}_${e.pVal}`;
    btnKeys.set(k, (btnKeys.get(k) || 0) + 1);
  });

  const tblKeys = new Map();
  tblElems.forEach(e => {
    const k = `${e.sym}_${e.x}_${e.y}_${e.z}_${e.rot}_${e.pVal}`;
    tblKeys.set(k, (tblKeys.get(k) || 0) + 1);
  });

  const inBtnNotTbl = [];
  for (const [k, count] of btnKeys.entries()) {
    const tblCount = tblKeys.get(k) || 0;
    if (count > tblCount) {
      inBtnNotTbl.push(`${k} (Btn has ${count}, Tbl has ${tblCount})`);
    }
  }

  const inTblNotBtn = [];
  for (const [k, count] of tblKeys.entries()) {
    const btnCount = btnKeys.get(k) || 0;
    if (count > btnCount) {
      inTblNotBtn.push(`${k} (Tbl has ${count}, Btn has ${btnCount})`);
    }
  }

  console.log('\n--- ELEMENTS IN BUTTON BUT MISSING IN TABLE ---');
  inBtnNotTbl.forEach(x => console.log('MISSING: ' + x));

  console.log('\n--- ELEMENTS IN TABLE BUT NOT IN BUTTON ---');
  inTblNotBtn.forEach(x => console.log('EXTRA: ' + x));
}

runComparison();
