// Test dynamic full continuous builder
function buildDynamicScaffolding(rows) {
  const placedCols = new Set();
  const placedTransoms = new Set();
  const elements = [];

  const z0 = 0, jackH = 250;
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

  // Anchor state for the chain
  // P_out: outer post of starting transverse line
  // P_in:  inner post of starting transverse line
  let P_out = { x: rows[0].StartX, y: rows[0].StartY };
  let W0 = rows[0].Width || 1200;
  let ang0Rad = (rows[0].AngleDeg || 0) * Math.PI / 180.0;
  let u0x = Math.Cos ? Math.Cos(ang0Rad) : Math.cos(ang0Rad);
  let u0y = Math.Sin ? Math.Sin(ang0Rad) : Math.sin(ang0Rad);
  let v0x = -u0y, v0y = u0x; // +90 deg perpendicular pointing towards front/inner
  let P_in = { x: P_out.x + W0 * v0x, y: P_out.y + W0 * v0y };

  for (let r = 0; r < rows.length; r++) {
    const row = rows[r];
    const nextRow = (r < rows.length - 1) ? rows[r + 1] : null;
    const bayLens = (row.BayLengths && row.BayLengths.length > 0) ? row.BayLengths : [1800];
    const W_A = row.Width || 1200;
    const W_B = nextRow ? (nextRow.Width || 1200) : W_A;
    const cCase = row.ConnectCase || "None";
    const hasNext = (nextRow !== null) && (cCase !== "None");

    const angRad = (row.AngleDeg || 0) * Math.PI / 180.0;
    const ux = Math.cos(angRad), uy = Math.sin(angRad);
    const vx = -uy, vy = ux; // perpendicular towards inner

    const nBays = bayLens.length;
    const nStandard = hasNext ? (nBays - 1) : nBays;

    // Start of row: ensure start columns
    ensureCol(P_out.x, P_out.y);
    ensureCol(P_in.x, P_in.y);

    // If first row: close the beginning
    if (r === 0) {
      let tRot = (row.AngleDeg || 0) + 90;
      placeSharedTransom(P_out.x, P_out.y, tRot, W_A);
      place('4', P_out.x, P_out.y, zH6, tRot, 'l', W_A);
      place('4', P_out.x, P_out.y, zH7, tRot, 'l', W_A);
      place('7', P_out.x, P_out.y, zDeck, tRot, 'l', W_A);
    }

    // Build standard bays
    let curDist = 0;
    for (let b = 0; b < nStandard; b++) {
      const bLen = bayLens[b];
      const p1_x = P_out.x + curDist * ux;
      const p1_y = P_out.y + curDist * uy;
      const p2_x = P_in.x + curDist * ux;
      const p2_y = P_in.y + curDist * uy;

      const nextDist = curDist + bLen;
      const p1_next_x = P_out.x + nextDist * ux;
      const p1_next_y = P_out.y + nextDist * uy;
      const p2_next_x = P_in.x + nextDist * ux;
      const p2_next_y = P_in.y + nextDist * uy;

      ensureCol(p1_next_x, p1_next_y);
      ensureCol(p2_next_x, p2_next_y);

      // Transom at end of this bay
      let tRot = (row.AngleDeg || 0) + 90;
      placeSharedTransom(p1_next_x, p1_next_y, tRot, W_A);

      // Longitudinal elements
      // Outer side
      place('1', p1_x, p1_y, z0, row.AngleDeg || 0, 'l', bLen);
      place('4', p1_x, p1_y, zH1, row.AngleDeg || 0, 'l', bLen);
      placeBayDiagonalBraces(p1_x, p1_y, row.AngleDeg || 0, bLen);
      place('7', p1_x + 66 * vx, p1_y + 66 * vy, zDeck, row.AngleDeg || 0, 'l', bLen);

      // Inner side
      place('1', p2_x, p2_y, z0, row.AngleDeg || 0, 'l', bLen);
      place('4', p2_x, p2_y, zH1, row.AngleDeg || 0, 'l', bLen);
      place('4', p2_x, p2_y, zH3, row.AngleDeg || 0, 'l', bLen);
      place('4', p2_x, p2_y, zH6, row.AngleDeg || 0, 'l', bLen);
      place('4', p2_x, p2_y, zH7, row.AngleDeg || 0, 'l', bLen);
      place('7', p2_x - 66 * vx, p2_y - 66 * vy, zDeck, row.AngleDeg || 0, 'l', bLen);

      // Deck planks
      place('6', p1_x + 345 * vx, p1_y + 345 * vy, zDeck, row.AngleDeg || 0, 'l', bLen);
      place('6', p2_x - 345 * vx, p2_y - 345 * vy, zDeck, row.AngleDeg || 0, 'l', bLen);

      curDist = nextDist;
    }

    // Now handle the last bay: either corner or end
    if (hasNext) {
      const L_last = bayLens[nBays - 1];
      const p1_start_x = P_out.x + curDist * ux;
      const p1_start_y = P_out.y + curDist * uy;
      const p2_start_x = P_in.x + curDist * ux;
      const p2_start_y = P_in.y + curDist * uy;

      if (cCase === "TH 1.1") {
        const L_cut = Math.max(0, L_last - W_B);
        const p1_max_x = p1_start_x + L_last * ux;
        const p1_max_y = p1_start_y + L_last * uy;
        const p2_max_x = p2_start_x + L_last * ux;
        const p2_max_y = p2_start_y + L_last * uy;

        const p_c_x = p2_start_x + L_cut * ux;
        const p_c_y = p2_start_y + L_cut * uy;

        ensureCol(p1_max_x, p1_max_y);
        ensureCol(p_c_x, p_c_y);
        ensureCol(p2_max_x, p2_max_y);

        // End cap at p1_max across W_A
        let tRot = (row.AngleDeg || 0) + 90;
        placeSharedTransom(p1_max_x, p1_max_y, tRot, W_A);
        place('4', p1_max_x, p1_max_y, zH6, tRot, 'l', W_A);
        place('4', p1_max_x, p1_max_y, zH7, tRot, 'l', W_A);
        place('7', p1_max_x, p1_max_y, zDeck, tRot, 'l', W_A);

        // Outer side full L_last
        place('1', p1_start_x, p1_start_y, z0, row.AngleDeg || 0, 'l', L_last);
        place('4', p1_start_x, p1_start_y, zH1, row.AngleDeg || 0, 'l', L_last);
        placeBayDiagonalBraces(p1_start_x, p1_start_y, row.AngleDeg || 0, L_last);
        place('7', p1_start_x + 66 * vx, p1_start_y + 66 * vy, zDeck, row.AngleDeg || 0, 'l', L_last);

        // Inner side cut to L_cut
        if (L_cut > 0) {
          place('1', p2_start_x, p2_start_y, z0, row.AngleDeg || 0, 'l', L_cut);
          place('4', p2_start_x, p2_start_y, zH1, row.AngleDeg || 0, 'l', L_cut);
          place('4', p2_start_x, p2_start_y, zH3, row.AngleDeg || 0, 'l', L_cut);
          place('4', p2_start_x, p2_start_y, zH6, row.AngleDeg || 0, 'l', L_cut);
          place('4', p2_start_x, p2_start_y, zH7, row.AngleDeg || 0, 'l', L_cut);
          place('7', p2_start_x - 66 * vx, p2_start_y - 66 * vy, zDeck, row.AngleDeg || 0, 'l', L_cut);
        }

        // Doorway transom across W_B (along row direction)
        placeSharedTransom(p_c_x, p_c_y, row.AngleDeg || 0, W_B);

        // Deck planks
        place('6', p1_start_x + 345 * vx, p1_start_y + 345 * vy, zDeck, row.AngleDeg || 0, 'l', L_last);
        place('6', p2_start_x - 345 * vx, p2_start_y - 345 * vy, zDeck, row.AngleDeg || 0, 'l', L_last);

        // Anchor next row: turns +90 deg (+Y)
        // Next row outer is at p2_max, inner is at p_c
        P_out = { x: p2_max_x, y: p2_max_y };
        P_in = { x: p_c_x, y: p_c_y };
      }
    } else {
      // Last row: close outer end
      let tRot = (row.AngleDeg || 0) + 90;
      place('4', P_out.x + curDist * ux, P_out.y + curDist * uy, zH6, tRot, 'l', W_A);
      place('4', P_out.x + curDist * ux, P_out.y + curDist * uy, zH7, tRot, 'l', W_A);
      place('7', P_out.x + curDist * ux, P_out.y + curDist * uy, zDeck, tRot, 'l', W_A);
    }
  }

  return { elements, placedCols };
}

// Test with 2 rows default:
const res = buildDynamicScaffolding([
  { StartX: 0, StartY: 0, AngleDeg: 0, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "TH 1.1" },
  { StartX: 3600, StartY: 1200, AngleDeg: 90, Width: 1200, BayLengths: [1800], ConnectCase: "None" }
]);

console.log("Total dynamic elements placed:", res.elements.length);
console.log("Total columns placed:", res.placedCols.size);
