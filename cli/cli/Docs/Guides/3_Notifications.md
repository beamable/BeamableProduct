## Listening for Notifications

Beamable provides several player facing and game facing callbacks
via Websocket. The Unity SDK relies on these websocket events to 
manage state and run C# callbacks within the SDK. The CLI provides
two useful commands to monitor these websocket events, one for
[player events](doc:listen-player) and a second for [game events](doc:listen-server).

**IMPORTANT**: these commands are meant as diagnostic tools, and should
not be used as critical components in a game's architecture. Neither command
has robust reconnection logic, and if the websocket is closed due to 
ephemeral network failure, it will not re-open automatically.


