POGOLib
===================

POGOLib is written in C# and aims to be a community driven Pok�monGO API Library. Feel free to submit pull requests.

Libraries
-------
These libraries are used and should be installed automatically through NuGet:

 - [Google Protocol Buffers C# 3.0.0-beta3](https://www.nuget.org/packages/Google.Protobuf) (For communicating with Pok�monGO)
 - [Newtonsoft.Json 9.0.1](https://www.nuget.org/packages/newtonsoft.json/) (For the PTC login flow and savedata)
 
Build
-------
In order to build POGOLib you need to have two things installed:

 - [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx)
 - [.NET Framework 4.6.1](https://www.microsoft.com/download/details.aspx?id=49981)

Open **POGO.sln** with **Visual Studio 2015** and go to **Build > Build Solution**. Visual Studio should automatically download the required libraries for you.

You can find the DLL here: **POGO\POGOLib\bin\Debug\POGOLib.dll**.


Credits
-------
https://github.com/Mila432/Pokemon_Go_API
https://github.com/tejado/pokemongo-api-demo