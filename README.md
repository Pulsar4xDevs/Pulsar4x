ECS-CrossPlatform status: [![Build status](https://ci.appveyor.com/api/projects/status/owpp4y7ruyn0skm1/branch/ECS-CrossPlatform?svg=true)](https://ci.appveyor.com/project/intercross21/pulsar4x/branch/ECS-CrossPlatform)
# Pulsar4x

A Fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is being develop in in C#. The long term goal of the project is to produce a feature complete clone of [Aurora](http://aurora2.pentarch.org/index.php).

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

IRC channel #Pulsar4x on Freenode.

Bugs can be reported on the [issue tracker.](https://github.com/Pulsar4xDevs/Pulsar4x/issues)

## Compiling Pulsar4x ECS-CrossPlatform branch:

To compile Pulsar4x we recommend [VisualStudio 2015 Community Edition](https://www.visualstudio.com/downloads/download-visual-studio-vs). we're starting to use a bit of C#6, so 2013 is not recomended unless you know what you're doing.  
Xamarin Studio does work under windows, and the process is the same as for VS2015.


Instructions:

1. Clone the Git Repo.

2. Open the solution file "Pulsar4X\Pulsar4X.sln".

3. a. If you're running Windows: unload the GTK and Mac projects by right clicking them in the solution explorer (!Don't delete them!) (this should eventualy  be fixed but at the time of this writing they're WIP.)
  
 b. If you're running Linux: Our linux dev has disapeared into the either, eventualy I hope to get around to attempting to compile on linux and writting instructions, but untill then, give it a go in xamarin studio and tell me your results. it's possible there are missing gtk related librarys.  
 c. If you're running Mac: welcome to the team! we don't currently have a mac specialist, feel free to take on the challenge!  

4. a. if you're running Windows Set Pulsar4x.CrossPlatform.WPF project as the startup project. you should be able to build and/or run from there.   
 b. Linux: See 3b
 c. Mac:  See 3c

Note! there are a number of nuget packages that VS should automaticaly get the first time you attempt to build. 
the IDE will have a number of errors, build the solution anyway, VS will get the packages for you.

##Working with the code:
I need to flesh this section out a bit more, but for now, read the pages in the wiki, or ask me on IRC.
