# Pulsar4x
[![Build status](https://ci.appveyor.com/api/projects/status/owpp4y7ruyn0skm1/branch/Master?svg=true)](https://ci.appveyor.com/project/intercross21/pulsar4x/branch/Master)
[![Join the chat at https://gitter.im/Pulsar4x/Lobby](https://badges.gitter.im/Pulsar4x/Lobby.svg)](https://gitter.im/Pulsar4x/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is developed in C#. The long term goal of the project was originally to reproduce a feature-complete clone of [Aurora](http://aurora2.pentarch.org/index.php).
However, due to Steve Walmsley picking up development on his own C# version of Aurora, this has started to vear off on its own path, and has become a flexible, moddable aurora-like engine.

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

IRC channel #Pulsar4x on Freenode.  
Gitter room at https://gitter.im/Pulsar4x/Lobby# (another chat system that uses your git account. Messages are persistent and you can paste/drag and drop screenshots directly to the chat)  

Bugs can be reported on the [issue tracker](https://github.com/Pulsar4xDevs/Pulsar4x/issues).

## Compiling Pulsar4x ECS-CrossPlatform branch:

To compile Pulsar4x we recommend [VisualStudio 2015 Community Edition](https://www.visualstudio.com/downloads/download-visual-studio-vs). We're starting to use a bit of C#6, so 2013 is not recommended unless you know what you're doing.  
Xamarin Studio does work under Windows, and the process is the same as for VS2015.

### Linux
MonoDevelop
Mono-Complete
and we recomended that you also install:
Monodevelop-nunit (to run the unit tests)
In monodevelop Tools -> Addin Manager -> Gallery 
install Eto.Forms Support Addin (this adds some autocomplete to the eto.forms xaml, and shows a live preview of forms as you code them, and adds some Templates for the eto.forms stuff. 
Note that the latest flatpack versions of MonoDevelop has issues due to running in a sandbox: currently, I am unable to get the game to run/debug under the latest flatpack version of MD.
Installing via this script: https://github.com/cra0zy/monodevelop-run-installer
is an option, and will run/debug, however version control is currently disabled due to a missing dependency. 
Also note that eto.forms plugin for MD is currently not as feature complete as it is for VS. 

For the current UI to work, you'll need SDL version 2.0.5 or higher, if you're running Ubuntu < than 18, you'll likely need to compile and install SDL from source, if your update repositories are high enough you should be fine. This shouldnt be a problem in Windows since the dll is included for the Windows build. 

### Mac
mono + GTK + Xamarin Studio
http://www.monodevelop.com/download/


### Cross Platforms:
Another IDE option is the cross platform [Rider](https://www.jetbrains.com/rider/) from JetBrains.
I'm currently using this under Linux and it's a good option. A bit heavier than MonoDevelop but it's lighter and more responsive than Visual Studio. 
Even though it is in beta, it appears to have all the bells and whistles. Easier to setup than MD and faster to install than VS.
Currently only tested in Linux (and Mac?)
There is currently no Eto.forms plugin for Rider. 

## Instructions:

NOTE: you'll need a 64-bit PC. possibly 32-bit Windows could work but some libraries are not availible in 32-bit Linux. 
You'll need SDL2 installed. 

1. Clone the Git Repo.

2. Open the solution file "Pulsar4X\Pulsar4X.sln". in Monodevelop or Visual Studio.

	a. If you're running Windows: Let me know, as of 2019.07, this is not yet tested. 

	b. If you're running Linux: Should work.

	c. If you're running Mac: you'll need xamarin studio to compile it for Mac. Let me know, as of 2019.07, this is not yet tested.

4. Set the imguinetUI project as the startup project. and run/debug to compile.  

**Important: there are a number of nuget packages that VS/Monodevelop should automaticaly get the first time you attempt to build. 
The IDE will have a number of errors but build the solution anyway, VS will get the packages for you.**

## Working with the code:
This section is a work in progress. Read the pages on the wiki, or ask me questions on IRC. 
