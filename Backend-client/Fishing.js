const findFishingPoints = (castDist) => {

    const MaxCastingDist = Math.min(maxCastingDist, 50);
    castDist = Math.min(Number(castDist.toFixed(2)), MaxCastingDist);

    const player = mp.players.local;
    const pos = player.position;

    const heading = player.getHeading();
    const rad = (heading * Math.PI) / 180;
    const forward = { x: -Math.sin(rad), y: Math.cos(rad) };

    let fromPos = null;
    let maxPos = null;
    let startPos = null;

    const checkPoint = (x, y) => {
        const playerPos = mp.players.local.position;
        const rayStart = new mp.Vector3(x, y, playerPos.z + 1.0);

        let waterZ = null;
        let waterProbeResult = mp.game.water.testVerticalProbeAgainstAllWater(x, y, rayStart.z, 0);
        if (waterProbeResult && waterProbeResult > -50.0 && waterProbeResult < (playerPos.z + 2.0)) {
            waterZ = waterProbeResult;
        }

        if (waterZ === null) {
            let h = mp.game.water.getWaterHeight(x, y, rayStart.z, 0);
            if (typeof h === 'object') h = h.z;
            if (h !== undefined && h > -50.0 && h < (playerPos.z + 2.0)) {
                waterZ = h;
            }
        }

        if (waterZ === null) return null;

        const rayEnd = new mp.Vector3(x, y, waterZ);


        const rayFlags = 1 + 16 + 256;

        const ignoredEntity = mp.players.local.handle;

        const hitData = mp.raycasting.testPointToPoint(rayStart, rayEnd, ignoredEntity, rayFlags);

        if (hitData && hitData.position) {
            return null;
        }
        return new mp.Vector3(x, y, waterZ);
    };

    let distToMin = 0;
    for (let d = 1.0; d <= 15.0; d += 0.5) {
        let testX = pos.x + forward.x * d;
        let testY = pos.y + forward.y * d;

        let hit = checkPoint(testX, testY);
        if (hit) {
            let safeD = d + 1.0;
            if (safeD + 4 > MaxCastingDist) return 1;
            fromPos = checkPoint(pos.x + forward.x * safeD, pos.y + forward.y * safeD) || hit;
            if (fromPos) fromPos.z -= 0.2;

            distToMin = safeD;
            break;
        }
    }

    if (!fromPos) return 1;

    let maxWaterDist = distToMin;
    for (let d = (distToMin + 50); d >= (distToMin + 4.0); d -= 0.5) {
        let testX = pos.x + forward.x * d;
        let testY = pos.y + forward.y * d;

        let hit = checkPoint(testX, testY);
        if (hit) {

            maxWaterDist = d - 2.0;

            maxPos = checkPoint(pos.x + forward.x * maxWaterDist, pos.y + forward.y * maxWaterDist) || hit;
            if (maxPos) {
                if (Math.abs(maxPos.z - fromPos.z) > 0.5) maxPos.z = fromPos.z;
                maxPos.z -= 0.05;
            }
            break;
        }
    }

    let waterAvailable = maxWaterDist - distToMin;
    if (waterAvailable < 4.0 || !maxPos) return 1;

    if (castDist > waterAvailable) {
        castDist = waterAvailable;
    }

    if (castDist < 4.0) {
        castDist = 4.0;
    }

    let startDist = distToMin + castDist;
    let targetX = pos.x + forward.x * startDist;
    let targetY = pos.y + forward.y * startDist;

    startPos = checkPoint(targetX, targetY);

    if (!startPos) {
        startPos = new mp.Vector3(targetX, targetY, fromPos.z);
    } else {
        if (Math.abs(startPos.z - fromPos.z) > 0.5) startPos.z = fromPos.z;
    }
    startPos.z -= 0.2;

    const finalCurrentDist = mp.game.system.vdist(fromPos.x, fromPos.y, 0.0, startPos.x, startPos.y, 0.0);
    const finalMaxDist = mp.game.system.vdist(fromPos.x, fromPos.y, 0.0, maxPos.x, maxPos.y, 0.0);

    return {
        from: fromPos,
        start: startPos,
        currentDist: finalCurrentDist,
        maxDist: finalMaxDist
    };
};