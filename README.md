POGOLib [![AppVeyor](https://img.shields.io/appveyor/ci/AeonLucid/pogolib/master.svg?maxAge=60)](https://ci.appveyor.com/project/AeonLucid/pogolib) [![NuGet](https://img.shields.io/nuget/v/POGOLib.Official.svg?maxAge=60)](https://www.nuget.org/packages/POGOLib.Official)
===================

POGOLib is written in C# and aims to be a community-driven PokémonGo API. Feel free to submit pull requests.

The library is a bit low-level now but the goal is to provide a high-level library while also allowing low-level request crafting.

# Changelog

[Click here to read the changelog of POGOLib.](CHANGELOG.md)

# Installation

## Supported Platforms

* .NET Standard 1.1

## NuGet

### Console
Run `Install-Package POGOLib.Official`  in `Tools > NuGet Package Manager > Package Manager Console` .

### Package Browser
Right click your project in Visual Studio, click `Manage NuGet Packages..`, make sure `Browse` is pressed. Search for `POGOLib.Official` and press `Install`.

# Features

## Authentication

POGOLib supports **Pokemon Trainer Club** and **Google**. 

We also allow you to store the `session.AccessToken` to a file using `JsonConvert.SerializeObject(accessToken, Formatting.Indented)` , using this you can cache your authenticated sessions and load them later by using `JsonConvert.DeserializeObject<AccessToken>("json here")`.

You can view an example of how I implemented this in the [demo](https://github.com/AeonLucid/POGOLib/blob/master/POGOLib.Official.Demo.ConsoleApp/Program.cs).

## Re-authentication

When PokémonGo tells POGOLib that the authentication token is no longer valid, we try to re-authenticate. This happens on intervals of 5, 10, 15, 20, 30... 60 (max) seconds. It keeps trying to re-authenticate. All other remote procedure calls that tried to request data will be stopped and continue when the session has re-authenticated. It will be like nothing happened.

When the session has successful re-authenticated, we fire an event. You can subscribe to the event to receive a notification.

```csharp
session.AccessTokenUpdated += (sender, eventArgs) =>
{
	// Save to file.. 
	// session.AccessToken
};
```

## Heartbeats
The map, inventory and player are automatically updated by the heartbeat.

The heartbeat checks every second if:

 - the seconds since the last heartbeat is greater than or equal to the [maximum allowed refresh seconds](https://github.com/AeonLucid/POGOProtos/blob/master/src/POGOProtos/Settings/MapSettings.proto#L9) of the game settings;
 - the distance moved is greater than or equal to the [minimum allowed distance](https://github.com/AeonLucid/POGOProtos/blob/master/src/POGOProtos/Settings/MapSettings.proto#L10) of the game settings;

If one of these is true, a heartbeat will be sent. This automatically fetches the map data surrounding your current position, your inventory data and the game settings.

If you want to receive a notification when these update, you can subscribe to the following events.

```csharp
session.Player.Inventory.Update += (sender, eventArgs) =>
{
    var session = (Session) sender;

    // Access updated inventory: session.Player.Inventory
    Console.WriteLine("Inventory was updated.");
};

session.Map.Update += (sender, eventArgs) =>
{
    var session = (Session) sender;

    // Access updated map: session.Map
    Console.WriteLine("Map was updated.");
};
```

*Make sure you start the session **after** subscribing to the events.*

## Throttling

Requests are throttled automatically, this means that only one request will be sent every X milliseconds. You can configure the milliseconds like this.

```csharp
POGOLib.Configuration.ThrottleDifference = 1000;
```

## PokeHash

POGOLib has built-in support for the PokeHash service and does also support multiple keys. You have to use this service if you want a minimal chance of receiving captcha's.

Read more: [https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer](https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer)

## Custom crafted requests

*This is for now almost the only way to receive data. It's easy though!*

If you want to know what requests are available, click [here](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos/Networking/Requests/Messages).
If you want to know what responses belong to the requests, click [here](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos/Networking/Responses).

If you want to know what kind of data is available, [have a look through all POGOProtos files](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos).

You can send a request and parse the response like this.

```csharp
var closestFort = session.Map.GetFortsSortedByDistance().FirstOrDefault();
if (closestFort != null)
{
    var fortDetailsBytes = await session.RpcClient.SendRemoteProcedureCallAsync(new Request
    {
        RequestType = RequestType.FortDetails,
        RequestMessage = new FortDetailsMessage
        {
            FortId = closestFort.Id,
            Latitude = closestFort.Latitude,
            Longitude = closestFort.Longitude
        }.ToByteString()
    });
    var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

    Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));
}
```

**Example output:**

```json
{
  "FortId": "e4a5b5a63cf34100bd620c598597f21c.12",
  "TeamColor": 0,
  "PokemonData": null,
  "Name": "King Charles I",
  "ImageUrls": [
    "http://lh5.ggpht.com/luiWs5VRelnqX1dtvOSR1taEKAuwnNJjReLaGwi0GQgrHL1BLRsb1p13Dzk0A0cY1EMgplX2ELLiLy0XHSPC"
  ],
  "Fp": 0,
  "Stamina": 0,
  "MaxStamina": 0,
  "Type": 1,
  "Latitude": 51.507335,
  "Longitude": -0.127689,
  "Description": "",
  "Modifiers": [
    {
      "ItemId": 501,
      "ExpirationTimestampMs": 1478571243838,
      "DeployerPlayerCodename": "Poketigre77"
    }
  ]
}
```

# Example

This example logs in, retrieves nearby pokestops, checks if you have already searched them. If you have not, he will check the distance between you and the pokestop. If you are close enough to the pokestop, he will search it and display the results.

```csharp
var loginProvider = new PtcLoginProvider("username", "password");
//                                                  Lat        Long
var session = await Login.GetSession(loginProvider, 51.507351, -0.127758);

// Send initial requests and start HeartbeatDispatcher.
// This makes sure that the initial heartbeat request finishes and the "session.Map.Cells" contains stuff.
if (!await session.StartupAsync())
{
    throw new Exception("Session couldn't start up.");
}

Console.WriteLine($"I have caught {session.Player.Stats.PokemonsCaptured} Pokemon.");
Console.WriteLine($"I have visisted {session.Player.Stats.PokeStopVisits} pokestops.");

foreach (var fortData in session.Map.GetFortsSortedByDistance(f => f.Type == FortType.Checkpoint && f.LureInfo != null))
{
    if (fortData.CooldownCompleteTimestampMs <= TimeUtil.GetCurrentTimestampInMilliseconds())
    {
        var playerDistance = session.Player.DistanceTo(fortData.Latitude, fortData.Longitude);
        if (playerDistance <= session.GlobalSettings.FortSettings.InteractionRangeMeters)
        {
            var fortSearchResponseBytestring = await session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.FortSearch,
                RequestMessage = new FortSearchMessage
                {
                    FortId = fortData.Id,
                    FortLatitude = fortData.Latitude,
                    FortLongitude = fortData.Longitude,
                    PlayerLatitude = session.Player.Latitude,
                    PlayerLongitude = session.Player.Longitude
                }.ToByteString()
            });

            var fortSearchResponse = FortSearchResponse.Parser.ParseFrom(fortSearchResponseBytestring);

            Console.WriteLine($"{playerDistance}: {fortSearchResponse.Result}");

            foreach (var itemAward in fortSearchResponse.ItemsAwarded)
            {
                Console.WriteLine($"\t({itemAward.ItemCount}) {itemAward.ItemId}");
            }
        }
        else
        {
            Console.WriteLine("Out of range.");
        }
    }
    else
    {
        Console.WriteLine("Cooldown.");
    }
}
```
