# CLI QA Wars

## Summary

I would like to schedule a QA Wars for CLI. For now at least once,
based on the outcome we can decide if and when we should do it again.

## Motivation

Right now I see two big issues with using CLI:

- there is some kind of "tribal knowledge" how to use some of the commands correctly.
- most of the team is not aware of what can be done using CLI tools.

## Implementation

At least for now just QA War spreadsheet on GDocs should be more than enough.
Steps:

- install latest version using install script from repository
  - does install work?
  - does `beam --help` work?
- init new project- `beam init`
  - can you login into existing project?
  - can you see created `.beamable` directory with files in it?
- save credentials
  - can you save credentials to project using `beam login --save-to-file`?
  - test it out using `beam get /basic/accounts/me`
- `beam content` commands
  - can you download content using `beam content pull`?
  - can you open and edit content using `beam content open <content_id>`?
  - can you check status using `beam content status`?
  - can you publish changes using `beam content publish`?
- `beam project` commands
  - can you connect unity project using `beam project add-unity-project <path>`?
  - can you create and run new SAMS using [this tutorial](https://docs.beamable.com/docs/standalone-microservices)?
  - can you create new storage using `beam project new-storage <name>`?
  - can you deploy those services with `beam services deploy --remote`?
