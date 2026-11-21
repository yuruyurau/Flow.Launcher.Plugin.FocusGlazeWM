dotnet publish Flow.Launcher.Plugin.FocusGlazeWM -c Release -r win-x64 --no-self-contained

Compress-Archive -Path "Flow.Launcher.Plugin.FocusGlazeWM/bin/Release/win-x64/publish/*" -DestinationPath "Flow.Launcher.Plugin.FocusGlazeWM/bin/FocusGlazeWM.zip" -Force
