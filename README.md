# 🌾 Farmer's Companion — Agriculture & Livestock Suite for Raft

[![Version](https://img.shields.io/badge/Version-1.0.0-blue.svg?style=for-the-badge&logo=github)](https://github.com/RAVITEJAanand)
[![Raft Version](https://img.shields.io/badge/Raft-The%20Final%20Chapter%20(v1.0+)-brightgreen.svg?style=for-the-badge&logo=steam)](https://store.steampowered.com/app/648800/Raft/)
[![Discord](https://img.shields.io/badge/Discord-Join%20Community-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/B4EMrR5Vrf)

**Author**: KONDURI (RAVITEJAanand)  
**Game**: Raft (The Final Chapter Update 1.09 / v13.01)  
**Version**: 1.0.0  
**Framework**: BepInEx 5.4.21+, Unity 2020.3  
**Discord**: [Join our Modding Community](https://discord.gg/B4EMrR5Vrf)  
**Compatibility**: 100% compatible with **Sailor's Companion** and **Inventory Master** (Zero hotkey conflicts, zero performance degradation)

---

## 🌟 Overview (పరిచయం)

**Farmer's Companion** is an all-in-one Agriculture, Forestry, and Livestock Quality-of-Life automation suite for **Raft (The Final Chapter)**.

It handles features 16 through 30, eliminating repetitive manual chores like watering dozens of crop plots, chasing domestic animals with shears, or waiting hours for palm trees to grow:
- ⚓ **Sailor's Companion** ([Nexus Mods #155](https://www.nexusmods.com/raft/mods/155)): Navigation, Teleportation, Sails, Engines, 3D Island Sonar, Reef Mining.
- 🎒 **Inventory Master** ([Nexus Mods #156](https://www.nexusmods.com/raft/mods/156)): Auto Sort, 15-slot Backpack Expansion, Hotbar Row Swap, Drop Protection, Slot Lock.
- 🌾 **Farmer's Companion**: Auto Water, Smart Water, Auto Harvest, Auto Replant, Seed Saver, Faster Crop & Tree Growth, Livestock Wool/Milk Auto-Collect, and 3D Health Indicators.

---

## 🎮 Keybindings & Controls

| Hotkey | Action | Description |
| :--- | :--- | :--- |
| **`F1`** | **Farmer's Companion Menu** | Opens the rustic in-game farming automation menu (Zero clash with F2 or F3–F11). |

---

## 🚀 Features Breakdown (Features 16–30)

1. **💧 Auto Water Crops (Feature 16):** Automatically detects and refills dry crop plots within farm radius.
2. **🌾 Auto Harvest Crops (Feature 17):** Fully grown plants are harvested cleanly directly into your inventory or raft storage.
3. **🌱 Auto Replant Seeds (Feature 18):** Automatically selects and plants matching seeds from your inventory after a harvest.
4. **⚡ Faster Crop Growth (Feature 19):** Configurable growth multiplier (1.0x to 3.0x, default 1.3x) for balanced survival pacing.
5. **🌴 Tree Growth Boost (Feature 20):** Accelerates growth of palm trees and mango/fruit trees so you never run out of wood and leaves.
6. **🐑 Animal Feeding Auto (Feature 21):** Automatically waters grass plots (`Cropplot_Grass`) so livestock (llamas, goats, clackers) eat on time.
7. **🥛 Milk & Wool Auto Collect (Feature 22):** Automatically shears llamas for wool and milks goats when their resources are ready—no more chasing animals!
8. **📊 Crop Health Indicator (Feature 23):** Displays floating 3D billboard indicators above plots showing hydration status and growth percentage.
9. **🎯 Farming Range Boost (Feature 24):** Configurable range slider (10m to 60m) to manage entire multi-deck raft farms seamlessly.
10. **⚡ Fertilizer Boost System (Feature 25):** Optional fertilizer surge mode granting an extra 1.5x speed boost to all crops.
11. **💡 Smart Water Usage (Feature 26):** Only waters plots that are genuinely dry (`SlotsNeedWater()`), preventing waste.
12. **🧹 Multi-Harvest Mode (Feature 27):** One-click button in the `F1` menu to instantly sweep and harvest all ripe crops across your farm.
13. **🌰 Seed Saver Mode (Feature 28):** Configurable chance (default 25%) to preserve/refund planted seeds.
14. **🔔 Farming Notifications (Feature 29):** Non-intrusive on-screen toasts when crops are ripe or livestock products are ready.
15. **⚙️ Farming Area Toggle (Feature 30):** Full in-game control canvas via **`F1`** with individual feature toggles and sliders.

---

## 🛡️ Performance & Thermal Safety Guarantee

- **Zero FPS Drop**: No object scans are run inside `Update()`.
- **Cooling Friendly**: All farm sweeps run on low-frequency background coroutines (every 2–3.5s) using `sqrMagnitude` math (no square roots), maintaining virtually 0.0% CPU overhead.

---

## 📥 Installation

1. Install **BepInEx 5.4.23 (x64)** into your Raft directory.
2. Place `FarmersCompanion.dll` in `Raft/BepInEx/plugins/FarmersCompanion/`.
3. Launch Raft and press **`F1`** in-game!
