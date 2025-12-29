# Unity Personal with GitHub Actions: Android APK CI (User Guide)

This document explains how to generate an **Android APK** via GitHub Actions for a project using a Unity **Personal** license.

With this setup:
- You get an automatic build after every `push` (you can also run it manually if you wish).
- If the build fails, you'll see **which step caused the failure** in the logs.
- If successful, it will produce a **downloadable APK artifact**.

Note: This project is a great example of a workflow. I used this old project, which I pulled from dusty shelves, to write the workflow (with the help of various editors and AI). You are welcome to examine the project as you wish. Integrating the workflow into your project is quite simple. I tried to make it as detailed and understandable as possible. If you have any questions, please don't hesitate to contact me. I am open to any feedback to continue improving the process. Thank you for your interest.

---

## 1) Required file
The repository should contain the following workflow file:

- `.github/workflows/personal-build-android.yml`

This file runs the Unity build on Docker and uploads the resulting APK as an artifact.

---

## 2) Settings to enter on GitHub

Repo → **Settings** → **Secrets and variables** → **Actions**

### 2.1 Secrets (required)
These three are the most practical and stable method:

- `UNITY_LICENSE` (the most critical one)
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

> `UNITY_SERIAL` is **not used** in the Personal workflow.

### 2.2 Variables (if using a workflow)
If they are read as env/with in the workflow, you need to enter them. Recommended:

- `UNITY_VERSION` (e.g., `6000.0.58f2`)
- `BUILD_NAME` (e.g., `MyGame`)
- `BUILDS_PATH` (e.g., `builds`)

---

## 3) How to prepare UNITY_LICENSE? (Personal)

Purpose: To place the contents of the license file provided by Unity into a GitHub secret.

### 3.1 Get the “request file” from your computer (Unity_lic.ulf)
1) Open Unity Hub.
2) Open and close any Unity Editor once (if necessary).
3) Go to:
   `C:\ProgramData\Unity\`
4) Find this file:
   `Unity_lic.ulf`

> `ProgramData` may be hidden, but you can type the folder name directly into the address bar to access it.

### 3.3 Add to GitHub Secret
1) Open the downloaded license file with **Notepad**.
2) Copy all the text inside.
3) GitHub → Secrets → `UNITY_LICENSE` → Paste into the Value field → Save.

That's it. From now on, if the workflow asks for a license, it will activate with `UNITY_LICENSE`.

---

## 4) Where can I find the Unity version (UNITY_VERSION)?

The safest method:
- Unity Hub → Installs → Copy the relevant Editor version (e.g., `6000.0.58f2`)

Alternative:
- It is written in `ProjectSettings/ProjectVersion.txt` in the project.

> Everyone's Unity version may be different. Therefore, the version parameter in the workflow must match the project version.

---

## 5) How do I run a workflow?

### Automatically
- Runs after every `push` to `main` (or the target branch).

### Manually
- Repo → **Actions** → relevant workflow → **Run workflow**

---

## 6) How do I download the APK?
Go to the Workflow run screen:
- Extract the APK in the **Artifacts** section.
- Download it and transfer it to your device.

> An APK size between 30–100 MB is normal. Due to the Unity runtime + additional assets, it's common to see 40 MB even in small projects.

---

## 7) Why does the first build take longer?

On the first run:
- The Docker image is pulled,
- The Unity Library is built from scratch.

On subsequent runs:
- If the **cache** is enabled, the time is significantly reduced.

> The cache is stored in GitHub Actions' “cache storage” mechanism (per repository/key). If you run the same project in a different repository, the cache starts from scratch; therefore, it is normal for the first build to take longer.

---

## 8) Common errors and quick fixes

### “Library folder does not exist...”
- This is not an error, but a warning. It is normal during the first build.

### Build takes too long
- The first run is usually long.
- Check the log to see if the cache step is working correctly:
  - You should see lines like “Cache restored” / “Cache saved”.

---

## 9) Security note (short)
- `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` should only be stored in **GitHub Secrets**.
- Never put them in the repo.
- Do not print the contents of Secrets to the log (the workflow should not do this).