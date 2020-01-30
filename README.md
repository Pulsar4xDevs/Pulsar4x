# Pulsar4x
[![Build status](https://ci.appveyor.com/api/projects/status/owpp4y7ruyn0skm1/branch/Master?svg=true)](https://ci.appveyor.com/project/intercross21/pulsar4x/branch/Master)
[![Join the chat at https://gitter.im/Pulsar4x/Lobby](https://badges.gitter.im/Pulsar4x/Lobby.svg)](https://gitter.im/Pulsar4x/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is developed in C#. The long term goal of the project was originally to reproduce a feature-complete clone of [Aurora](http://aurora2.pentarch.org/index.php).
However, due to Steve Walmsley picking up development on his own C# version of Aurora, this has started to vear off on its own path, and has become a flexible, moddable aurora-like engine.

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

Pulsar4x on [Gitter](https://gitter.im/Pulsar4x/Lobby#).<br />
Gitter is a chat system that uses your git account. Messages are persistent and you can paste/drag and drop screenshots directly to the chat.

Bugs can be reported on the [issue tracker](https://github.com/Pulsar4xDevs/Pulsar4x/issues).  
The [Wiki](https://github.com/Pulsar4xDevs/Pulsar4x/wiki) has a good amount of information, though is not always up to date.

## Compiling Pulsar4x Master branch:

### Windows
To compile and debug Pulsar4x we recommend [VisualStudio 2019 Community Edition](https://www.visualstudio.com/downloads/download-visual-studio-vs). We're starting to use a bit of C#6, ~~so 2013 is not recommended unless you know what you're doing.~~ won't even load the newer csproj files.  2015 *might* work?
Xamarin Studio does work under Windows, and the process is the same as for VS2015.
Rider also works under windows. 

### Linux
MonoDevelop
Mono-Complete
and we recomended that you also install:
Monodevelop-nunit (to run the unit tests)

For the current UI to work, you'll need SDL version 2.0.5 or higher, if you're running Ubuntu < than 18, you'll likely need to compile and install SDL from source, if your update repositories are high enough you should be fine. 
This shouldnt be a problem on Windows since the dll is included for the Windows build. 
Note, you may have a permissions problem that I've been unable to fully figure out: https://github.com/Pulsar4xDevs/Pulsar4x/issues/216

### Mac
mono + GTK + Xamarin Studio
http://www.monodevelop.com/download/


### Cross Platforms:
Another IDE option is the cross platform [Rider](https://www.jetbrains.com/rider/) from JetBrains.
I'm currently using this under Linux and it's a good option. A bit heavier than MonoDevelop but it's lighter and more responsive than Visual Studio. 
Another option is [VSCode](https://code.visualstudio.com/) which is light, but required a bit of setup. 

## Instructions:

NOTE: you'll need a 64-bit PC. possibly 32-bit Windows could work but some libraries are not available on 32-bit Linux. 
You'll need SDL2 installed. 

1. Clone the Git Repo.

2. Open the solution file "Pulsar4X\Pulsar4X.sln". in Monodevelop, Visual Studio, or Rider.

	a. If you're running Windows: Should work. 

	b. If you're running Linux: Should work.

	c. If you're running Mac: you'll need xamarin studio to compile it for Mac. Let me know, as of 2019.07, this is not yet tested.

4. Set the imguinetUI project as the startup project. 

5. Run/debug to compile and run the project.  

**Important: there are a number of NuGet packages that VS/Monodevelop should automaticaly get the first time you attempt to build. 
The IDE will have a number of errors but build the solution anyway, VS will get the packages for you.**

### Compiling from a terminal:
See https://github.com/Pulsar4xDevs/Pulsar4x/issues/255 for instructions on compiling in linux from the term.

## Working with the code:
This section is a work in progress. Read the pages on the [Wiki](https://github.com/Pulsar4xDevs/Pulsar4x/wiki), which includes some [style guidelines](https://github.com/Pulsar4xDevs/Pulsar4x/wiki/Formating-rules-and-guidelines) or ask me questions on [Gitter](https://gitter.im/Pulsar4x/Lobby#), [Discord](https://discord.gg/qy8eZHh), or create an [issue](https://github.com/Pulsar4xDevs/Pulsar4x/issues). 
