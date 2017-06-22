ECS-CrossPlatform status: [![Build status](https://ci.appveyor.com/api/projects/status/owpp4y7ruyn0skm1/branch/ECS-CrossPlatform?svg=true)](https://ci.appveyor.com/project/intercross21/pulsar4x/branch/ECS-CrossPlatform)
# Pulsar4x

[![Join the chat at https://gitter.im/Pulsar4x/Lobby](https://badges.gitter.im/Pulsar4x/Lobby.svg)](https://gitter.im/Pulsar4x/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A Fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is being develop in in C#. The long term goal of the project was origonaly to produce a feature complete clone of [Aurora](http://aurora2.pentarch.org/index.php).
however due to Steve Walmsley picking up development on his own C# version of Aurora, this has started to vear off on it's own path, and has become a flexable, modable auroralike engine. 

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

IRC channel #Pulsar4x on Freenode.  
Gitter room at https://gitter.im/Pulsar4x/Lobby# (another chat system that uses your git account. messages are persistant and you can paste/drag and drop screenshots directly to the chat)  

Bugs can be reported on the [issue tracker.](https://github.com/Pulsar4xDevs/Pulsar4x/issues)

## Compiling Pulsar4x ECS-CrossPlatform branch:

To compile Pulsar4x we recommend [VisualStudio 2015 Community Edition](https://www.visualstudio.com/downloads/download-visual-studio-vs). we're starting to use a bit of C#6, so 2013 is not recomended unless you know what you're doing.  
Xamarin Studio does work under windows, and the process is the same as for VS2015.

### Linux
MonoDevelop
Mono-Complete
and we recomended that you also install:
Monodevelop-nunit (to run the unit tests)
In monodevelop Tools -> Addin Manager -> Gallery 
install Eto.Forms Support Addin (this adds some autocomplete to the eto.forms xaml, and shows a live preview of forms as you code them, and adds some Templates for the eto.forms stuff. 
Note that the latest flatpack versions of monodevelop has issues due to running in a sandbox, currently I am unable to get the game to run/debug under the latest flatpack version of MD.
installing via this script: https://github.com/cra0zy/monodevelop-run-installer
is an option, and will run/debug, however versioncontrol is currently disabled due to a missing dependancy. 
also note that eto.forms plugin for MD is not currently as feature complete as it is for VS. 


### Mac
mono + GTK + Xamarin Studio
http://www.monodevelop.com/download/


### Cross Platforms:
Another IDE option is the cross platform Rider from jetbrains: https://www.jetbrains.com/rider/  
I'm currently using this under linux and I'm finding it a good option, though a bit heavier than MonoDevelop, it's lighter and more responsive than Visual studio. Although in beta it apears to have all the bells and whistles, and currently a lot easier to setup than MD, and a far faster install than VS.
Currently only tested in linux (and mac?)
There is currently no Eto.forms plugin for Rider. 

## Instructions:

1. Clone the Git Repo.

2. Open the solution file "Pulsar4X\Pulsar4X.sln".

3. a. If you're running Windows: unload the GTK and Mac projects by right clicking them in the solution explorer (!Don't delete them!) (this should eventualy  be fixed but at the time of this writing they're WIP.)
  
 b. If you're running Linux: Our primary linux dev has disapeared into the either, but I've started attempting to dev on linux mint. wish me luck...  
install monodev and mono-complete. (bare minimum)
open the Pulsar4x.sln in monodevelop.  

 c. If you're running Mac: you'll need xamarin studio to compile it for mac, there is a dedicated mac project, but that is missing the OpenTK lib. as I dont' have access to mac I'm unable to test or fix this. you can however run the gtk project, which has some bugs I'm hoping will get fixed when a new version of eto.forms comes out.

4. a. if you're running Windows Set Pulsar4x.CrossPlatform.WPF project as the startup project. you should be able to build and/or run from there.   
 b. Linux: Set Pulsar4x.CrossPlatform.Gtk2 as teh startup project. if you have problems building and running try unloading the WPF, Mac and Test projects.
 c. Mac:  See 3c

Note! there are a number of nuget packages that VS should automaticaly get the first time you attempt to build. 
the IDE will have a number of errors, build the solution anyway, VS will get the packages for you.

You *can* also run the gtk2 project in visual studio, this will help with debugging linux side of things as visual studio debugger apears to be superior to monodevelop. there currently apear to be some null refference exceptions that get hit when running from the GTK2 project that don't happen in the wpf. 

##Working with the code:
I need to flesh this section out a bit more, but for now, read the pages in the wiki, or ask me on IRC.
