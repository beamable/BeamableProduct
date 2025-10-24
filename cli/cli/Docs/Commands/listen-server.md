# About

The {{title}} command will monitor realm events, such as content publications 
or changes to realm configuration. Realm events are sent through a websocket
connection, and require admin level privileges. 

This command is meant only as a diagnostic tool, and does not have robust
connection recovery logic. 


To get started with the command, make sure you have the [CLI configured](../../../../guides/getting-started/),
and an available player.

1. Run `beam listen server`, and leave the process running. 
2. Open portal, and navigate to the realm config page. Add a new configuration setting.
3. On the console, notice that the new configuration has been received. 