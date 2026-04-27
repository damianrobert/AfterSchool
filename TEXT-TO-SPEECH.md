Task: Develop and integrate a complete Text-to-Speech (TTS) module into the target .NET application.

1. Technology Stack & Dependencies:

- Framework: .NET Framework or .NET Core/5+.
- Primary Library: System.Speech.Synthesis (Namespace: System.Speech).
- Project Modification: Ensure the System.Speech NuGet package or assembly reference is added to the .csproj file.

2. Core Logic Requirements:

- Engine Initialization: Create a persistent instance of the SpeechSynthesizer class to manage the audio stream.
- Speech Synthesis:
  - Implement Asynchronous Playback using .SpeakAsync(string text) so the UI remains responsive.
  - Implement Playback Control using the .Pause() and .Resume() methods of the synthesizer instance.
- Audio Customization:
  - Provide inputs (e.g., TrackBars or Sliders) to control the .Rate property (Speed, typically range -10 to 10).
  - Provide inputs to control the .Volume property (Loudness, range 0 to 100).
- File I/O Features:
  - Text Import: Use a StreamReader to load the contents of a .txt file into the synthesis input field.
  - Audio Export: Implement a "Save to File" feature. Use .SetOutputToWaveFile(string path) to redirect the output to a
    .wav file, call .Speak(text) synchronously, and then use .SetOutputToDefaultAudioDevice() to reset the output for
    normal playback.

3. Implementation Steps:
1. UI Setup: Add a text input area, buttons for Play, Pause, Resume, and Save, and sliders for Volume and Speed.
1. Event Handling: Wire the buttons to the SpeechSynthesizer methods mentioned above.
1. Error Handling: Add try-catch blocks for file operations (reading text and saving audio) to handle invalid paths or locked
   files.

1. Validation:

- Confirm that the System.Speech reference is correctly resolved.
- Verify that the audio terminates correctly when the application is closed.
