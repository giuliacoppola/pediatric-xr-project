# EMBRACE-XR — Pediatric XR Project

A **Mixed Reality experience for Meta Quest 3**, built in Unity, designed to reduce anticipatory anxiety in children during blood-draw procedures. The child is guided through an immersive space-mission story — a rare fuel, the "Scintilla," can only be provided by a brave child's blood — while a guided-breathing mini-game and voice narration (from the in-experience AI, ARIA) turn the procedure itself into an active, predictable part of the mission rather than something that just happens to the child. A colour-passthrough view keeps the child aware of the real room and caregivers, and a hospital operator follows and paces the session from a web control panel synced to the real procedure.

This was developed as a **university group project**, in collaboration with two other students, grounded in five semi-structured interviews with pediatric clinicians (design rationale, interview findings, and evaluation plan are in the full paper — see [`docs/08_Paper_EMBRACE-XR.pdf`](docs/08_Paper_EMBRACE-XR.pdf)).

> 📄 **Full paper:** [`docs/08_Paper_EMBRACE-XR.pdf`](docs/08_Paper_EMBRACE-XR.pdf) — *"EMBRACE-XR: un sistema XR interattivo pediatrico per la riduzione dell'ansia da prelievo"*. It covers the clinical problem, the design goals, how each interview finding maps to a specific design choice, the full UX flow, and the system architecture (scene structure, `GameManager`/`WaitingRoomManager`/`MissionController`/`OperatorServer`) in much more depth than this README.

## Features

- **Space-mission narrative**: countdown, rocket launch, atmosphere/flight sequence, planets to explore, reaching a "home planet" as the session's goal.
- **Breathing & stillness guidance**: in-experience mechanics (`BreathingMechanic`, `StillnessGuide`, `PositioningGuide`) that translate the calming techniques used during the procedure into game feedback.
- **Voice guidance** (`VoiceGuide`) accompanying the child through each phase of the experience.
- **Operator control panel**: a tablet-side client/server link (`OperatorServer`, `TabletServer`) so hospital staff can monitor and control the session in real time without wearing the headset.
- **Waiting-room mode** (`WaitingRoomManager`/`WaitingRoomUI`) for the moments before the procedure starts.

## Tech stack

- **Unity 6000.4.8f1** (Unity 6), Universal Render Pipeline (URP)
- **Meta XR SDK** (Core + Interaction) for Meta Quest 3
- **Unity XR Interaction Toolkit**, **XR Hands**, **AR Foundation**, **OpenXR** (incl. Android XR OpenXR)
- **Unity Input System**
- C# gameplay/UI/networking scripts under `Assets/Scripts`

## Repository structure

```
Assets/             Unity scenes, scripts, and (free/original) third-party art & audio packages used in the experience
Packages/           Unity Package Manager manifest (dependencies listed above)
ProjectSettings/    Unity project configuration (render pipeline, XR, input, build settings)
docs/               Project documentation (see below)
```

`docs/` contains:

| File | Content |
|---|---|
| `01_Documentazione_Progetto.pdf` | Full project documentation |
| `02_Game_Design_Iniziale.pdf` | Initial game design document |
| `03_Rassegna_Paper.xlsx` | Literature/paper review |
| `07_Video_Demo_Progetto.mp4` | Project demo video |
| `08_Paper_EMBRACE-XR.pdf` | Full project paper: clinical motivation, interview findings, design rationale, UX flow, and system architecture |

> **Note:** the raw interview transcripts and thematic analysis are not included in this repository, since they identify the interviewed clinicians by name. The paper (`08_Paper_EMBRACE-XR.pdf`) summarizes the same findings anonymously, by clinical role only.

## System requirements

- **Unity Hub** with **Unity Editor 6000.4.8f1** installed (exact version matters for XR/URP compatibility — install it from Unity Hub if you don't have it, it will offer to match the version in `ProjectSettings/ProjectVersion.txt`).
- **Android Build Support** module for Unity (with OpenJDK, Android SDK & NDK — installable directly from Unity Hub's module selector), required to build for Meta Quest 3.
- A **Meta Quest 3** headset with **Developer Mode** enabled, or the **Meta Quest Link** app for testing via a PC connection.
- (Optional, for deployment/debugging) **Meta Quest Developer Hub** and Android **ADB**.

## Setup

1. **Clone the repository:**
   ```
   git clone <this-repo-url>
   ```
2. **Open the project in Unity Hub** — select "Add project from disk" and point it at the cloned folder. Unity Hub will prompt to install `6000.4.8f1` if it's missing.
3. **Switch the build target to Android** (`File > Build Settings > Android > Switch Platform`) if it isn't already selected.
4. **Check the XR Plug-in Management settings** (`Edit > Project Settings > XR Plug-in Management`) — OpenXR should be enabled with the Meta Quest feature group active.
5. **Run it:**
   - **On-headset:** connect the Quest 3 via USB (with Developer Mode + USB debugging authorized), then `Build And Run` from Unity, or build an `.apk` and install it with ADB/Meta Quest Developer Hub.
   - **Via Meta Quest Link:** connect the headset to your PC with Link enabled and press Play in the Unity Editor.

## Team & credits

Developed as a group project for university coursework, in collaboration with two other students, combining UX research, game design and XR development.

## License

No open-source license has been chosen for this project — all rights are reserved by the authors. Reuse of the code, documentation, or included assets requires permission from the project authors.
