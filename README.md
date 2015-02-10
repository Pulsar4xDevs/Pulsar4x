# Pulsar4x

A Fan work recreation of Aurora, a 4x space sim created by Steve Walmsley. Pulsar4x is being develop in in C# and is intended to be cross platform. The long term goal of the project is to produce a feature complete clone of [Aurora](http://aurora2.pentarch.org/index.php).

## Community

Main forum at: http://aurora2.pentarch.org/index.php/board,169.0.html

IRC channel #Pulsar4x on Freenode.

Bugs can be reported on the [issue tracker.](https://github.com/Pulsar4xDevs/Pulsar4x/issues)

## Compiling Pulsar4x

To compile Pulsar4x you'll need either Visual Studio 2010/2012/2013 or the latest version of MonoDevlop/SharpDevelop. If you are on windows we recommend either [VisualStudio 2013 Community Edition](http://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx) or Visual Studio Express 2013 for Windows Desktop, available [here.](http://www.visualstudio.com/downloads/download-visual-studio-vs) If you are on Mac or Linux you can use either [Monodevelop](http://www.monodevelop.com/) or [SharpDevelop](http://www.icsharpcode.net/OpenSource/SD/Default.aspx).  

The project uses a custom premake4 build, source available [here.](https://bitbucket.org/antagonist/premake-stable). Note that there are pre-compiled versions of Premake as part of the Project Repo.

Instructions:

1. Clone the Git Repo.

2. Run the version of premake appropriate for your platform (premake4.exe for windows, premake4.lin for linux and premake4.osx for Mac OS X). This will build the visual studio solution files (which are also used by MonoDevelop/SharpDevelop).

3. Open the solution file "Pulsar4X\Pulsar4X.sln".

4. Build the "Pulsar4X.UI" project. This will automatically build the Pulsar Library (containing the game logic) as well as the UI. You can find the compile program in "Pulsar4X\Pulsar4X.UI\bin\Debug" or "Pulsar4X\Pulsar4X.UI\bin\Release", depending on the build configuration.


