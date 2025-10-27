# About

The {{title}} command will monitor events sent to the logged in
player on the CLI. Player events include updates such as content
notifications, inventory updates, mail updates, and more. 

**IMPORTANT**: The command only works if the realm is configured to use
Beamable Notifications, which is the default setting for new realms 
as of July 2023. However, if the realm is not using Beamable Notifications,
the following error will be displayed.

```
Only realms with beam notifications are supported. This realm currently has pubnub.
Try setting the realm config to beam with this command, 
""beam config realm set --key-values 'notification|publisher::beamable'""
```

To get started with the command, make sure you have the [CLI configured](../../../../guides/getting-started/),
and an available player. 

1. Run `beam me` to get access to the player's `playerId`. 
2. Use the portal, and navigate to the player's inventory page.
3. Run `beam listen player`, and leave the program running. This starts the listening process.
4. On the portal, change the player's currency value. Alternatively, send the player some mail.
5. On the console, notice the event is received. 