# Sharktooth [![Build status](https://ci.appveyor.com/api/projects/status/w39w48jjb2721qba?svg=true)](https://ci.appveyor.com/project/PikminGuts92/sharktooth)
At the moment this is just a set of tools to convert song charts from FSG games.

The latest build can be found on [AppVeyor](https://ci.appveyor.com/project/PikminGuts92/sharktooth/branch/master/artifacts).

# System Requirements
You will need a Windows machine with at least .NET Framework 4.6.1 installed (Linux with Mono installed may also work).

# Overview
## Mid2Mub
CLI tool for converting `.mid` charts to `.fsgmub`. Primarily used for DJ Hero and DJ Hero 2.
- Usage:
  - `Mid2Mub.exe song.mid output.fsgmub`

## Mub2Mid
CLI tool for converting `.fsgmub` charts to `.mid`. Primarily used for DJ Hero and DJ Hero 2.
- Usage:
  - `Mub2Mid.exe song.fsgmub output.mid`

## Xmk2Mid
CLI tool for converting `.xmk` charts and `.far` archives containing `.xmk` files to `.mid`. Primarily used for Guitar Hero Live.
- Usage:
  - `Xmk2Mid.exe GHL2517.far -o GHL2517.mid -r -q=1/32`
    - Note: Please use `--help` in tool for more details on command line options