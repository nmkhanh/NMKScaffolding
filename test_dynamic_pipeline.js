// Test dynamic chaining algorithm
function simulateContinuous(rows) {
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

  // Chain state: current start points
  let curX0 = rows[0].StartX;
  let curY0 = rows[0].StartY;

  for (let r = 0; r < rows.length; r++) {
    const row = rows[r];
    const nextRow = (r < rows.length - 1) ? rows[r + 1] : null;
    const bayLens = row.BayLengths || [1800];
    const W_A = row.Width || 1200;
    const W_B = nextRow ? (nextRow.Width || 1200) : W_A;
    const cCase = row.ConnectCase || "None";

    console.log(`Building Row ${r}: ConnectCase=${cCase}, Bays=${bayLens.join(',')}, W=${W_A}`);
  }
}

const defaultRows = [
  { StartX: 0, StartY: 0, AngleDeg: 0, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "TH 1.1" },
  { StartX: 3600, StartY: 0, AngleDeg: 90, Width: 1200, BayLengths: [1800, 1200], ConnectCase: "TH 1.2" },
  { StartX: 3600, StartY: 4200, AngleDeg: 0, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "TH 1.3" },
  { StartX: 7200, StartY: 4200, AngleDeg: 270, Width: 1200, BayLengths: [1800, 1200], ConnectCase: "TH 1.4" },
  { StartX: 7200, StartY: 0, AngleDeg: 0, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "TH 2.1" },
  { StartX: 10800, StartY: 0, AngleDeg: 90, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "TH 2.2" },
  { StartX: 12600, StartY: 5400, AngleDeg: 0, Width: 1200, BayLengths: [1800, 1800], ConnectCase: "None" }
];

simulateContinuous(defaultRows);
