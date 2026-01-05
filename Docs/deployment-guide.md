# Deployment Guide

GitHub actions are set up to automatically compile binaries and to create the release information.

Because of this, releases must not be created manually: They are automatically triggered when
a new tag is pushed to the repository.

## Trigger a release

```bash
git tag 1.2.0
git push origin 1.2.0
```

## Undoing a tag

This may be needed if the release process fails for some reason. Delete the tag via the GIT web UI,
then run:

```bash
git tag -d 1.2.0
```

After this, the tag can be re-created and pushed again.

## Testing the version checker

When running the application locally for testing, the version of the binary is
read from the project file. It does not use the changelog version like the 
releases do.

1. Open the project file, `Starfield Load Orderer.csproj`.
2. Change the version number to the desired version, e.g. `1.6.2`.