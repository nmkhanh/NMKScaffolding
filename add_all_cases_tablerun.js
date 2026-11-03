const fs = require('fs');

let code = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', 'utf8');

const target = `                        // Neo hàng sau (rẽ +90 độ)
                        curOutX = p2MaxX;
                        curOutY = p2MaxY;
                        curInX = p2StartX;
                        curInY = p2StartY;
                        curUx = curVx; curUy = curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else
                    {`;

const replacement = `                        // Neo hàng sau (rẽ +90 độ)
                        curOutX = p2MaxX;
                        curOutY = p2MaxY;
                        curInX = p2StartX;
                        curInY = p2StartY;
                        curUx = curVx; curUy = curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 2.1")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);

                        var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH1)), curRot, "l", L_last);
                        var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH3)), curRot, "l", L_last);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH6)), curRot, "l", L_last);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH7)), curRot, "l", L_last);
                        if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                        if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                        if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        double L_first = (rowNext != null && rowNext.BayLengths != null && rowNext.BayLengths.Count > 0) ? rowNext.BayLengths[0] : 1800.0;
                        double pExtOutX = p1MaxX + nextW * curUx;
                        double pExtOutY = p1MaxY + nextW * curUy;
                        double pExtTopOutX = pExtOutX + L_first * curVx;
                        double pExtTopOutY = pExtOutY + L_first * curVy;
                        double pExtTopInX = p1MaxX + L_first * curVx;
                        double pExtTopInY = p1MaxY + L_first * curVy;

                        ensureCol(pExtOutX, pExtOutY);
                        ensureCol(pExtTopOutX, pExtTopOutY);
                        ensureCol(pExtTopInX, pExtTopInY);

                        placeTransom(p1MaxX, p1MaxY, zH1, curRot, nextW);
                        placeTransom(p1MaxX, p1MaxY, zDeck, curRot, nextW);
                        var rS1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH6)), curRot, "l", nextW);
                        var rS2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH7)), curRot, "l", nextW);
                        if (rS1 != null) placedIds.Add(rS1.Id.IntegerValue);
                        if (rS2 != null) placedIds.Add(rS2.Id.IntegerValue);

                        var bbExtE = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pExtOutX), MmToFt(pExtOutY), MmToFt(z0)), curRot + 90, "l", L_first);
                        var lgFExtE = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtOutX), MmToFt(pExtOutY), MmToFt(zH1)), curRot + 90, "l", L_first);
                        if (bbExtE != null) placedIds.Add(bbExtE.Id.IntegerValue);
                        if (lgFExtE != null) placedIds.Add(lgFExtE.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, pExtOutX, pExtOutY, curRot + 90, L_first, z0, jackH, opts);

                        double L_in = Math.Max(0, L_first - curW);
                        if (L_in > 0)
                        {
                            var bbInS = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(z0)), curRot + 90, "l", L_in);
                            var lgFInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH1)), curRot + 90, "l", L_in);
                            var lgMInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH3)), curRot + 90, "l", L_in);
                            var rInS1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH6)), curRot + 90, "l", L_in);
                            var rInS2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH7)), curRot + 90, "l", L_in);
                            if (bbInS != null) placedIds.Add(bbInS.Id.IntegerValue);
                            if (lgFInS != null) placedIds.Add(lgFInS.Id.IntegerValue);
                            if (lgMInS != null) placedIds.Add(lgMInS.Id.IntegerValue);
                            if (rInS1 != null) placedIds.Add(rInS1.Id.IntegerValue);
                            if (rInS2 != null) placedIds.Add(rInS2.Id.IntegerValue);
                        }

                        placeTransom(pExtTopInX, pExtTopInY, zH1, curRot, nextW);
                        placeTransom(pExtTopInX, pExtTopInY, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pExtTopInX, pExtTopInY, topZ, curRot, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1MaxX + 345 * curUx), MmToFt(p1MaxY + 345 * curUy), MmToFt(zDeck)), curRot + 90, "l", L_first);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pExtOutX - 345 * curUx), MmToFt(pExtOutY - 345 * curUy), MmToFt(zDeck)), curRot + 90, "l", L_first);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        curOutX = pExtTopOutX;
                        curOutY = pExtTopOutY;
                        curInX = pExtTopInX;
                        curInY = pExtTopInY;
                        curUx = curVx; curUy = curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 2.2")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);

                        var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH1)), curRot, "l", L_last);
                        var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH3)), curRot, "l", L_last);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH6)), curRot, "l", L_last);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH7)), curRot, "l", L_last);
                        if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                        if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                        if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        double L_first = (rowNext != null && rowNext.BayLengths != null && rowNext.BayLengths.Count > 0) ? rowNext.BayLengths[0] : 1800.0;
                        double pExtTopInX = p2MaxX + nextW * curUx;
                        double pExtTopInY = p2MaxY + nextW * curUy;
                        double pExtOutEndX = p2MaxX + L_first * (-curVx);
                        double pExtOutEndY = p2MaxY + L_first * (-curVy);
                        double pExtTopOutX = pExtTopInX + L_first * (-curVx);
                        double pExtTopOutY = pExtTopInY + L_first * (-curVy);

                        ensureCol(pExtTopInX, pExtTopInY);
                        ensureCol(pExtTopOutX, pExtTopOutY);
                        ensureCol(pExtOutEndX, pExtOutEndY);

                        placeTransom(p2MaxX, p2MaxY, zH1, curRot, nextW);
                        placeTransom(p2MaxX, p2MaxY, zDeck, curRot, nextW);
                        var rF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH6)), curRot, "l", nextW);
                        var rF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH7)), curRot, "l", nextW);
                        if (rF1 != null) placedIds.Add(rF1.Id.IntegerValue);
                        if (rF2 != null) placedIds.Add(rF2.Id.IntegerValue);

                        var bbExtT = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(z0)), curRot - 90, "l", L_first);
                        var lgFExtT = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH1)), curRot - 90, "l", L_first);
                        var lgMExtT = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH3)), curRot - 90, "l", L_first);
                        var rExtT1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH6)), curRot - 90, "l", L_first);
                        var rExtT2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH7)), curRot - 90, "l", L_first);
                        if (bbExtT != null) placedIds.Add(bbExtT.Id.IntegerValue);
                        if (lgFExtT != null) placedIds.Add(lgFExtT.Id.IntegerValue);
                        if (lgMExtT != null) placedIds.Add(lgMExtT.Id.IntegerValue);
                        if (rExtT1 != null) placedIds.Add(rExtT1.Id.IntegerValue);
                        if (rExtT2 != null) placedIds.Add(rExtT2.Id.IntegerValue);

                        double L_in = Math.Max(0, L_first - curW);
                        if (L_in > 0)
                        {
                            double pInStartX = p1MaxX;
                            double pInStartY = p1MaxY;
                            var bbInS = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pInStartX), MmToFt(pInStartY), MmToFt(z0)), curRot - 90, "l", L_in);
                            var lgFInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pInStartX), MmToFt(pInStartY), MmToFt(zH1)), curRot - 90, "l", L_in);
                            if (bbInS != null) placedIds.Add(bbInS.Id.IntegerValue);
                            if (lgFInS != null) placedIds.Add(lgFInS.Id.IntegerValue);
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, pInStartX, pInStartY, curRot - 90, L_in, z0, jackH, opts);
                        }

                        placeTransom(pExtOutEndX, pExtOutEndY, zH1, curRot, nextW);
                        placeTransom(pExtOutEndX, pExtOutEndY, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pExtOutEndX, pExtOutEndY, topZ, curRot, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2MaxX - 345 * curUx), MmToFt(p2MaxY - 345 * curUy), MmToFt(zDeck)), curRot - 90, "l", L_first);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pExtTopInX + 345 * curUx), MmToFt(pExtTopInY + 345 * curUy), MmToFt(zDeck)), curRot - 90, "l", L_first);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        curOutX = pExtTopOutX;
                        curOutY = pExtTopOutY;
                        curInX = pExtOutEndX;
                        curInY = pExtOutEndY;
                        curUx = -curVx; curUy = -curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot - 90.0;
                        curW = nextW;
                    }
                    else
                    {`;

code = code.replace(target, replacement);
fs.writeFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', code, 'utf8');
console.log('Added TH 2.1 and TH 2.2 into BuildTableChainedContinuous.');
