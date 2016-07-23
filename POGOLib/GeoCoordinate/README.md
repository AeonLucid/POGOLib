![Icon](https://i.imgur.com/OfoiwTs.png)
# GeoCoordinate [![Build status](https://ci.appveyor.com/api/projects/status/k18x42mpj4bp93rn?svg=true)](https://ci.appveyor.com/project/ghuntley/geocoordinate)


GeoCoordinate is a Portable Class Library compatible implementation of System.Device.Location.GeoCoordinate. It is an exact 1:1 API compliant implementation and will be supported until MSFT [sees it fit to embed the type](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/5221530-geocoordinate-class-included-in-portable-class-lib). Which at that point this implementation will cease development/support and you will be able to simply remove this package and everything will still work.

# Supported Platforms

* Mono
* .NET 4.5
* .NET Core
* Windows Phone 8.x
* Universal Windows Platform
* Xamarin iOS
* Xamarin Android

# Installation
Installation is done via NuGet:

    PM> Install-Package GeoCoordinate
    
# Usage

    GeoCoordinate pin1 = new GeoCoordinate(lat, lng);
    GeoCoordinate pin2 = new GeoCoordinate(lat, lng);
    
    double distanceBetween = pin1.GetDistanceTo(pin2);

For more examples, refer to the MSDN reference documentation over at: https://msdn.microsoft.com/en-us/library/system.device.location.geocoordinate(v=vs.110).aspx

# With thanks to
* The icon "[Map Marker](https://thenounproject.com/term/map-marker/60112)" designed by [julianne](https://thenounproject.com/janne232) from The Noun Project.
