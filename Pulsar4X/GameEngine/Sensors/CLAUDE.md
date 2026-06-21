# Sensors Subsystem — Developer Reference

**What it does:** Detects other entities in space. Every ship and planet radiates and reflects electromagnetic energy across different wavelengths. Sensors pick up that energy and, if it's strong enough at the sensor's tuned wavelength, register a contact. Think of it like a ship's ESM/RADAR suite — you're listening for energy in specific frequency bands.

**Why it matters for ground combat:** Sensors determine whether you can see enemy fleets approaching, which drives when you get to respond to an incoming invasion. The ground-unit survey component in Phase 4 will also reuse the survey-points mechanic (not the EM detection mechanic).

---

## Files

| File | Role |
|------|------|
| `SensorProfileDB.cs` | DataBlob on every detectable entity. Holds `EmittedEMSpectra` (what it broadcasts) and `ReflectedEMSpectra` (what it bounces back from active sensors). Keyed by `EMWaveForm`. |
| `SensorAbilityDB.cs` | DataBlob on entities that can sense. Holds `CurrentContacts` and `OldContacts` (dict of entity ID → `SensorReturnValues`). |
| `SensorReceiverAtb.cs` | Component attribute defining a sensor. Peak wavelength, bandwidth, best/worst sensitivity in kW, resolution, scan time. Added to ship/station via `AddComponent()`. |
| `SensorSignatureAtb.cs` | Component attribute defining an entity's signature (what it emits). |
| `SensorReflectionProcessor.cs` | `IHotloopProcessor` running every **1 hour**. Updates the `ReflectedEMSpectra` on all entities with `SensorProfileDB`. |
| `SensorScan.cs` | `IInstanceProcessor`. Fires on a per-entity schedule (set by `sensorAtb.ScanTime`). Does the actual detection — calls `SensorTools.GetDetectedEntites()`, updates contacts. |
| `SensorTools.cs` | Static helper with all the math: `AttenuatedForDistance()`, `DetectonQuality()`, `GetDetectedEntites()`. This is where signal strength drops off with distance. |
| `SensorReturnValues.cs` | Struct returned from detection: `SignalStrength_kW` and `SignalQuality` (0–1, how much detail you can infer). |
| `SystemSensorContacts.cs` | Per-system contact list. `GetSensorContacts(factionId)` returns the faction's current known entities. |
| `SensorEntityFactory.cs` | Creates/updates the contact entity a faction sees (the "blip" on the map). |
| `EMWaveForm.cs` | Defines a band of EM spectrum: min wavelength, peak, max. |
| `SensorInfoDB.cs` | Per-contact info blob: last detection time, latest/highest detection quality. |
| `SensorPositionDB.cs` | Stores the last-known position of a detected entity (may lag reality if not recently scanned). |

---

## How It Works

**Signal chain:**
1. Every entity has `SensorProfileDB` — it stores what frequencies it emits and reflects at what power (kW).
2. `SensorReflectionProcessor` updates reflected signatures every hour based on active illuminators nearby.
3. When a `SensorScan` fires for an entity with `SensorReceiverAtb`:
   - `SensorTools.GetDetectedEntites()` iterates every `SensorProfileDB` entity in the system.
   - For each target, `AttenuatedForDistance()` calculates how much signal survives the distance (inverse-square law).
   - `DetectonQuality()` compares the attenuated signal to the sensor's sensitivity at its tuned wavelength using a triangular overlap function.
   - If signal > threshold, a `SensorReturnValues` is produced: `SignalStrength_kW` (how strong) and `SignalQuality` (0.0–1.0 how much detail).
4. Contacts above threshold are added to `SensorAbilityDB.CurrentContacts` and `SystemSensorContacts`.

**Detection resolution:** Quality 0 = "something is there", Quality 1 = "full identification." Intermediate quality = partial information.

**Scan scheduling:** `SensorScan` reschedules itself: when it fires, it processes and then calls `ManagerSubpulses.AddEntityInterupt(now + scanTime, ...)`. So scan rate is determined by the sensor's `ScanTime` attribute.

---

## Key Formulas

Attenuation (from `SensorTools.AttenuationCalc`):
```
AttenuatedPower = sourcePower / (4π × distance²)
```

Detection quality: triangular overlap between sensor's waveform band and signal's waveform band. Signal must overlap the sensor's peak sensitivity region to score high quality.

---

## Pulsar Status vs Aurora

| Aurora concept | Pulsar | Status |
|----------------|--------|--------|
| Thermal signature (heat radiation) | `EmittedEMSpectra` at IR wavelengths | ✅ functional |
| EM signature (radar/active) | `ReflectedEMSpectra` + `SensorReflectionProcessor` | ✅ functional |
| Active vs passive sensors | `SensorReceiverAtb` tuned wavelength determines if it picks up emission or reflection | ✅ functional |
| Detection range scales with signature | `AttenuatedForDistance()` | ✅ functional |
| Contact quality (partial vs full ID) | `SignalQuality` 0–1 on `SensorReturnValues` | ✅ functional |
| EMCON (reduce your own signature) | Not verified — check if ships can zero out `EmittedEMSpectra` | ⚠️ unknown |
| Electronic warfare (jamming) | Not present | ❌ missing |

**Verdict: sensors are among the most complete subsystems in Pulsar.** The EM-spectrum physics approach is arguably more rigorous than Aurora's simpler thermal/EM bands. The main gaps are EMCON controls and EW jamming, neither of which are on the ground-combat critical path.

---

## Phase 4 Relevance

Ground forces have a sensor component (`SensorSignatureAtb`) that gives them an EM profile when in space transport. On the ground, sensor ranges become terrain-line-of-sight problems — a different mechanic from the space EM system. Do not reuse `SensorScan` for ground unit spotting; it's the wrong tool.
