Beamable Project [![Test](https://github.com/beamable/BeamableProduct/actions/workflows/buildPush.yml/badge.svg?branch=main)](https://github.com/beamable/BeamableProduct/actions/workflows/buildPush.yml)
================

This repo contains the source code to the Beamable Package

Project Structure
-----------------

### Primary

* `client/` - Unity project with Beamable packages
* `cli/` - cli project written in C#
* `microservice` - microservices C# project

### Additional

* `README.md` - this README file
* `build/` - server side component builds using Docker
* `jenkins/` - supporting files for Jenkins builds of Beamable
* `plugins/` - source code for native Android and iOS plugins
* `rfc/` - place to create and store [request for comments](https://en.wikipedia.org/wiki/Request_for_Comments)
* `stress-tests` - standalone microservice used for stress testing, called by GitHub Action
* `templates` - C# project templates for standalone services
* `terraform` - terraform configuration
* `wiki` - folder with additional description for some of the task, updates repository [wiki](https://github.com/beamable/BeamableProduct/wiki)


Branching policy
-----------------

This repo folows [gitflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) workflow: 

- `main` is development branch
- `production` is production branch
- work is done on feature branches which starts with prefixes `feature/` or `fix/` depending of if change is introducing new features or just fixes existing ones
