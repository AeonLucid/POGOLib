# POGOLib Changelog

## 1.5.2
- Updated POGOProtos to v2.12.0.

## 1.5.1
- Updated POGOProtos to v2.11.0.
- Added GetInbox to all requests [#92](https://github.com/AeonLucid/POGOLib/pull/92).

## 1.5.0
- Updated POGOProtos to v2.11.0-beta.
- Updated PokeHash to 0.67.1 [#91](https://github.com/AeonLucid/POGOLib/pull/91).

## 1.4.4
- Updated POGOProtos to v2.10.3.

## 1.4.3
- Updated PokeHash to 0.63.1 [#88](https://github.com/AeonLucid/POGOLib/pull/88).

## 1.4.2
- Updated dependencies.
- Updated POGOProtos to v2.9.2.
- Updated TwoFish Encryption.
- Fixed bug where HeartbeatDispatcher didn't cancel [#84](https://github.com/AeonLucid/POGOLib/pull/84).

## 1.4.1
- Updated PokeHash to 0.61.0 [#82](https://github.com/AeonLucid/POGOLib/pull/82).
- Updated POGOProtos to v2.9.1.
- Fixed [#81](https://github.com/AeonLucid/POGOLib/issues/81).
- Improved user agent management.

## 1.4.0
- Added `Pause()` and `ResumeAsync()` methods to `Session`.
- Updated PokeHash to 0.59.1 [#79](https://github.com/AeonLucid/POGOLib/pull/79).
- Updated Encryption / Decryption to TwoFish [#79](https://github.com/AeonLucid/POGOLib/pull/79).
- Fixed [#80](https://github.com/AeonLucid/POGOLib/issues/80).
- Fixed [#63](https://github.com/AeonLucid/POGOLib/issues/63).
- **Moved all events to `Session`**.

## 1.3.1
- Updated PokeHash to 0.57.4.

## 1.3.0
- Added PokeHash support.
- Added "Unknown 8".
- Updated dependencies.
- Improved location fixes generation.
- Improved `RefreshMapObjectsAsync` so it does not use `0` for all `SinceTimestampMs` fields anymore.
- Migrated projects to VS2017 RC.
- Removed xamarin demo project.

## 1.2.1
- Fixed not being able to specify `DeviceInfo` for a `Session`.
- Added version checking to `Session` Startup. Can be disabled using the `Configuration` class.
- Added the possibility to implement an hashing server or another source of hashing.
    - Create a class that extends the `IHasher` interface and set it to `Configuration.Hasher`. 

## 1.2.0
- Fixed issue [#59](https://github.com/AeonLucid/POGOLib/issues/59).
- Updated to "POGOProtos" v2.1.0.
- Updated `NiaHash` to work like PokemonGo IOS 1.15.0.

## 1.1.0
- Added `GoogleLoginProvider` through an additional NuGet package "POGOLib.Official.Google".
- Added a random IOS `DeviceInfo` generator.
- Added an option to specify a `DeviceInfo` object in the `Session` constructor.
- Updated methods / properties documentation.
- Updated to "POGOProtos" v2.0.2.
- Moved namespace from "POGOLib" to "POGOLib.Official".
- Removed unused assets code temporarily.

## 1.0.1
- Added target framework ".NET Framework 4.5".

## 1.0.0
- Release!