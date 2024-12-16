## Init Readme file

## About

This Repository is a nuget package responsible standardizing HVS (Health Vulnerability and security) for subsurface applications. The package is hosted in the [github registry](https://github.com/orgs/equinor/packages?repo_name=SubSurfAppManagementMonitoringNuGet).

[github nuget registry documentation](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)

## Uploading new version of the nuget package

In the github repository, Click Create new release. Create a tag in the fromat `v#.#.#`, where `#` are one or more numbers. Upon publishing the release Github actions will pack and upload a new package with version `v#.#.#`.
