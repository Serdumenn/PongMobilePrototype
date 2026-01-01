# Pong Mobile Prototype (Unity)

A lightweight 2D Pong prototype built with Unity, designed for mobile (touch) controls.  
The project focuses on responsive paddle feel, consistent ball physics, and a clean iteration loop supported by CI.

> Unity Version: **6000.0.58f2** (can be changed via repo variable)

## What this project includes

This is not a “content-heavy” game project. It’s a compact prototype that demonstrates:

- **Physics-driven Pong** with predictable collisions  
- **Spin / angular response** based on paddle movement (feel + control)  
- **Mobile-first input** (touch controls)  
- **Simple scoring loop** suitable for quick iteration

---

## Continuous Integration (Android APK)

![Android Build](https://img.shields.io/github/actions/workflow/status/Serdumenn/PongMobilePrototype/build-android.yml?branch=main&label=Android%20Build&logo=unity&logoColor=white&style=for-the-badge)

This repository contains a GitHub Actions pipeline that builds an **Android APK**:

- Runs on **every push** to `main` (and can also be run manually)
- Produces a downloadable **APK artifact** on each successful run
- Surfaces build/import issues early (acts like a quality gate)
- Uses **Library caching** to reduce build time after the first run

📌 Setup guide Android APK CI Guide: "[docs/CI-ANDROID.md](docs/CI-ANDROID.md)" 📌


---

## Notes on Unity versioning

Unity releases frequently. To keep builds reproducible, the CI pipeline uses a fixed Unity version.  
If you use a different version, update the repository variable:

`Settings → Secrets and variables → Actions → Variables → UNITY_VERSION`
