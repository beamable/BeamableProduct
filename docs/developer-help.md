Welcome to the monorepo, friend. Stay awhile, and pay attention :smile: 

---

This repository has the following products, 
1. the CLI
2. the Unity SDK
3. the Templates
4. the Microservice Framework

This repository also has a lot of supporting libraries. Some important ones to call out include, 
1. the Terraform
2. the Perf Test Standalone Microservice
3. the Build Scripts




### Releasing 1.x patches

In the 1.x.y days of Beamable, we used Jenkins to make deployments of the Unity SDK, CLI, Base Docker Image (which we no longer even have), and Nuget packages. While hopefully rare, we still may need to deploy 1.x.y releases every so often. 

The first step is to get the code you want to deploy onto the `production-1-19-0` branch. It will likely be dicey, but I recommend 
1. checking out `production-1-19-0`, forking a branch called `patch/1-x-y` (where `x` and `y` are your release numbers), 
2. cherry picking commits from the main repo back into the branch, 
3. opening a PR from your branch back into `production-1-19-0` and letting the tests run and the team stare at the changes for awhile. 

Then, when it is time to release, navigate to Jenkins, [https://db-jenkins.disruptorbeam.com/](https://db-jenkins.disruptorbeam.com/). 
If you're making a Release Candidate, select the "Beamable_Staging" job, or if you're making a full Production Release, select the "Beamable_Production" job.

Inside the job, on the left-hand menu, select, "Build with Parameters",
1. ensure the `SOURCE_BRANCH` is set to "production-1-19-0"
2. ensure the `VERSION` is set correctly for your version. If you're releasing 1.19.500, then manually enter that in the `VERSION` field. Do not enter the release candidate number if you're making a staging build (that will be added automatically). 