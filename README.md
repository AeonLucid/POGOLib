POGOLib
===================

POGOLib is written in C# and aims to be a community-driven PokémonGo API. Feel free to submit pull requests.

The library is a bit low-level now but the goal is to provide a high-level library while also allowing low-level request crafting.

# Features

## Authentication

POGOLib supports **Pokemon Trainer Club** and **Google**. 

We also allow you to store the `session.AccessToken` to a file using `JsonConvert.SerializeObject(accessToken, Formatting.Indented)` , using this you can cache your authenticated sessions and load them later by using `JsonConvert.DeserializeObject<AccessToken>("json here")`.

You can view an example of how I implemented this in the [demo](https://github.com/AeonLucid/POGOLib/blob/master/Demo/Program.cs).

## Re-authentication

When PokémonGo tells POGOLib that the authentication token is no longer valid, we try to re-authenticate. This happens on intervals of 5, 10, 15, 20, 30... 60 (max) seconds. It keeps trying to re-authenticate. All other remote procedure calls that tried to request data will be stopped and continue when the session has re-authenticated. It will be like nothing happened.

When the session has successful re-authenticated, we fire an event. You can subscribe to receive a notification.

```csharp
var session = Login.GetSession("username", "password", LoginProvider.PokemonTrainerClub, 51.507351, -0.127758);
session.AccessTokenUpdated += (sender, eventArgs) =>
{
	// Save to file.. 
	// session.AccessToken
};
```

## Heartbeats
The map, inventory and player are automatically updated by the heartbeat.

The heartbeat is checks every second if:

 - the seconds since the last heartbeat is greater than or equal to the [maximum allowed refresh seconds](https://github.com/AeonLucid/POGOProtos/blob/master/src/POGOProtos/Settings/MapSettings.proto#L9) of the game settings;
 - the distance moved is greater than or equal to the [minimum allowed distance](https://github.com/AeonLucid/POGOProtos/blob/master/src/POGOProtos/Settings/MapSettings.proto#L10) of the game settings;

If one of these is true, an heartbeat will be sent. This automatically fetches the map data surrounding your current position, your inventory data and the game settings.

If you want to receive a notification when these update, you can subscribe to following events.

```csharp
var session = Login.GetSession("username", "password", LoginProvider.PokemonTrainerClub, 51.507351, -0.127758);
session.Player.Inventory.Update += (sender, eventArgs) =>
{
	// Access updated inventory: session.Player.Inventory
	Console.WriteLine("Inventory was updated.");
};
session.Map.Update += (sender, eventArgs) =>
{
	// Access updated map: session.Map
	Console.WriteLine("Map was updated.");
};
```

## Custom crafted requests

*This is for now almost the only way to receive data. It's easy though!*

If you want to know what requests are available, click [here](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos/Networking/Requests/Messages).
If you want to know what responses belong to the requests, click [here](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos/Networking/Responses).

If you want to know what kind of data is available, [have a look through all POGOProtos files](https://github.com/AeonLucid/POGOProtos/tree/master/src/POGOProtos).

You can send a request and parse the response like this.

```csharp
var session = Login.GetSession("username", "password", LoginProvider.PokemonTrainerClub, 51.507351, -0.127758);
var fortDetailsBytes = session.RpcClient.SendRemoteProcedureCall(new Request
{
	RequestType = RequestType.FortDetails,
	RequestMessage = new FortDetailsMessage
	{
		FortId = "e4a5b5a63cf34100bd620c598597f21c.12",
		Latitude = 51.507335,
		Longitude = -0.127689
	}.ToByteString()
});
var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);
					
Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));
```

**Output:**

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
      "ExpirationTimestampMs": 1469238785723,
      "DeployerPlayerCodename": "Gibbons3D"
    }
  ]
}
```

# Examples

This example logs in, retrieves nearby pokestops, checks if you have already searched them, if you have not, he will check the distance between you and the pokestop. If you are close enough to the pokestop, he will search it and display the results.

```csharp
var session = Login.GetSession("username", "password", LoginProvider.PokemonTrainerClub, 51.507351, -0.127758);
				
Console.WriteLine($"I have caught {session.Player.Stats.PokemonsCaptured} Pokémon.");
Console.WriteLine($"I have visisted {session.Player.Stats.PokeStopVisits} pokestops.");

foreach (var fortData in session.Map.GetFortsSortedByDistance(f => f.Type == FortType.Checkpoint && f.LureInfo != null))
{
	if (fortData.CooldownCompleteTimestampMs <= TimeUtil.GetCurrentTimestampInMilliseconds())
	{
		var playerDistance = session.Player.DistanceTo(fortData.Latitude, fortData.Longitude);
		if (playerDistance <= session.GlobalSettings.FortSettings.InteractionRangeMeters)
		{
			var fortSearchResponseBytestring = session.RpcClient.SendRemoteProcedureCall(new Request
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

# Libraries
These libraries are used and should be installed automatically through NuGet:

 - [Google Protocol Buffers C# 3.0.0-beta4](https://www.nuget.org/packages/Google.Protobuf) (For communicating with PokémonGo)
 - [Newtonsoft.Json 9.0.1](https://www.nuget.org/packages/newtonsoft.json/) (For the PTC login flow and savedata)
 - [log4net [1.2.15] 2.0.5](https://www.nuget.org/packages/log4net/) (For debug logging)
 - [S2 Geometry Library 1.0.1](https://www.nuget.org/packages/S2Geometry/1.0.1) (For calculating the necessary cell IDs)
 - [GPSOAuthSharp 0.0.5](https://www.nuget.org/packages/GPSOAuthSharp) (For Google authentication)
 
# Build
In order to build POGOLib you need to have two things installed:

 - [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx)
 - [.NET Framework 4.6.1](https://www.microsoft.com/download/details.aspx?id=49981)

Open **POGO.sln** with **Visual Studio 2015** and go to **Build > Build Solution**. Visual Studio should automatically download the required libraries for you through NuGet.

You can find the DLL here: **POGO\POGOLib\bin\Debug\POGOLib.dll**.