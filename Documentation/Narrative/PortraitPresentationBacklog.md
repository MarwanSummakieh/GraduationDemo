# Deferred dialogue presentation

User design direction, saved for later implementation:

- Build character portraits from separate body-part sprites.
- Use Unity Animator Controllers to animate their transforms (position, rotation and scale), allowing poses, gestures and expressions without requiring a full frame-by-frame sprite sequence.
- During dialogue, darken the exploration view and emphasize the portraits and dialogue text.

Existing voice definitions already carry speaker and expression cues, which can provide inputs to a future portrait presentation layer. Keep exact subtitle/voice-ID binding and dialogue completion behavior intact when integrating it.

No portrait assets, Animator Controllers or exploration dimming have been implemented as part of the hint-coverage change.
