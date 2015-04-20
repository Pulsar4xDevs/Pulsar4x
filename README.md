ECS-Switchover status: [![Build status](https://ci.appveyor.com/api/projects/status/owpp4y7ruyn0skm1/branch/ECS-Switchover?svg=true)](https://ci.appveyor.com/project/intercross21/pulsar4x/branch/ECS-Switchover)
# Pulsar4x

A Fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is being develop in in C#. The long term goal of the project is to produce a feature complete clone of [Aurora](http://aurora2.pentarch.org/index.php).

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

IRC channel #Pulsar4x on Freenode.

Bugs can be reported on the [issue tracker.](https://github.com/Pulsar4xDevs/Pulsar4x/issues)

## Compiling Pulsar4x

To compile Pulsar4x we recommend either [VisualStudio 2013 Community Edition](http://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx) or Visual Studio Express 2013 for Windows Desktop, available [here.](http://www.visualstudio.com/downloads/download-visual-studio-vs)
 
Instructions:

1. Clone the Git Repo.

2. Open the solution file "Pulsar4X\Pulsar4X.sln".

3. Build the "Pulsar4X.UI" project. This will automatically build the Pulsar Library (containing the game logic) as well as the UI. You can find the compile program in "Pulsar4X\Pulsar4X.UI\bin\Debug" or "Pulsar4X\Pulsar4X.UI\bin\Release", depending on the build configuration.  