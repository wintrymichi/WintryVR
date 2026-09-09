# Privacy model

* **Nothing is recorded continuously.** The microphone runs a local energy VAD; audio leaves the device only
  when an utterance is sent to the STT provider (and only if the wake word was heard or the conversation window
  is open). Camera frames are captured **only on a trigger** (voice command, gaze dwell, selection, scene change,
  low-confidence retry), downscaled, sent once, and not stored.
* **Indicators**: camera (orange), microphone (green) and cloud (blue) dots in the HUD light up while in use
  (`PrivacyIndicatorEvent`). `Privacy → Show indicators` cannot hide them while cloud processing is on.
* **Permissions** are requested lazily with the feature that needs them (`PermissionManager`): microphone,
  camera + headset camera, scene, coarse location (opt-in only).
* **Cloud Processing toggle** (AI and Privacy sections). Off → the AI provider must be local (`LocalAIProvider`)
  or mock; STT/TTS must be local; spatial questions are still answered locally.
* **Local processing**: intent detection, wake word, VAD, spatial queries (where/how many/nearest/left-right),
  memory, scene graph, texture/LOD generation, all UI — everything except LLM reasoning, cloud STT/TTS and search.
* **History**: `Privacy → Keep history` off disables the transcript panel; *Clear session* forgets objects, cards,
  highlights and turns; *Clear history* keeps objects; *Clear all data* wipes PlayerPrefs, secrets entered in-app,
  the asset cache and resets settings.
* **Location** is off by default, coarse (500 m), and only added as text context to the AI when enabled.
* **Keys** never live in the source tree or the APK (`SecretStore`); a backend proxy is the recommended deployment.
