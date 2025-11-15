# Physical Model Alignment Review

This document cross-checks the implemented telemetry simulator against the provided physical model description.

## 1. Power Subsystem

* **Currents and load composition.** The default node configuration pins the microcontroller, network controller, per-speaker draw, grid charge current, and nominal capacity to 0.125 A, 0.125 A, 0.5 A, 3 A, and 26 Ah respectively, exactly as listed in the model description.【F:TelemetryGenerator.Core/Configuration/NodeConfig.cs†L3-L25】
* **Load aggregation.** Each simulation step sums the fixed controller currents with the non-negative amplifier draw to form the total DC load passed into the battery integrator, reproducing the specified formulas for `I_load` / `I_discharge`.【F:TelemetryGenerator.Core/TelemetryGenerator.cs†L61-L64】【F:TelemetryGenerator.Core/TelemetryGenerator.cs†L104-L108】
* **Battery charge/discharge (Ah model).** Charge is accumulated or depleted in ampere-hours via `dt.TotalHours`, using the 3 A grid current when charging and subtracting the calculated load current with an additional Peukert-like multiplier once the draw exceeds 1 A before clamping to the effective capacity, matching the Ah-model narrative.【F:TelemetryGenerator.Core/Models/BatteryModel.cs†L6-L31】
* **Voltage vs. SOC mapping.** A three-segment piecewise linear SOC-to-voltage curve spans the same ranges as the document (27.5–25.5 V, 25.5–23.5 V, 23.5 V down to the cutoff), so high SOC moves slowly while low SOC collapses more steeply.【F:TelemetryGenerator.Core/Models/BatteryModel.cs†L33-L63】
* **Cutoff handling and hysteresis.** When voltage drops to the cutoff (21 V) the battery status is forced false and, if the grid is unavailable, the power module also disables sound and the amplifier. A 0.05 V hysteresis keeps `BatteryStatusOk` false until the voltage recovers slightly, preventing chatter around the threshold.【F:TelemetryGenerator.Core/Models/BatteryModel.cs†L25-L39】【F:TelemetryGenerator.Core/Modules/PowerSupplyModule.cs†L22-L43】

## 2. Audio Path

* **Amplifier load model.** The amplifier draws current only when sound is active and either mains or battery power are present, scales consumption with the effective speaker count at 0.5 A per speaker, and injects Gaussian noise to avoid unrealistically flat values—mirroring the described behaviour.【F:TelemetryGenerator.Core/Modules/AmplifierModule.cs†L6-L45】
* **Diagnostics exposure.** Telemetry samples expose amplifier status, instantaneous draw, configured speakers, and sound status so downstream consumers can detect open lines, partial failures, or overloads exactly as outlined in the specification.【F:TelemetryGenerator.Core/TelemetryGenerator.cs†L118-L145】

## 3. Thermal Behaviour

* **Outside temperature cycle.** The temperature model uses a sinusoidal 24-hour cycle with random noise, representing daily ambient variation.【F:TelemetryGenerator.Core/Models/TemperatureModel.cs†L6-L15】
* **Inside temperature dynamics.** Internal temperature approaches a load-dependent target via an exponential (RC-style) blend factor derived from the elapsed minutes, while cooling efficiency and amplifier load factor modulate the target—exactly the smoothing behaviour requested.【F:TelemetryGenerator.Core/Models/TemperatureModel.cs†L17-L24】【F:TelemetryGenerator.Core/Modules/EnvironmentModule.cs†L40-L70】
* **Door impact.** Door open/close is randomised per the configured probabilities; when open the load factor is reduced by 10 % before computing the thermal response, which captures the improved cooling noted in the description.【F:TelemetryGenerator.Core/Modules/EnvironmentModule.cs†L26-L68】

## 4. Network Behaviour

* **State-based network model.** The network model keys off the active anomaly type: during `NetDegradation` it gradually worsens signal strength and injects latency spikes, while `NetControllerFailure` collapses signal to ≤−105 dBm and fixes latency at 9999 ms. In the absence of anomalies it emits nominal values with noise. This event-driven approach covers the same degraded/failure regimes as the specification, albeit via injected anomalies instead of a continuous Markov chain.【F:TelemetryGenerator.Core/Models/NetworkModel.cs†L6-L36】【F:TelemetryGenerator.Core/Services/AnomalyInjector.cs†L34-L78】【F:TelemetryGenerator.Core/Services/AnomalyInjector.cs†L97-L167】

## 5. Disk Usage

* **Log accumulation.** Disk usage is incremented by a base growth rate each minute plus an extra term when sound is active, with Gaussian noise and clamping to 0–100 %, capturing the stated “faster growth during broadcasts” effect.【F:TelemetryGenerator.Core/Modules/MicroControllerModule.cs†L41-L71】

## 6. Missing Higher-Level Telemetry (Section 7)

The current implementation does not provide the extended operational, software-health, configuration, historical, or supervisory labels outlined in section 7 of the description. Notably, the node state and emitted telemetry lack uptime counters, restart history, software health flags, version identifiers, aggregated fault counters, maintenance/incident logs beyond a simple queue, or explicit health-state labels.【F:TelemetryGenerator.Core/State/NodeState.cs†L5-L37】【F:TelemetryGenerator.Core/Telemetry/TelemetrySample.cs†L5-L18】 These additions would still be required to deliver the full AI-focused dataset.

## Conclusion

Sections 1–5 of the physical model are directly represented in code, including detailed behaviour for power, audio, thermal, network, and storage subsystems. However, the additional AI-oriented telemetry from section 7 is currently absent and would require further development.
