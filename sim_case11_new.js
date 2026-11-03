const fs = require('fs');

function countCase11New() {
  const x0 = 0, y0 = 0, z0 = 0, W = 1200, jackH = 250, storyCount = 1;
  const elements = [];
  function place(sym, x, y, z, rot, pName, pVal) {
    elements.push({ sym, x: Math.round(x), y: Math.round(y), z: Math.round(z), rot: Math.round(rot), pName, pVal: Math.round(pVal || 0) });
  }

  // 1. colX = [0, 1800]
  const colX = [0, 1800];
  for (let k = 0; k < colX.length; k++) {
    const px = colX[k];
    const isOuterEnd = (k === 0 || k === colX.length - 1);
    place('2', px, y0, z0, 0, 'h', jackH);
    place('2', px, y0 - W, z0, 0, 'h', jackH);
    place('3', px, y0, z0 + jackH, 0);
    place('3', px, y0 - W, z0 + jackH, 0);

    const zH1 = z0 + jackH + 135;
    place('4', px, y0, zH1, -90, 'l', W);
    for (let s = 0; s < storyCount; s++) {
      const sDeck = z0 + jackH + 1730 + s * 1800;
      place('4', px, y0, sDeck, -90, 'l', W);
    }
    if (isOuterEnd) {
      for (let s = 0; s < storyCount; s++) {
        const sOff = s * 1800;
        place('4', px, y0, z0 + jackH + 2180 + sOff, -90, 'l', W);
        place('4', px, y0, z0 + jackH + 2630 + sOff, -90, 'l', W);
        place('7', px, y0, z0 + jackH + 1730 + sOff, -90, 'l', W);
      }
    }
  }

  // Bay 1: xBay = 0, L = 1800
  const xBay5 = 0, L_bay5 = 1800;
  place('1', xBay5, y0 - W, z0, 0, 'l', L_bay5);
  const zH1_5 = z0 + jackH + 135;
  place('4', xBay5, y0 - W, zH1_5, 0, 'l', L_bay5);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    place('5', xBay5, y0 - W, z0 + jackH + 135, 0, 'l', L_bay5);
    const zDeck = z0 + jackH + 1730 + sOff;
    place('7', xBay5, y0 - W + 66, zDeck, 0, 'l', L_bay5);
    place('6', xBay5, y0 - 345, zDeck, 0, 'l', L_bay5);
    place('6', xBay5, y0 - W + 345, zDeck, 0, 'l', L_bay5);
  }

  // Cột cấy xC = 600
  const xC = 600;
  place('2', xC, y0, z0, 0, 'h', jackH);
  place('3', xC, y0, z0 + jackH, 0);

  // Mép trước thu ngắn L_cut = 600
  const L_cut = 600;
  place('1', xBay5, y0, z0, 0, 'l', L_cut);
  place('4', xBay5, y0, zH1_5, 0, 'l', L_cut);
  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    place('4', xBay5, y0, z0 + jackH + 830 + sOff, 0, 'l', L_cut);
    place('4', xBay5, y0, z0 + jackH + 2180 + sOff, 0, 'l', L_cut);
    place('4', xBay5, y0, z0 + jackH + 2630 + sOff, 0, 'l', L_cut);
    const zDeck = z0 + jackH + 1730 + sOff;
    place('7', xBay5, y0 - 66, zDeck, 0, 'l', L_cut);
  }

  // Nhánh rẽ: xMax = 1800, yTop = 1800, L_return = 1800, W_return = 1200
  const xMax = 1800, yTop = 1800, L_return = 1800, W_return = 1200;
  place('1', xC, y0, z0, 90, 'l', L_return);
  place('1', xMax, y0, z0, 90, 'l', L_return);
  place('2', xC, yTop, z0, 0, 'h', jackH);
  place('2', xMax, yTop, z0, 0, 'h', jackH);
  place('3', xC, yTop, z0 + jackH, 0);
  place('3', xMax, yTop, z0 + jackH, 0);

  const zH1_R = z0 + jackH + 135;
  place('4', xC, y0, zH1_R, 90, 'l', L_return);
  place('4', xMax, y0, zH1_R, 90, 'l', L_return);
  place('4', xC, y0, zH1_R, 0, 'l', W_return);
  place('4', xC, yTop, zH1_R, 0, 'l', W_return);

  for (let s = 0; s < storyCount; s++) {
    const sOff = s * 1800;
    place('4', xC, y0, z0 + jackH + 830 + sOff, 90, 'l', L_return);
    place('5', xMax, y0, z0 + jackH + 135, 90, 'l', L_return);

    const zDeck_R = z0 + jackH + 1730 + sOff;
    place('4', xC, y0, zDeck_R, 0, 'l', W_return);
    place('4', xC, yTop, zDeck_R, 0, 'l', W_return);

    place('6', xC + 345, y0, zDeck_R, 90, 'l', L_return);
    place('6', xMax - 345, y0, zDeck_R, 90, 'l', L_return);

    place('7', xC + 66, y0, zDeck_R, 90, 'l', L_return);
    place('7', xMax - 66, y0, zDeck_R, 90, 'l', L_return);
    place('7', xC, yTop, zDeck_R, 0, 'l', W_return);

    place('4', xC, y0, z0 + jackH + 2180 + sOff, 90, 'l', L_return);
    place('4', xC, yTop, z0 + jackH + 2180 + sOff, 0, 'l', W_return);
    place('4', xC, y0, z0 + jackH + 2630 + sOff, 90, 'l', L_return);
    place('4', xC, yTop, z0 + jackH + 2630 + sOff, 0, 'l', W_return);
  }

  console.log("Total elements in new ExecuteCase1_1:", elements.length);
  return elements;
}

countCase11New();
